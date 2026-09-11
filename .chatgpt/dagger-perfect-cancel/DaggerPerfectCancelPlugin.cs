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
        public const string PluginName = "Goni True Extended Block Cancel";
        public const string PluginVersion = "2.1.0";

        private const int VkXButton1 = 0x05;
        private const int VkXButton2 = 0x06;
        private const int KeyDownMask = 0x8000;

        private static DaggerPerfectCancelPlugin _instance;
        private static ManualLogSource _log;

        private Harmony _harmony;
        private Type _playerType;
        private Type _humanoidType;
        private Type _attackType;

        private FieldInfo _localPlayerField;
        private FieldInfo _currentAttackField;
        private FieldInfo _previousAttackField;
        private FieldInfo _timeSinceLastAttackField;
        private FieldInfo _attackNextChainField;
        private FieldInfo _attackChainLevelsField;
        private FieldInfo _attackCharacterField;
        private FieldInfo _blockingInputField;

        private MethodInfo _setControlsMethod;
        private MethodInfo _startAttackMethod;
        private MethodInfo _getCurrentWeaponMethod;
        private MethodInfo _inAttackMethod;
        private MethodInfo _isBlockingMethod;
        private MethodInfo _attackTriggerMethod;
        private MethodInfo _attackIsDoneMethod;

        private int _attackIndex = -1;
        private int _attackHoldIndex = -1;
        private int _blockIndex = -1;
        private int _blockHoldIndex = -1;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<int> _triggerVirtualKey;
        private ConfigEntry<bool> _acceptEitherSideButton;
        private ConfigEntry<int> _blockVisibleFrames;
        private ConfigEntry<float> _bridgeRetryInterval;
        private ConfigEntry<float> _bridgeTimeout;
        private ConfigEntry<bool> _verbose;

        private CycleState _state = CycleState.Idle;
        private object _player;
        private object _firstAttack;
        private object _bridgeAttack;
        private bool _firstHitSeen;
        private bool _mouseWasDown;
        private int _blockObservedFrame = -1;
        private float _stateStarted;
        private float _nextBridgeAttempt;
        private bool _warnedChainReflection;

        private enum CycleState
        {
            Idle,
            FirstAttack,
            RaiseRealBlock,
            StartExtendedBridge,
            HoldVanillaCombo
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private void Awake()
        {
            _instance = this;
            _log = Logger;

            _enabled = Config.Bind("General", "Enabled", true,
                "Hold Mouse5 to force the real Valheim block-carry/extended-combo sequence.");
            _triggerVirtualKey = Config.Bind("Input", "TriggerVirtualKey", VkXButton2,
                "Win32 virtual-key code. 6 / 0x06 is XBUTTON2 (Mouse5 on this PC).");
            _acceptEitherSideButton = Config.Bind("Input", "AcceptEitherSideButtonFallback", false,
                "Also accept XBUTTON1. Disabled by default because the supplied runtime log confirms Mouse5 is VK 0x06.");
            _blockVisibleFrames = Config.Bind("Visual", "BlockVisibleFrames", 1,
                "Keep the real blocking state visible for this many rendered frames after IsBlocking() becomes true.");
            _bridgeRetryInterval = Config.Bind("Safety", "BridgeRetryInterval", 0.01f,
                "Retry interval if Valheim temporarily refuses the post-block combo attack.");
            _bridgeTimeout = Config.Bind("Safety", "BridgeTimeout", 1.0f,
                "Give up and fall back to held primary if the post-block bridge cannot start in this many seconds.");
            _verbose = Config.Bind("Debug", "VerboseLogging", false,
                "Log every state transition and chain level.");

            if (!BindGameMembers())
            {
                Logger.LogError("[TEC] Required Valheim 1.0 combat members were not found. Mod disabled.");
                _enabled.Value = false;
                return;
            }

            _harmony = new Harmony(PluginGuid);
            _harmony.Patch(_setControlsMethod,
                prefix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SetControlsPrefix)));
            _harmony.Patch(_attackTriggerMethod,
                postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(AttackTriggerPostfix)));

            Logger.LogInfo(PluginName + " " + PluginVersion +
                " loaded. Mouse5 = first primary -> real block startup -> preserved combo carry -> vanilla primary hold.");
        }

        private void OnDestroy()
        {
            try { StopAutomation("plugin unload"); } catch { }
            try { _harmony?.UnpatchSelf(); } catch { }
            _instance = null;
            _log = null;
        }

        private void Update()
        {
            if (_enabled == null || !_enabled.Value)
            {
                if (_state != CycleState.Idle)
                    StopAutomation("disabled");
                return;
            }

            var down = IsTriggerDown(out var source);
            var pressed = down && !_mouseWasDown;
            _mouseWasDown = down;

            if (!down)
            {
                if (_state != CycleState.Idle)
                    StopAutomation("Mouse5 released");
                return;
            }

            var local = GetLocalPlayer();
            if (local == null)
                return;

            if (pressed)
                Logger.LogInfo("[TEC] Mouse5 detected via " + source + ".");

            if (_state == CycleState.Idle)
            {
                _player = local;
                BeginFirstAttack(local);
                return;
            }

            if (!ReferenceEquals(local, _player))
            {
                StopAutomation("local player changed");
                return;
            }

            switch (_state)
            {
                case CycleState.FirstAttack:
                    UpdateFirstAttack();
                    break;

                case CycleState.RaiseRealBlock:
                    UpdateRealBlock();
                    break;

                case CycleState.StartExtendedBridge:
                    UpdateExtendedBridge();
                    break;

                case CycleState.HoldVanillaCombo:
                    // Nothing to manufacture here. Player.SetControls is patched to supply only
                    // attackHold=true. From this point onward Valheim's own combo system owns all
                    // attacks, animation events, stamina use, multi-hit finishers and chain damage.
                    break;
            }
        }

        private bool BindGameMembers()
        {
            _playerType = AccessTools.TypeByName("Player");
            _humanoidType = AccessTools.TypeByName("Humanoid");
            _attackType = AccessTools.TypeByName("Attack");
            if (_playerType == null || _humanoidType == null || _attackType == null)
                return false;

            _localPlayerField = AccessTools.Field(_playerType, "m_localPlayer");
            _currentAttackField = FindField(_humanoidType, "m_currentAttack");
            _previousAttackField = FindField(_humanoidType, "m_previousAttack");
            _timeSinceLastAttackField = FindField(_humanoidType, "m_timeSinceLastAttack");
            _blockingInputField = FindField(_playerType, "m_blocking") ?? FindField(_humanoidType, "m_blocking");

            _attackNextChainField = FindField(_attackType, "m_nextAttackChainLevel");
            _attackChainLevelsField = FindField(_attackType, "m_attackChainLevels");
            _attackCharacterField = FindField(_attackType, "m_character");

            _getCurrentWeaponMethod = AccessTools.Method(_humanoidType, "GetCurrentWeapon", Type.EmptyTypes);
            _inAttackMethod = AccessTools.Method(_humanoidType, "InAttack", Type.EmptyTypes);
            _isBlockingMethod = AccessTools.Method(_humanoidType, "IsBlocking", Type.EmptyTypes);
            _attackTriggerMethod = AccessTools.Method(_attackType, "OnAttackTrigger", Type.EmptyTypes);
            _attackIsDoneMethod = AccessTools.Method(_attackType, "IsDone", Type.EmptyTypes);

            _startAttackMethod = _humanoidType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "StartAttack") return false;
                    var p = m.GetParameters();
                    return p.Length == 2 && p[1].ParameterType == typeof(bool);
                });

            _setControlsMethod = _playerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == "SetControls")
                .OrderByDescending(m => m.GetParameters().Length)
                .FirstOrDefault(m => HasControlLayout(m.GetParameters()));

            if (_setControlsMethod != null)
            {
                var p = _setControlsMethod.GetParameters();
                _attackIndex = FindParameter(p, "attack", 1);
                _attackHoldIndex = FindParameter(p, "attackHold", 2);
                _blockIndex = FindParameter(p, "block", 5);
                _blockHoldIndex = FindParameter(p, "blockHold", 6);
            }

            var ok = _localPlayerField != null && _currentAttackField != null &&
                     _previousAttackField != null && _timeSinceLastAttackField != null &&
                     _getCurrentWeaponMethod != null && _inAttackMethod != null &&
                     _isBlockingMethod != null && _startAttackMethod != null &&
                     _setControlsMethod != null && _attackTriggerMethod != null &&
                     ValidIndex(_attackIndex) && ValidIndex(_attackHoldIndex) &&
                     ValidIndex(_blockIndex) && ValidIndex(_blockHoldIndex);

            Logger.LogInfo("[TEC] Hooks: SetControls=" + Found(_setControlsMethod) +
                ", StartAttack=" + Found(_startAttackMethod) +
                ", currentAttack=" + Found(_currentAttackField) +
                ", previousAttack=" + Found(_previousAttackField) +
                ", nextChain=" + Found(_attackNextChainField) +
                ", chainLevels=" + Found(_attackChainLevelsField) +
                ", IsBlocking=" + Found(_isBlockingMethod) + ".");
            return ok;
        }

        private void BeginFirstAttack(object player)
        {
            _firstHitSeen = false;
            _blockObservedFrame = -1;
            _firstAttack = GetCurrentAttack(player);

            // Mouse5 can be pressed during a manually-started primary. If there is no attack,
            // start hit #1 directly instead of relying on a synthetic button edge.
            if (_firstAttack == null || IsAttackDone(_firstAttack))
            {
                if (!TryStartPrimary(player))
                {
                    // Keep Idle and try again next Update while Mouse5 remains held.
                    _state = CycleState.Idle;
                    return;
                }
                _firstAttack = GetCurrentAttack(player);
            }

            if (_firstAttack == null)
                return;

            _state = CycleState.FirstAttack;
            _stateStarted = Time.realtimeSinceStartup;
            DebugLog("first attack armed");
        }

        private void UpdateFirstAttack()
        {
            if (_firstAttack == null)
            {
                StopAutomation("first attack disappeared");
                return;
            }

            // Do NOT stop/fast-forward this attack. A real extended combo waits until the first
            // swing naturally returns to the point where blocking can begin. The hit event is
            // tracked only so we never cancel before the real attack event happened.
            if (!_firstHitSeen)
                return;

            if (InvokeBool(_inAttackMethod, _player))
                return;

            EnsureFirstAttackCarriesChain();
            _state = CycleState.RaiseRealBlock;
            _stateStarted = Time.realtimeSinceStartup;
            _blockObservedFrame = -1;
            SetBlockingInputField(true);
            DebugLog("first swing left attack state; requesting real block");
        }

        private void UpdateRealBlock()
        {
            SetBlockingInputField(true);

            var blocking = InvokeBool(_isBlockingMethod, _player);
            if (!blocking)
                return;

            if (_blockObservedFrame < 0)
            {
                _blockObservedFrame = Time.frameCount;
                DebugLog("IsBlocking=true; block startup reached screen path");
                return;
            }

            if (Time.frameCount - _blockObservedFrame < Mathf.Max(1, _blockVisibleFrames.Value))
                return;

            // This is the exact bridge: the first Attack object and its m_nextAttackChainLevel
            // survive across the block. Do not reset/clone it ourselves.
            SetBlockingInputField(false);
            PreserveFirstAttackAsPrevious();
            _state = CycleState.StartExtendedBridge;
            _stateStarted = Time.realtimeSinceStartup;
            _nextBridgeAttempt = 0f;
            DebugLog("block visibly entered; releasing and bridging into chain hit #2");
        }

        private void UpdateExtendedBridge()
        {
            if (Time.realtimeSinceStartup < _nextBridgeAttempt)
                return;

            SetBlockingInputField(false);
            PreserveFirstAttackAsPrevious();

            if (TryStartPrimary(_player))
            {
                _bridgeAttack = GetCurrentAttack(_player);
                _state = CycleState.HoldVanillaCombo;
                _stateStarted = Time.realtimeSinceStartup;
                DebugLog("bridge attack accepted; handing control to vanilla attackHold");
                return;
            }

            if (Time.realtimeSinceStartup - _stateStarted > Mathf.Max(0.2f, _bridgeTimeout.Value))
            {
                // Safer fallback: never spin StartAttack forever. Keep Mouse5 behaving as held LMB.
                _state = CycleState.HoldVanillaCombo;
                DebugLog("bridge timeout; falling back to vanilla hold");
                return;
            }

            _nextBridgeAttempt = Time.realtimeSinceStartup + Mathf.Max(0.005f, _bridgeRetryInterval.Value);
        }

        private void EnsureFirstAttackCarriesChain()
        {
            if (_firstAttack == null || _attackNextChainField == null)
                return;

            try
            {
                var next = (int)_attackNextChainField.GetValue(_firstAttack);
                var levels = _attackChainLevelsField != null ? (int)_attackChainLevelsField.GetValue(_firstAttack) : 0;

                // Attack.Update normally sets next=current+1 when the attack animation first enters.
                // If another mod or a frame edge left it at zero, force only the expected first->second
                // carry for weapons that actually declare a combo chain. This is the only chain write.
                if (levels > 1 && next == 0)
                {
                    _attackNextChainField.SetValue(_firstAttack, 1);
                    next = 1;
                }
                DebugLog("first attack carry: nextChain=" + next + ", chainLevels=" + levels);
            }
            catch (Exception ex)
            {
                if (!_warnedChainReflection)
                {
                    _warnedChainReflection = true;
                    Logger.LogWarning("[TEC] Could not inspect first attack chain state: " + ex.Message);
                }
            }
        }

        private void PreserveFirstAttackAsPrevious()
        {
            if (_player == null || _firstAttack == null)
                return;

            try
            {
                EnsureFirstAttackCarriesChain();
                _previousAttackField.SetValue(_player, _firstAttack);
                _timeSinceLastAttackField.SetValue(_player, 0f);
            }
            catch (Exception ex)
            {
                if (!_warnedChainReflection)
                {
                    _warnedChainReflection = true;
                    Logger.LogWarning("[TEC] Could not preserve combo carry: " + ex.Message);
                }
            }
        }

        private bool TryStartPrimary(object player)
        {
            if (player == null)
                return false;

            try
            {
                var weapon = _getCurrentWeaponMethod.Invoke(player, null);
                if (weapon == null)
                    return false;
                var result = _startAttackMethod.Invoke(player, new object[] { null, false });
                return result is bool b && b;
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                Logger.LogWarning("[TEC] StartAttack failed: " + inner.GetType().Name + ": " + inner.Message);
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[TEC] StartAttack reflection failed: " + ex.Message);
                return false;
            }
        }

        private static void SetControlsPrefix(object __instance, object[] __args)
        {
            _instance?.ModifyControls(__instance, __args);
        }

        private void ModifyControls(object player, object[] args)
        {
            if (_state == CycleState.Idle || player == null || args == null || !ReferenceEquals(player, _player))
                return;

            if (!IsTriggerDown(out _))
                return;

            try
            {
                if (_state == CycleState.RaiseRealBlock)
                {
                    args[_attackIndex] = false;
                    args[_attackHoldIndex] = false;
                    args[_blockIndex] = true;
                    args[_blockHoldIndex] = true;
                }
                else if (_state == CycleState.StartExtendedBridge)
                {
                    args[_blockIndex] = false;
                    args[_blockHoldIndex] = false;
                    args[_attackIndex] = false;
                    args[_attackHoldIndex] = false;
                }
                else if (_state == CycleState.HoldVanillaCombo)
                {
                    // Crucial difference from 2.0: no more manual Stop/Start per hit.
                    // This is literally the equivalent of holding LMB after the successful bridge.
                    args[_blockIndex] = false;
                    args[_blockHoldIndex] = false;
                    args[_attackHoldIndex] = true;
                }
            }
            catch { }
        }

        private static void AttackTriggerPostfix(object __instance)
        {
            _instance?.OnAttackTrigger(__instance);
        }

        private void OnAttackTrigger(object attack)
        {
            if (_state != CycleState.FirstAttack || _firstAttack == null || !ReferenceEquals(attack, _firstAttack))
                return;

            if (!IsTriggerDown(out _))
                return;

            _firstHitSeen = true;
            DebugLog("real first OnAttackTrigger observed");
        }

        private void SetBlockingInputField(bool value)
        {
            if (_player == null || _blockingInputField == null)
                return;
            try { _blockingInputField.SetValue(_player, value); } catch { }
        }

        private void StopAutomation(string reason)
        {
            SetBlockingInputField(false);
            DebugLog("Idle <- " + reason);
            _state = CycleState.Idle;
            _player = null;
            _firstAttack = null;
            _bridgeAttack = null;
            _firstHitSeen = false;
            _blockObservedFrame = -1;
            _stateStarted = 0f;
            _nextBridgeAttempt = 0f;
        }

        private object GetLocalPlayer()
        {
            try { return _localPlayerField.GetValue(null); }
            catch { return null; }
        }

        private object GetCurrentAttack(object player)
        {
            try { return _currentAttackField.GetValue(player); }
            catch { return null; }
        }

        private bool IsAttackDone(object attack)
        {
            if (attack == null || _attackIsDoneMethod == null)
                return attack == null;
            try
            {
                var v = _attackIsDoneMethod.Invoke(attack, null);
                return v is bool b && b;
            }
            catch { return false; }
        }

        private bool IsTriggerDown(out string source)
        {
            source = string.Empty;
            try
            {
                if ((GetAsyncKeyState(_triggerVirtualKey.Value) & KeyDownMask) != 0)
                {
                    source = "Win32 VK 0x" + _triggerVirtualKey.Value.ToString("X2");
                    return true;
                }
                if (_acceptEitherSideButton.Value)
                {
                    var alternate = _triggerVirtualKey.Value == VkXButton1 ? VkXButton2 : VkXButton1;
                    if ((GetAsyncKeyState(alternate) & KeyDownMask) != 0)
                    {
                        source = "Win32 alternate VK 0x" + alternate.ToString("X2");
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static bool InvokeBool(MethodInfo method, object obj)
        {
            if (method == null || obj == null)
                return false;
            try
            {
                var v = method.Invoke(obj, null);
                return v is bool b && b;
            }
            catch { return false; }
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null) return f;
            }
            return null;
        }

        private static bool HasControlLayout(ParameterInfo[] p)
        {
            var names = new HashSet<string>(p.Select(x => x.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase);
            if (names.Contains("attack") && names.Contains("attackHold") && names.Contains("block") && names.Contains("blockHold"))
                return true;
            return p.Length >= 7 && p[1].ParameterType == typeof(bool) && p[2].ParameterType == typeof(bool) &&
                   p[5].ParameterType == typeof(bool) && p[6].ParameterType == typeof(bool);
        }

        private static int FindParameter(ParameterInfo[] p, string name, int fallback)
        {
            for (var i = 0; i < p.Length; ++i)
                if (string.Equals(p[i].Name, name, StringComparison.OrdinalIgnoreCase)) return i;
            return fallback;
        }

        private bool ValidIndex(int i) => i >= 0 && _setControlsMethod != null && i < _setControlsMethod.GetParameters().Length;
        private static string Found(MemberInfo m) => m != null ? "found" : "missing";

        private void DebugLog(string message)
        {
            if (_verbose != null && _verbose.Value)
                Logger.LogInfo("[TEC] " + message);
        }
    }
}
