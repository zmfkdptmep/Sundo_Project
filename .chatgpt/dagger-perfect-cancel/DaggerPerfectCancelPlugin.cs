using System;
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
        public const string PluginName = "Goni Universal Perfect Block Cancel";
        public const string PluginVersion = "2.0.0";

        private const int VkXButton1 = 0x05;
        private const int VkXButton2 = 0x06;
        private const int KeyDownMask = 0x8000;

        private static DaggerPerfectCancelPlugin _instance;
        private static ManualLogSource _log;

        private Harmony _harmony;

        private Type _playerType;
        private Type _humanoidType;
        private Type _attackType;
        private Type _zsyncAnimationType;

        private FieldInfo _localPlayerField;
        private FieldInfo _currentAttackField;
        private FieldInfo _previousAttackField;
        private FieldInfo _timeSinceLastAttackField;
        private FieldInfo _blockingInputField;
        private FieldInfo _internalBlockingStateField;
        private FieldInfo _animatorField;
        private FieldInfo _zanimField;
        private FieldInfo _blockingHashField;
        private FieldInfo _attackCharacterField;

        private MethodInfo _startAttackMethod;
        private MethodInfo _getCurrentWeaponMethod;
        private MethodInfo _inAttackMethod;
        private MethodInfo _attackTriggerMethod;
        private MethodInfo _attackStopMethod;
        private MethodInfo _attackIsDoneMethod;
        private MethodInfo _zanimSetBoolMethod;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<bool> _acceptEitherSideButton;
        private ConfigEntry<int> _triggerVirtualKey;
        private ConfigEntry<int> _blockVisualFrames;
        private ConfigEntry<float> _attackTriggerTimeoutSeconds;
        private ConfigEntry<float> _restartRetrySeconds;
        private ConfigEntry<float> _fastForwardNormalizedTime;
        private ConfigEntry<bool> _verbose;

        private CycleState _state = CycleState.Idle;
        private object _player;
        private object _trackedAttack;
        private bool _mouseWasDown;
        private bool _forceInAttackFalse;
        private int _blockStartedFrame;
        private int _renderFramesWithBlock;
        private float _stateStartedRealtime;
        private float _nextRestartAttempt;
        private bool _warnedFastForward;
        private bool _warnedBlockVisual;

        private enum CycleState
        {
            Idle,
            WaitingForHit,
            ShowingBlock,
            Restarting
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private void Awake()
        {
            _instance = this;
            _log = Logger;

            _enabled = Config.Bind("General", "Enabled", true,
                "Hold Mouse5 to repeatedly perform primary attack -> visible block flash -> next primary attack.");
            _acceptEitherSideButton = Config.Bind("Input", "AcceptEitherSideButtonFallback", true,
                "Also accept XBUTTON1 if mouse software swaps the side-button numbering.");
            _triggerVirtualKey = Config.Bind("Input", "TriggerVirtualKey", VkXButton2,
                "Primary Win32 virtual-key code. Decimal 6 / 0x06 is XBUTTON2, normally Mouse5.");
            _blockVisualFrames = Config.Bind("Visual", "BlockVisualFrames", 1,
                "Rendered frames to keep the synthetic block visible between attacks. 1 is the minimum and fastest.");
            _attackTriggerTimeoutSeconds = Config.Bind("Safety", "AttackTriggerTimeoutSeconds", 3.0f,
                "If a weapon never fires an attack trigger, release the cycle instead of hanging forever.");
            _restartRetrySeconds = Config.Bind("Safety", "RestartRetrySeconds", 0.05f,
                "Retry interval if the game temporarily refuses the next attack because of stamina, stagger, dodge, etc.");
            _fastForwardNormalizedTime = Config.Bind("Visual", "FastForwardNormalizedTime", 0.97f,
                "After the hit event, jump the current attack animation near its end before showing block. This removes recovery without skipping the hit itself.");
            _verbose = Config.Bind("Debug", "VerboseLogging", false,
                "Write every perfect-cancel state transition to LogOutput.log.");

            if (!InitializeReflection())
            {
                Logger.LogError("[UPC] Could not bind required Valheim 1.0 combat members. Mod disabled.");
                _enabled.Value = false;
                return;
            }

            _harmony = new Harmony(PluginGuid);
            _harmony.Patch(_attackTriggerMethod,
                postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(AttackTriggerPostfix)));
            _harmony.Patch(_inAttackMethod,
                postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(InAttackPostfix)));

            Logger.LogInfo(
                PluginName + " " + PluginVersion +
                " loaded. Mouse5 uses real Attack.OnAttackTrigger events; all primary weapons are eligible.");
        }

        private void OnDestroy()
        {
            try { EndCycle(restoreBlock: true, reason: "plugin unload"); } catch { }
            try { _harmony?.UnpatchSelf(); } catch { }
            _instance = null;
            _log = null;
        }

        private void Update()
        {
            if (_enabled == null || !_enabled.Value)
            {
                if (_state != CycleState.Idle)
                    EndCycle(true, "disabled");
                return;
            }

            var down = IsTriggerDown(out var inputSource);
            var pressed = down && !_mouseWasDown;
            _mouseWasDown = down;

            if (!down)
            {
                if (_state != CycleState.Idle)
                    EndCycle(true, "Mouse5 released");
                return;
            }

            var local = GetLocalPlayer();
            if (local == null)
                return;

            if (pressed)
                Logger.LogInfo("[UPC] Mouse5 detected via " + inputSource + ".");

            if (_state == CycleState.Idle)
            {
                _player = local;
                ArmOrStartAttack(local);
                return;
            }

            if (!ReferenceEquals(local, _player))
            {
                EndCycle(true, "local player changed");
                return;
            }

            switch (_state)
            {
                case CycleState.WaitingForHit:
                    // Some unusual attacks can finish without the standard OnAttackTrigger event.
                    // If that happens, re-arm rather than leaving the button dead forever.
                    if (_trackedAttack == null || IsAttackDone(_trackedAttack))
                    {
                        DebugLog("tracked attack ended without a trigger; restarting");
                        _state = CycleState.Restarting;
                        _nextRestartAttempt = Time.realtimeSinceStartup;
                        break;
                    }

                    if (Time.realtimeSinceStartup - _stateStartedRealtime > _attackTriggerTimeoutSeconds.Value)
                    {
                        DebugLog("attack trigger timeout; restarting");
                        _state = CycleState.Restarting;
                        _nextRestartAttempt = Time.realtimeSinceStartup;
                    }
                    break;

                case CycleState.ShowingBlock:
                    // The block flag is set in the hit-event postfix. Count actual rendered frames,
                    // not milliseconds, so the pose is guaranteed to reach the screen at least once.
                    if (Time.frameCount > _blockStartedFrame)
                        _renderFramesWithBlock++;

                    if (_renderFramesWithBlock >= Mathf.Max(1, _blockVisualFrames.Value))
                    {
                        ClearSyntheticBlock(_player);
                        _forceInAttackFalse = false;
                        _state = CycleState.Restarting;
                        _nextRestartAttempt = Time.realtimeSinceStartup;
                        DebugLog("block frame shown; starting next primary attack");
                    }
                    break;

                case CycleState.Restarting:
                    if (Time.realtimeSinceStartup >= _nextRestartAttempt)
                    {
                        if (TryStartPrimary(_player, bypassAnimatorAttackState: true))
                        {
                            TrackCurrentAttack(_player);
                        }
                        else
                        {
                            // Respect real game blockers (no stamina, stagger, dodge, menus, etc.).
                            // There is no timing gamble: retry until the engine accepts the attack.
                            _nextRestartAttempt = Time.realtimeSinceStartup + Mathf.Max(0.01f, _restartRetrySeconds.Value);
                        }
                    }
                    break;
            }
        }

        private bool InitializeReflection()
        {
            _playerType = AccessTools.TypeByName("Player");
            _humanoidType = AccessTools.TypeByName("Humanoid");
            _attackType = AccessTools.TypeByName("Attack");
            _zsyncAnimationType = AccessTools.TypeByName("ZSyncAnimation");
            if (_playerType == null || _humanoidType == null || _attackType == null)
                return false;

            _localPlayerField = AccessTools.Field(_playerType, "m_localPlayer");
            _currentAttackField = FindFieldInHierarchy(_humanoidType, "m_currentAttack");
            _previousAttackField = FindFieldInHierarchy(_humanoidType, "m_previousAttack");
            _timeSinceLastAttackField = FindFieldInHierarchy(_humanoidType, "m_timeSinceLastAttack");
            _blockingInputField = FindFieldInHierarchy(_playerType, "m_blocking");
            _internalBlockingStateField = FindFieldInHierarchy(_humanoidType, "m_internalBlockingState");
            _animatorField = FindFieldInHierarchy(_humanoidType, "m_animator");
            _zanimField = FindFieldInHierarchy(_humanoidType, "m_zanim");
            _blockingHashField = AccessTools.Field(_humanoidType, "s_blocking");
            _attackCharacterField = FindFieldInHierarchy(_attackType, "m_character");

            _getCurrentWeaponMethod = AccessTools.Method(_humanoidType, "GetCurrentWeapon", Type.EmptyTypes);
            _inAttackMethod = AccessTools.Method(_humanoidType, "InAttack", Type.EmptyTypes);
            _attackTriggerMethod = AccessTools.Method(_attackType, "OnAttackTrigger", Type.EmptyTypes);
            _attackStopMethod = AccessTools.Method(_attackType, "Stop", Type.EmptyTypes);
            _attackIsDoneMethod = AccessTools.Method(_attackType, "IsDone", Type.EmptyTypes);

            _startAttackMethod = _humanoidType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "StartAttack") return false;
                    var p = m.GetParameters();
                    return p.Length == 2 && p[1].ParameterType == typeof(bool);
                });

            if (_zsyncAnimationType != null)
            {
                _zanimSetBoolMethod = _zsyncAnimationType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "SetBool") return false;
                        var p = m.GetParameters();
                        return p.Length == 2 && p[0].ParameterType == typeof(int) && p[1].ParameterType == typeof(bool);
                    });
            }

            var ok = _localPlayerField != null &&
                     _currentAttackField != null &&
                     _previousAttackField != null &&
                     _timeSinceLastAttackField != null &&
                     _getCurrentWeaponMethod != null &&
                     _inAttackMethod != null &&
                     _startAttackMethod != null &&
                     _attackTriggerMethod != null &&
                     _attackStopMethod != null;

            Logger.LogInfo(
                "[UPC] Combat hooks: currentAttack=" + Found(_currentAttackField) +
                ", previousAttack=" + Found(_previousAttackField) +
                ", timeSinceLastAttack=" + Found(_timeSinceLastAttackField) +
                ", blocking=" + Found(_blockingInputField) +
                ", internalBlock=" + Found(_internalBlockingStateField) +
                ", animator=" + Found(_animatorField) +
                ", zanim=" + Found(_zanimField) +
                ", StartAttack=" + Found(_startAttackMethod) +
                ", OnAttackTrigger=" + Found(_attackTriggerMethod) + ".");
            return ok;
        }

        private static string Found(MemberInfo member) => member != null ? "found" : "missing";

        private static FieldInfo FindFieldInHierarchy(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }
            return null;
        }

        private object GetLocalPlayer()
        {
            try { return _localPlayerField?.GetValue(null); }
            catch { return null; }
        }

        private void ArmOrStartAttack(object player)
        {
            // If Mouse5 is pressed during an already-running primary swing, use that swing as hit #1.
            var current = GetCurrentAttack(player);
            if (current != null && !IsAttackDone(current))
            {
                _trackedAttack = current;
                _state = CycleState.WaitingForHit;
                _stateStartedRealtime = Time.realtimeSinceStartup;
                DebugLog("armed existing attack");
                return;
            }

            if (TryStartPrimary(player, bypassAnimatorAttackState: false))
                TrackCurrentAttack(player);
            else
            {
                _state = CycleState.Restarting;
                _nextRestartAttempt = Time.realtimeSinceStartup + Mathf.Max(0.01f, _restartRetrySeconds.Value);
                DebugLog("initial attack not yet legal; retrying");
            }
        }

        private bool TryStartPrimary(object player, bool bypassAnimatorAttackState)
        {
            if (player == null || _startAttackMethod == null)
                return false;

            try
            {
                var weapon = _getCurrentWeaponMethod?.Invoke(player, null);
                if (weapon == null)
                    return false;

                _forceInAttackFalse = bypassAnimatorAttackState;
                var result = _startAttackMethod.Invoke(player, new object[] { null, false });
                return result is bool && (bool)result;
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException ?? tie;
                _log?.LogWarning("[UPC] StartAttack failed: " + inner.GetType().Name + ": " + inner.Message);
                return false;
            }
            catch (Exception ex)
            {
                _log?.LogWarning("[UPC] StartAttack reflection failed: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
            finally
            {
                _forceInAttackFalse = false;
            }
        }

        private void TrackCurrentAttack(object player)
        {
            _trackedAttack = GetCurrentAttack(player);
            if (_trackedAttack == null)
            {
                _state = CycleState.Restarting;
                _nextRestartAttempt = Time.realtimeSinceStartup + Mathf.Max(0.01f, _restartRetrySeconds.Value);
                DebugLog("StartAttack returned true but current attack was null; retrying");
                return;
            }

            _state = CycleState.WaitingForHit;
            _stateStartedRealtime = Time.realtimeSinceStartup;
            DebugLog("primary attack started; waiting for real hit trigger");
        }

        private object GetCurrentAttack(object player)
        {
            try { return _currentAttackField?.GetValue(player); }
            catch { return null; }
        }

        private bool IsAttackDone(object attack)
        {
            if (attack == null)
                return true;
            if (_attackIsDoneMethod == null)
                return false;
            try
            {
                var result = _attackIsDoneMethod.Invoke(attack, null);
                return result is bool && (bool)result;
            }
            catch { return false; }
        }

        private static void AttackTriggerPostfix(object __instance)
        {
            _instance?.OnRealAttackTrigger(__instance);
        }

        private void OnRealAttackTrigger(object attack)
        {
            if (_state != CycleState.WaitingForHit || attack == null || !ReferenceEquals(attack, _trackedAttack))
                return;
            if (!IsTriggerDown(out _))
                return;

            var local = GetLocalPlayer();
            if (local == null || !ReferenceEquals(local, _player))
                return;

            // The original OnAttackTrigger has already completed at this point: melee damage,
            // projectile spawning, ammo/resource event work, etc. are preserved. We only delete
            // the recovery portion after the legitimate hit event.
            BeginGuaranteedBlockCancel(local, attack);
        }

        private void BeginGuaranteedBlockCancel(object player, object attack)
        {
            try
            {
                var current = GetCurrentAttack(player);
                if (current == null || !ReferenceEquals(current, attack))
                    return;

                // Finish the attack object AFTER its real hit event, preserving it as previousAttack
                // so vanilla combo-chain logic still advances normally.
                _attackStopMethod.Invoke(attack, null);
                _previousAttackField.SetValue(player, attack);
                _currentAttackField.SetValue(player, null);
                _timeSinceLastAttackField.SetValue(player, 0f);

                FastForwardRecoveryAnimation(player);
                SetSyntheticBlock(player, true);

                // During this tiny visual block phase, report not-in-attack to the local engine so
                // Valheim's own block transition is never held hostage by the old attack tag.
                _forceInAttackFalse = true;
                _blockStartedFrame = Time.frameCount;
                _renderFramesWithBlock = 0;
                _state = CycleState.ShowingBlock;
                _stateStartedRealtime = Time.realtimeSinceStartup;
                DebugLog("real hit fired -> recovery removed -> block shown");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[UPC] Could not enter synthetic block: " + ex.GetType().Name + ": " + ex.Message);
                EndCycle(true, "block-cancel exception");
            }
        }

        private void FastForwardRecoveryAnimation(object player)
        {
            if (_animatorField == null)
                return;
            try
            {
                var animator = _animatorField.GetValue(player) as Animator;
                if (animator == null || !animator.isActiveAndEnabled)
                    return;

                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash == 0)
                    return;

                // Do not skip the attack event: this method is only called from the postfix of
                // OnAttackTrigger. Jumping to the end here removes only recovery frames.
                var normalized = Mathf.Clamp(_fastForwardNormalizedTime.Value, 0.80f, 0.999f);
                animator.Play(state.fullPathHash, 0, normalized);
            }
            catch (Exception ex)
            {
                if (!_warnedFastForward)
                {
                    _warnedFastForward = true;
                    Logger.LogWarning("[UPC] Animator fast-forward unavailable; block cancel will still run: " + ex.Message);
                }
            }
        }

        private void SetSyntheticBlock(object player, bool value)
        {
            try
            {
                _blockingInputField?.SetValue(player, value);
                _internalBlockingStateField?.SetValue(player, value);

                if (_zanimField != null && _blockingHashField != null && _zanimSetBoolMethod != null)
                {
                    var zanim = _zanimField.GetValue(player);
                    var hash = _blockingHashField.GetValue(null);
                    if (zanim != null && hash is int)
                        _zanimSetBoolMethod.Invoke(zanim, new object[] { (int)hash, value });
                }
            }
            catch (Exception ex)
            {
                if (!_warnedBlockVisual)
                {
                    _warnedBlockVisual = true;
                    Logger.LogWarning("[UPC] Block visual reflection partially failed: " + ex.Message);
                }
            }
        }

        private void ClearSyntheticBlock(object player)
        {
            if (player != null)
                SetSyntheticBlock(player, false);
        }

        private static void InAttackPostfix(object __instance, ref bool __result)
        {
            var inst = _instance;
            if (inst == null || !inst._forceInAttackFalse || __instance == null)
                return;

            var local = inst.GetLocalPlayer();
            if (local != null && ReferenceEquals(local, __instance))
                __result = false;
        }

        private bool IsTriggerDown(out string source)
        {
            source = string.Empty;
            try
            {
                var primary = _triggerVirtualKey != null ? _triggerVirtualKey.Value : VkXButton2;
                if ((GetAsyncKeyState(primary) & KeyDownMask) != 0)
                {
                    source = "Win32 VK 0x" + primary.ToString("X2");
                    return true;
                }

                if (_acceptEitherSideButton != null && _acceptEitherSideButton.Value)
                {
                    var alternate = primary == VkXButton1 ? VkXButton2 : VkXButton1;
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

        private void EndCycle(bool restoreBlock, string reason)
        {
            if (restoreBlock && _player != null)
                ClearSyntheticBlock(_player);

            _forceInAttackFalse = false;
            _state = CycleState.Idle;
            _player = null;
            _trackedAttack = null;
            _renderFramesWithBlock = 0;
            _blockStartedFrame = 0;
            _stateStartedRealtime = 0f;
            _nextRestartAttempt = 0f;
            DebugLog("cycle ended: " + reason);
        }

        private void DebugLog(string message)
        {
            if (_verbose != null && _verbose.Value)
                Logger.LogInfo("[UPC] " + message);
        }
    }
}
