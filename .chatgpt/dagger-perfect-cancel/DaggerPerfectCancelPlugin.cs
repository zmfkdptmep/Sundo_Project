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
using UnityEngine;

namespace Goni.DaggerPerfectCancel
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class DaggerPerfectCancelPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "goni.valheim.daggerperfectcancel";
        public const string PluginName = "Goni Dagger Perfect Cancel";
        public const string PluginVersion = "1.1.0";

        private const int VkXButton1 = 0x05;
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
        private FieldInfo _queuedAttackTimerField;

        private int _attackIndex = -1;
        private int _attackHoldIndex = -1;
        private int _blockIndex = -1;
        private int _blockHoldIndex = -1;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<bool> _requireKnife;
        private ConfigEntry<bool> _confirmAnimatorBlock;
        private ConfigEntry<bool> _verbose;
        private ConfigEntry<bool> _acceptEitherSideButton;
        private ConfigEntry<int> _triggerVirtualKey;
        private ConfigEntry<float> _queuedAttackSeconds;
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
        private bool _warnedQueueReflection;
        private bool _unityMouse5Down;
        private bool _unityMouse4Down;
        private string _lastTriggerSource = string.Empty;

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
                "Only activate when the equipped weapon uses the Knives skill.");
            _confirmAnimatorBlock = Config.Bind("General", "ConfirmAnimatorBlock", true,
                "Wait for the actual Animator blocking flag before releasing block when available.");
            _verbose = Config.Bind("General", "VerboseLogging", false,
                "Log every state transition.");
            _acceptEitherSideButton = Config.Bind("Input", "AcceptEitherSideButtonFallback", true,
                "Also accept the other side mouse button as a fallback. Useful because mouse software can swap XBUTTON1/XBUTTON2 naming.");
            _triggerVirtualKey = Config.Bind("Input", "TriggerVirtualKey", VkXButton2,
                "Primary Win32 virtual-key. Decimal 6 is XBUTTON2, normally Mouse5.");
            _queuedAttackSeconds = Config.Bind("Timing", "QueuedAttackSeconds", 0.25f,
                "How long to keep the post-block primary attack queued internally. This is not an animation delay; it ensures Valheim consumes the attack on the first legal tick.");

            _firstAttackStartTimeoutMs = Config.Bind("Failsafe", "FirstAttackStartTimeoutMs", 700,
                "Abort if first attack cannot start.");
            _firstAttackEndTimeoutMs = Config.Bind("Failsafe", "FirstAttackEndTimeoutMs", 1800,
                "Abort if first attack never ends.");
            _blockStartTimeoutMs = Config.Bind("Failsafe", "BlockStartTimeoutMs", 700,
                "Abort if block never becomes active.");
            _secondAttackStartTimeoutMs = Config.Bind("Failsafe", "SecondAttackStartTimeoutMs", 600,
                "Abort if the post-block attack cannot start.");

            if (!InitializeReflection())
            {
                Logger.LogError("Could not initialize against Player.SetControls; mod disabled.");
                _enabled.Value = false;
                return;
            }

            _harmony = new Harmony(PluginGuid);
            _harmony.Patch(_setControls,
                prefix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SetControlsPrefix)));

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Mouse5 multi-source input + state-driven block cancel ready.");
        }

        private void Update()
        {
            // Unity names mouse buttons zero-based: Mouse4 is the fifth mouse button (Mouse5 in
            // common mouse-software/UI naming), Mouse3 is the fourth. Cache them on the Unity
            // Update thread and consume in the SetControls patch.
            try
            {
                _unityMouse5Down = Input.GetKey(KeyCode.Mouse4);
                _unityMouse4Down = Input.GetKey(KeyCode.Mouse3);
            }
            catch
            {
                _unityMouse5Down = false;
                _unityMouse4Down = false;
            }
        }

        private void OnDestroy()
        {
            try { _harmony?.UnpatchSelf(); } catch { }
            ResetSequence("plugin unload");
            _instance = null;
            _log = null;
        }

        private bool InitializeReflection()
        {
            _playerType = AccessTools.TypeByName("Player");
            if (_playerType == null)
                return false;

            _setControls = _playerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == "SetControls")
                .OrderByDescending(m => m.GetParameters().Length)
                .FirstOrDefault(m => HasControlParameters(m.GetParameters()));
            if (_setControls == null)
                return false;

            var parameters = _setControls.GetParameters();
            _attackIndex = FindParameter(parameters, "attack", 1);
            _attackHoldIndex = FindParameter(parameters, "attackHold", 2);
            _blockIndex = FindParameter(parameters, "block", 5);
            _blockHoldIndex = FindParameter(parameters, "blockHold", 6);
            if (!ValidIndex(_attackIndex, parameters.Length) ||
                !ValidIndex(_attackHoldIndex, parameters.Length) ||
                !ValidIndex(_blockIndex, parameters.Length) ||
                !ValidIndex(_blockHoldIndex, parameters.Length))
                return false;

            _inAttack = AccessTools.Method(_playerType, "InAttack", Type.EmptyTypes);
            _isBlocking = AccessTools.Method(_playerType, "IsBlocking", Type.EmptyTypes);
            _getCurrentWeapon = AccessTools.Method(_playerType, "GetCurrentWeapon", Type.EmptyTypes);
            _localPlayerField = AccessTools.Field(_playerType, "m_localPlayer");
            _queuedAttackTimerField = FindFieldInHierarchy(_playerType, "m_queuedAttackTimer");
            if (_inAttack == null || _isBlocking == null)
                return false;

            _animatorField = FindFieldInHierarchy(_playerType, "m_animator");
            var humanoidType = AccessTools.TypeByName("Humanoid");
            if (humanoidType != null)
                _blockingAnimatorHashField = AccessTools.Field(humanoidType, "s_blocking");
            if (_animatorField != null)
                _animatorGetBool = _animatorField.FieldType.GetMethod("GetBool", new[] { typeof(int) });

            Logger.LogInfo(
                $"Patched {_playerType.FullName}.{_setControls.Name}({parameters.Length} args); " +
                $"attack={_attackIndex}, attackHold={_attackHoldIndex}, block={_blockIndex}, blockHold={_blockHoldIndex}, " +
                $"queuedAttackTimer={(_queuedAttackTimerField != null ? "found" : "missing")}.");
            return true;
        }

        private static bool HasControlParameters(ParameterInfo[] parameters)
        {
            var names = new HashSet<string>(parameters.Select(p => p.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase);
            if (names.Contains("attack") && names.Contains("attackHold") && names.Contains("block") && names.Contains("blockHold"))
                return true;
            return parameters.Length >= 7 && parameters[1].ParameterType == typeof(bool) &&
                parameters[2].ParameterType == typeof(bool) && parameters[5].ParameterType == typeof(bool) &&
                parameters[6].ParameterType == typeof(bool);
        }

        private static int FindParameter(ParameterInfo[] parameters, string name, int fallback)
        {
            for (var i = 0; i < parameters.Length; i++)
                if (string.Equals(parameters[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return fallback;
        }

        private static bool ValidIndex(int index, int length) => index >= 0 && index < length;

        private static FieldInfo FindFieldInHierarchy(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name, BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
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
            if (!_enabled.Value || player == null || args == null || !IsLocalPlayer(player))
                return;

            var mouseDown = IsTriggerDown(out var triggerSource);
            var pressedThisFrame = mouseDown && !_mouseWasDown;
            _mouseWasDown = mouseDown;

            if (pressedThisFrame)
            {
                _lastTriggerSource = triggerSource;
                Logger.LogInfo("[DPC] Mouse5 trigger detected via " + triggerSource + ".");
            }

            if (_state == SequenceState.Idle)
            {
                if (!pressedThisFrame)
                    return;

                if (_requireKnife.Value && !IsKnifeEquipped(player))
                {
                    Logger.LogWarning("[DPC] Trigger detected, but equipped weapon is not a knife. Sequence ignored.");
                    return;
                }

                if (InvokeBool(_inAttack, player))
                    SetState(SequenceState.WaitFirstAttackEnd, "armed during existing attack");
                else
                    SetState(SequenceState.RequestFirstAttack, "trigger from idle");
            }

            if (_state == SequenceState.Idle)
                return;

            if (!mouseDown)
            {
                ResetSequence("Mouse5 released");
                return;
            }

            SetAttack(args, false, false);
            SetBlock(args, false, false);

            switch (_state)
            {
                case SequenceState.RequestFirstAttack:
                    if (InvokeBool(_inAttack, player))
                    {
                        SetState(SequenceState.WaitFirstAttackEnd, "first attack accepted");
                        break;
                    }
                    if (StateElapsedMs() > _firstAttackStartTimeoutMs.Value)
                    {
                        ResetSequence("first attack start timeout");
                        break;
                    }
                    // A physical click is both edge + held on its first frame.
                    SetAttack(args, true, true);
                    break;

                case SequenceState.WaitFirstAttackEnd:
                    if (InvokeBool(_inAttack, player))
                    {
                        if (StateElapsedMs() > _firstAttackEndTimeoutMs.Value)
                            ResetSequence("first attack end timeout");
                        break;
                    }
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
                        SetState(SequenceState.ReleaseBlock, "real block state/animation observed");
                        SetBlock(args, false, false);
                        QueuePrimaryAttack(player);
                        break;
                    }
                    if (StateElapsedMs() > _blockStartTimeoutMs.Value)
                    {
                        ResetSequence("block start timeout");
                        break;
                    }
                    SetBlock(args, !_blockPressSent, true);
                    _blockPressSent = true;
                    break;
                }

                case SequenceState.ReleaseBlock:
                    SetBlock(args, false, false);
                    QueuePrimaryAttack(player);
                    if (!InvokeBool(_isBlocking, player))
                    {
                        SetState(SequenceState.RequestSecondAttack, "blocking fully released");
                        SetAttack(args, true, true);
                    }
                    break;

                case SequenceState.RequestSecondAttack:
                    SetBlock(args, false, false);
                    QueuePrimaryAttack(player);
                    if (InvokeBool(_inAttack, player))
                    {
                        SetState(SequenceState.HoldPrimary, "post-block attack accepted");
                        SetAttack(args, false, true);
                        break;
                    }
                    if (StateElapsedMs() > _secondAttackStartTimeoutMs.Value)
                    {
                        ResetSequence("second attack start timeout");
                        break;
                    }
                    SetAttack(args, true, true);
                    break;

                case SequenceState.HoldPrimary:
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
            catch { return true; }
        }

        private bool IsTriggerDown(out string source)
        {
            source = string.Empty;

            // Preferred: Unity input for the fifth mouse button.
            if (_unityMouse5Down)
            {
                source = "Unity KeyCode.Mouse4 (5th mouse button)";
                return true;
            }

            try
            {
                if ((GetAsyncKeyState(_triggerVirtualKey.Value) & KeyDownMask) != 0)
                {
                    source = "Win32 VK 0x" + _triggerVirtualKey.Value.ToString("X2");
                    return true;
                }
            }
            catch { }

            if (_acceptEitherSideButton.Value)
            {
                if (_unityMouse4Down)
                {
                    source = "Unity KeyCode.Mouse3 fallback";
                    return true;
                }

                try
                {
                    var alternate = _triggerVirtualKey.Value == VkXButton1 ? VkXButton2 : VkXButton1;
                    if ((GetAsyncKeyState(alternate) & KeyDownMask) != 0)
                    {
                        source = "Win32 alternate VK 0x" + alternate.ToString("X2");
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }

        private void QueuePrimaryAttack(object player)
        {
            if (_queuedAttackTimerField == null)
            {
                if (!_warnedQueueReflection)
                {
                    _warnedQueueReflection = true;
                    Logger.LogWarning("[DPC] m_queuedAttackTimer not found; relying on held attack input only.");
                }
                return;
            }

            try
            {
                var current = (float)_queuedAttackTimerField.GetValue(player);
                var target = Mathf.Max(current, _queuedAttackSeconds.Value);
                _queuedAttackTimerField.SetValue(player, target);
            }
            catch (Exception ex)
            {
                if (!_warnedQueueReflection)
                {
                    _warnedQueueReflection = true;
                    Logger.LogWarning("[DPC] Could not set m_queuedAttackTimer: " + ex.Message);
                }
            }
        }

        private bool IsKnifeEquipped(object player)
        {
            if (_getCurrentWeapon == null)
                return true;
            try
            {
                var weapon = _getCurrentWeapon.Invoke(player, null);
                if (weapon == null)
                    return false;
                var sharedField = FindFieldInHierarchy(weapon.GetType(), "m_shared");
                if (sharedField == null)
                    return WeaponReflectionFallback("m_shared missing");
                var shared = sharedField.GetValue(weapon);
                if (shared == null)
                    return WeaponReflectionFallback("m_shared null");
                var skillField = FindFieldInHierarchy(shared.GetType(), "m_skillType");
                if (skillField == null)
                    return WeaponReflectionFallback("m_skillType missing");
                var skill = skillField.GetValue(shared);
                var skillName = skill?.ToString() ?? string.Empty;
                return skillName.IndexOf("Kniv", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       skillName.IndexOf("Dagger", StringComparison.OrdinalIgnoreCase) >= 0;
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
                Logger.LogWarning("[DPC] Could not verify knife skill (" + reason + "); allowing sequence as fallback.");
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
                    return AnimatorReflectionFallback("animator/hash null");
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
                Logger.LogWarning("[DPC] Animator confirmation unavailable (" + reason + "); using IsBlocking only.");
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
                _log?.LogWarning("[DPC] State reflection failed: " + ex.GetType().Name + ": " + ex.Message);
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
