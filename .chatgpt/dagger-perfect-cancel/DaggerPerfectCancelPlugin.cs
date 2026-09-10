using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace Goni.DaggerPerfectCancel
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class DaggerPerfectCancelPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "goni.valheim.daggerperfectcancel";
        public const string PluginName = "Goni Dagger Perfect Cancel";
        public const string PluginVersion = "1.0.0";

        // Win32: XBUTTON2 = the second side mouse button, normally called Mouse5.
        private const int VkXButton2 = 0x06;
        private const int KeyDownMask = 0x8000;

        private static DaggerPerfectCancelPlugin _instance;
        private static ManualLogSource _log;

        private Harmony _harmony;
        private Type _playerType;
        private MethodInfo _setControls;
        private MethodInfo _inAttack;
        private MethodInfo _isBlocking;
        private MethodInfo _getCurrentWeapon;
        private FieldInfo _localPlayerField;
        private FieldInfo _animatorField;
        private FieldInfo _blockingAnimatorHashField;
        private MethodInfo _animatorGetBool;

        private int _attackIndex = -1;
        private int _attackHoldIndex = -1;
        private int _blockIndex = -1;
        private int _blockHoldIndex = -1;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<bool> _requireKnife;
        private ConfigEntry<bool> _confirmAnimatorBlock;
        private ConfigEntry<bool> _verbose;
        private ConfigEntry<int> _triggerVirtualKey;
        private ConfigEntry<int> _firstAttackStartTimeoutMs;
        private ConfigEntry<int> _firstAttackEndTimeoutMs;
        private ConfigEntry<int> _blockStartTimeoutMs;
        private ConfigEntry<int> _secondAttackStartTimeoutMs;

        private SequenceState _state = SequenceState.Idle;
        private long _stateStartedTicks;
        private bool _mouseWasDown;
        private bool _blockPressSent;
        private bool _warnedWeaponReflection;
        private bool _warnedAnimatorReflection;

        private enum SequenceState
        {
            Idle,
            RequestFirstAttack,
            WaitFirstAttackEnd,
            RaiseBlock,
            ReleaseBlock,
            RequestSecondAttack,
            HoldPrimary
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private void Awake()
        {
            _instance = this;
            _log = Logger;

            _enabled = Config.Bind("General", "Enabled", true,
                "Enable Mouse5 automatic dagger block-cancel.");
            _requireKnife = Config.Bind("General", "RequireKnife", true,
                "Only activate when the currently equipped weapon uses the Knives skill. If weapon reflection fails after a game update, the mod falls back to allowing the sequence instead of hard-failing.");
            _confirmAnimatorBlock = Config.Bind("General", "ConfirmAnimatorBlock", true,
                "When possible, wait until the actual Animator blocking parameter has switched on before releasing block. Falls back to IsBlocking() if the animator field changes.");
            _verbose = Config.Bind("General", "VerboseLogging", false,
                "Log each state transition to BepInEx/LogOutput.log.");
            _triggerVirtualKey = Config.Bind("Input", "TriggerVirtualKey", VkXButton2,
                "Win32 virtual-key code. 0x06 / decimal 6 is XBUTTON2 (normally Mouse5).");

            // These are only fail-safe timeouts. Successful timing is state-driven, not millisecond-driven.
            _firstAttackStartTimeoutMs = Config.Bind("Failsafe", "FirstAttackStartTimeoutMs", 700,
                "Abort if the first attack cannot start in this many milliseconds.");
            _firstAttackEndTimeoutMs = Config.Bind("Failsafe", "FirstAttackEndTimeoutMs", 1600,
                "Abort if the first attack never reaches its end state.");
            _blockStartTimeoutMs = Config.Bind("Failsafe", "BlockStartTimeoutMs", 500,
                "Abort if blocking cannot become active after the first attack.");
            _secondAttackStartTimeoutMs = Config.Bind("Failsafe", "SecondAttackStartTimeoutMs", 300,
                "Abort if the post-block attack cannot start in this many milliseconds.");

            if (!InitializeReflection())
            {
                Logger.LogError("Could not initialize against Player.SetControls; mod is disabled.");
                _enabled.Value = false;
                return;
            }

            _harmony = new Harmony(PluginGuid);
            var prefix = new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SetControlsPrefix));
            _harmony.Patch(_setControls, prefix: prefix);

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Mouse5/XBUTTON2 state-driven block cancel ready.");
        }

        private void OnDestroy()
        {
            try
            {
                _harmony?.UnpatchSelf();
            }
            catch
            {
                // Ignore shutdown-time patch errors.
            }

            ResetSequence("plugin unload");
            _instance = null;
            _log = null;
        }

        private bool InitializeReflection()
        {
            _playerType = AccessTools.TypeByName("Player");
            if (_playerType == null)
            {
                Logger.LogError("Player type was not found.");
                return false;
            }

            _setControls = _playerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == "SetControls")
                .OrderByDescending(m => m.GetParameters().Length)
                .FirstOrDefault(m => HasControlParameters(m.GetParameters()));

            if (_setControls == null)
            {
                Logger.LogError("Player.SetControls with attack/block arguments was not found.");
                return false;
            }

            var parameters = _setControls.GetParameters();
            _attackIndex = FindParameter(parameters, "attack", 1);
            _attackHoldIndex = FindParameter(parameters, "attackHold", 2);
            _blockIndex = FindParameter(parameters, "block", 5);
            _blockHoldIndex = FindParameter(parameters, "blockHold", 6);

            if (!ValidIndex(_attackIndex, parameters.Length)
                || !ValidIndex(_attackHoldIndex, parameters.Length)
                || !ValidIndex(_blockIndex, parameters.Length)
                || !ValidIndex(_blockHoldIndex, parameters.Length))
            {
                Logger.LogError("Player.SetControls argument layout is incompatible with this build.");
                return false;
            }

            _inAttack = AccessTools.Method(_playerType, "InAttack", Type.EmptyTypes);
            _isBlocking = AccessTools.Method(_playerType, "IsBlocking", Type.EmptyTypes);
            _getCurrentWeapon = AccessTools.Method(_playerType, "GetCurrentWeapon", Type.EmptyTypes);
            _localPlayerField = AccessTools.Field(_playerType, "m_localPlayer");

            if (_inAttack == null || _isBlocking == null)
            {
                Logger.LogError("Required Player state methods InAttack()/IsBlocking() were not found.");
                return false;
            }

            // Optional animation confirmation. Everything here is allowed to fail and fall back.
            _animatorField = FindFieldInHierarchy(_playerType, "m_animator");
            var humanoidType = AccessTools.TypeByName("Humanoid");
            if (humanoidType != null)
                _blockingAnimatorHashField = AccessTools.Field(humanoidType, "s_blocking");

            if (_animatorField != null)
            {
                var animatorType = _animatorField.FieldType;
                _animatorGetBool = animatorType.GetMethod("GetBool", new[] { typeof(int) });
            }

            Logger.LogInfo(
                $"Patched {_playerType.FullName}.{_setControls.Name}({parameters.Length} args); " +
                $"attack={_attackIndex}, attackHold={_attackHoldIndex}, block={_blockIndex}, blockHold={_blockHoldIndex}.");
            return true;
        }

        private static bool HasControlParameters(ParameterInfo[] parameters)
        {
            var names = new HashSet<string>(parameters.Select(p => p.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase);
            if (names.Contains("attack") && names.Contains("attackHold") && names.Contains("block") && names.Contains("blockHold"))
                return true;

            // Known Valheim SetControls layout fallback if metadata parameter names are stripped.
            return parameters.Length >= 7
                && parameters[1].ParameterType == typeof(bool)
                && parameters[2].ParameterType == typeof(bool)
                && parameters[5].ParameterType == typeof(bool)
                && parameters[6].ParameterType == typeof(bool);
        }

        private static int FindParameter(ParameterInfo[] parameters, string name, int fallback)
        {
            for (var i = 0; i < parameters.Length; i++)
            {
                if (string.Equals(parameters[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return fallback;
        }

        private static bool ValidIndex(int index, int length) => index >= 0 && index < length;

        private static FieldInfo FindFieldInHierarchy(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }
            return null;
        }

        private static void SetControlsPrefix(object __instance, object[] __args)
        {
            _instance?.ProcessControls(__instance, __args);
        }

        private void ProcessControls(object player, object[] args)
        {
            if (!_enabled.Value || player == null || args == null)
                return;

            if (!IsLocalPlayer(player))
                return;

            var mouseDown = IsTriggerDown();
            var pressedThisFrame = mouseDown && !_mouseWasDown;
            _mouseWasDown = mouseDown;

            if (_state == SequenceState.Idle)
            {
                if (!pressedThisFrame)
                    return;

                if (_requireKnife.Value && !IsKnifeEquipped(player))
                {
                    DebugLog("Mouse5 ignored: current weapon is not a knife.");
                    return;
                }

                // You can press Mouse5 from idle (the mod starts hit #1), or during a manually-started
                // first swing (the mod arms itself and takes over at the recovery boundary).
                if (InvokeBool(_inAttack, player))
                    SetState(SequenceState.WaitFirstAttackEnd, "armed during an existing first attack");
                else
                    SetState(SequenceState.RequestFirstAttack, "Mouse5 pressed from idle");
            }

            if (_state == SequenceState.Idle)
                return;

            // Hold-mode safety: releasing Mouse5 at any time stops forcing inputs immediately.
            if (!mouseDown)
            {
                ResetSequence("Mouse5 released");
                return;
            }

            // During the sequence, neutralize only primary attack and block; movement, camera,
            // jump, dodge, secondary attack, etc. remain vanilla.
            SetAttack(args, false, false);
            SetBlock(args, false, false);

            switch (_state)
            {
                case SequenceState.RequestFirstAttack:
                    if (InvokeBool(_inAttack, player))
                    {
                        SetState(SequenceState.WaitFirstAttackEnd, "first attack accepted");
                        return;
                    }

                    if (StateElapsedMs() > _firstAttackStartTimeoutMs.Value)
                    {
                        ResetSequence("first attack start timeout");
                        return;
                    }

                    // Request a clean first click every control tick until Valheim accepts it.
                    SetAttack(args, true, false);
                    break;

                case SequenceState.WaitFirstAttackEnd:
                    if (InvokeBool(_inAttack, player))
                    {
                        if (StateElapsedMs() > _firstAttackEndTimeoutMs.Value)
                            ResetSequence("first attack end timeout");
                        return;
                    }

                    // InAttack() has actually dropped: this is the recovery boundary we care about.
                    _blockPressSent = false;
                    SetState(SequenceState.RaiseBlock, "first attack ended; raising block");
                    SetBlock(args, true, true);
                    _blockPressSent = true;
                    break;

                case SequenceState.RaiseBlock:
                {
                    var gameBlocking = InvokeBool(_isBlocking, player);
                    var animatorReady = IsAnimatorBlockingOrUnavailable(player);

                    if (gameBlocking && animatorReady)
                    {
                        // We observed the real block state (and, when available, the Animator's
                        // blocking parameter). Release it on this exact control tick.
                        SetState(SequenceState.ReleaseBlock, "block animation/state observed; releasing block");
                        SetBlock(args, false, false);
                        break;
                    }

                    if (StateElapsedMs() > _blockStartTimeoutMs.Value)
                    {
                        ResetSequence("block start timeout");
                        return;
                    }

                    // Initial tick sends both edge+hold; later ticks only hold.
                    SetBlock(args, !_blockPressSent, true);
                    _blockPressSent = true;
                    break;
                }

                case SequenceState.ReleaseBlock:
                    SetBlock(args, false, false);

                    // Do not guess a delay. Wait until Valheim itself reports block is actually off,
                    // then request the follow-up attack on the first legal control tick.
                    if (!InvokeBool(_isBlocking, player))
                    {
                        SetState(SequenceState.RequestSecondAttack, "blocking fully released");
                        SetAttack(args, true, true);
                    }
                    break;

                case SequenceState.RequestSecondAttack:
                    SetBlock(args, false, false);

                    if (InvokeBool(_inAttack, player))
                    {
                        SetState(SequenceState.HoldPrimary, "post-block attack accepted; holding primary");
                        SetAttack(args, false, true);
                        break;
                    }

                    if (StateElapsedMs() > _secondAttackStartTimeoutMs.Value)
                    {
                        ResetSequence("second attack start timeout");
                        return;
                    }

                    // Re-request every frame until the exact first frame the engine permits it.
                    // This removes the human/FPS-dependent timing guess.
                    SetAttack(args, true, true);
                    break;

                case SequenceState.HoldPrimary:
                    // Equivalent to keeping LMB held after the successful block cancel.
                    SetBlock(args, false, false);
                    SetAttack(args, false, true);
                    break;
            }
        }

        private bool IsLocalPlayer(object player)
        {
            if (_localPlayerField == null)
                return true;

            try
            {
                var local = _localPlayerField.GetValue(null);
                return local == null || ReferenceEquals(local, player);
            }
            catch
            {
                return true;
            }
        }

        private bool IsTriggerDown()
        {
            try
            {
                return (GetAsyncKeyState(_triggerVirtualKey.Value) & KeyDownMask) != 0;
            }
            catch
            {
                return false;
            }
        }

        private bool IsKnifeEquipped(object player)
        {
            if (_getCurrentWeapon == null)
                return true; // compatibility fallback

            try
            {
                var weapon = _getCurrentWeapon.Invoke(player, null);
                if (weapon == null)
                    return false;

                var sharedField = FindFieldInHierarchy(weapon.GetType(), "m_shared");
                if (sharedField == null)
                    return WeaponReflectionFallback("m_shared field not found");

                var shared = sharedField.GetValue(weapon);
                if (shared == null)
                    return WeaponReflectionFallback("m_shared was null");

                var skillField = FindFieldInHierarchy(shared.GetType(), "m_skillType");
                if (skillField == null)
                    return WeaponReflectionFallback("m_skillType field not found");

                var skill = skillField.GetValue(shared);
                if (skill == null)
                    return WeaponReflectionFallback("m_skillType was null");

                var skillName = skill.ToString() ?? string.Empty;
                return skillName.IndexOf("Kniv", StringComparison.OrdinalIgnoreCase) >= 0
                    || skillName.IndexOf("Dagger", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch (Exception ex)
            {
                return WeaponReflectionFallback(ex.GetType().Name + ": " + ex.Message);
            }
        }

        private bool WeaponReflectionFallback(string reason)
        {
            if (!_warnedWeaponReflection)
            {
                _warnedWeaponReflection = true;
                Logger.LogWarning("Could not verify knife skill after game update (" + reason + "). Allowing Mouse5 sequence as compatibility fallback.");
            }
            return true;
        }

        private bool IsAnimatorBlockingOrUnavailable(object player)
        {
            if (!_confirmAnimatorBlock.Value)
                return true;

            if (_animatorField == null || _blockingAnimatorHashField == null || _animatorGetBool == null)
                return AnimatorReflectionFallback("animator reflection unavailable");

            try
            {
                var animator = _animatorField.GetValue(player);
                var hashValue = _blockingAnimatorHashField.GetValue(null);
                if (animator == null || hashValue == null)
                    return AnimatorReflectionFallback("animator/hash was null");

                var blocking = _animatorGetBool.Invoke(animator, new[] { hashValue });
                return blocking is bool b && b;
            }
            catch (Exception ex)
            {
                return AnimatorReflectionFallback(ex.GetType().Name + ": " + ex.Message);
            }
        }

        private bool AnimatorReflectionFallback(string reason)
        {
            if (!_warnedAnimatorReflection)
            {
                _warnedAnimatorReflection = true;
                Logger.LogWarning("Animator block confirmation unavailable (" + reason + "). Falling back to Player.IsBlocking().");
            }
            return true;
        }

        private static bool InvokeBool(MethodInfo method, object instance)
        {
            if (method == null || instance == null)
                return false;

            try
            {
                var result = method.Invoke(instance, null);
                return result is bool b && b;
            }
            catch (Exception ex)
            {
                _log?.LogWarning("State reflection failed: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private void SetAttack(object[] args, bool press, bool hold)
        {
            args[_attackIndex] = press;
            args[_attackHoldIndex] = hold;
        }

        private void SetBlock(object[] args, bool press, bool hold)
        {
            args[_blockIndex] = press;
            args[_blockHoldIndex] = hold;
        }

        private void SetState(SequenceState next, string reason)
        {
            _state = next;
            _stateStartedTicks = Stopwatch.GetTimestamp();
            DebugLog(next + " <- " + reason);
        }

        private double StateElapsedMs()
        {
            if (_stateStartedTicks == 0)
                return 0;
            return (Stopwatch.GetTimestamp() - _stateStartedTicks) * 1000.0 / Stopwatch.Frequency;
        }

        private void ResetSequence(string reason)
        {
            if (_state != SequenceState.Idle)
                DebugLog("Idle <- " + reason);

            _state = SequenceState.Idle;
            _stateStartedTicks = 0;
            _blockPressSent = false;
        }

        private void DebugLog(string message)
        {
            if (_verbose != null && _verbose.Value)
                Logger.LogInfo("[DPC] " + message);
        }
    }
}
