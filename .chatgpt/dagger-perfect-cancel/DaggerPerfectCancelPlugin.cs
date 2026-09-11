using System;
using System.Collections.Generic;
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
        public const string PluginName = "Goni Five-Hit Block Cancel";
        public const string PluginVersion = "2.2.0";

        private const int VkXButton1 = 0x05;
        private const int VkXButton2 = 0x06;
        private const int KeyDownMask = 0x8000;
        private const int BurstHits = 5;

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
        private MethodInfo _getAttackStaminaMethod;
        private MethodInfo _zanimSetBoolMethod;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<bool> _acceptEitherSideButton;
        private ConfigEntry<int> _triggerVirtualKey;
        private ConfigEntry<int> _blockVisualFrames;
        private ConfigEntry<float> _fastForwardNormalizedTime;
        private ConfigEntry<float> _retrySeconds;
        private ConfigEntry<float> _hitTimeoutSeconds;
        private ConfigEntry<bool> _verbose;

        private BurstState _state = BurstState.Idle;
        private object _player;
        private object _trackedAttack;
        private readonly Dictionary<object, int> _burstAttackIndices = new Dictionary<object, int>();

        private int _hitIndex;
        private int _pendingHitIndex;
        private int _startingBurstHitIndex;
        private bool _forceInAttackFalse;
        private bool _mouseWasDown;
        private int _blockStartedFrame;
        private int _blockFramesSeen;
        private float _nextStartAttempt;
        private float _stateStartedRealtime;
        private bool _warnedFastForward;
        private bool _warnedBlockVisual;

        private enum BurstState
        {
            Idle,
            StartingHit,
            WaitingForHit,
            ShowingBlock,
            Completed
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private void Awake()
        {
            _instance = this;
            _log = Logger;

            _enabled = Config.Bind("General", "Enabled", true,
                "Press Mouse5 to execute one five-hit primary-attack burst.");
            _acceptEitherSideButton = Config.Bind("Input", "AcceptEitherSideButtonFallback", true,
                "Also accept XBUTTON1 if mouse software swaps side-button numbering.");
            _triggerVirtualKey = Config.Bind("Input", "TriggerVirtualKey", VkXButton2,
                "Win32 virtual-key. Decimal 6 / 0x06 is XBUTTON2, normally Mouse5.");
            _blockVisualFrames = Config.Bind("Visual", "BlockVisualFrames", 1,
                "Rendered frames to show a block after hit 1. 1 is the minimum.");
            _fastForwardNormalizedTime = Config.Bind("Timing", "RecoveryCutNormalizedTime", 0.985f,
                "After a legitimate hit event, jump the old attack animation near its end before chaining.");
            _retrySeconds = Config.Bind("Safety", "StartRetrySeconds", 0.01f,
                "Retry interval if Valheim temporarily refuses the next burst hit.");
            _hitTimeoutSeconds = Config.Bind("Safety", "HitTimeoutSeconds", 3.0f,
                "Abort a burst if an attack never reaches OnAttackTrigger.");
            _verbose = Config.Bind("Debug", "VerboseLogging", false,
                "Log every burst transition.");

            if (!InitializeReflection())
            {
                Logger.LogError("[5HBC] Could not bind required Valheim 1.0 combat members. Mod disabled.");
                _enabled.Value = false;
                return;
            }

            _harmony = new Harmony(PluginGuid);
            _harmony.Patch(_attackTriggerMethod,
                postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(AttackTriggerPostfix)));
            _harmony.Patch(_inAttackMethod,
                postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(InAttackPostfix)));
            _harmony.Patch(_getAttackStaminaMethod,
                postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(GetAttackStaminaPostfix)));

            Logger.LogInfo(
                PluginName + " " + PluginVersion +
                " loaded. Mouse5 = 5-hit burst; block after hit 1; stamina only on hits 1-2.");
        }

        private void OnDestroy()
        {
            try { AbortBurst("plugin unload"); } catch { }
            try { _harmony?.UnpatchSelf(); } catch { }
            _instance = null;
            _log = null;
        }

        private void Update()
        {
            if (_enabled == null || !_enabled.Value)
            {
                if (_state != BurstState.Idle)
                    AbortBurst("disabled");
                return;
            }

            bool down = IsTriggerDown(out string source);
            bool pressed = down && !_mouseWasDown;
            _mouseWasDown = down;

            object local = GetLocalPlayer();

            if (_state == BurstState.Idle)
            {
                if (!pressed || local == null)
                    return;

                Logger.LogInfo("[5HBC] Mouse5 detected via " + source + "; starting five-hit burst.");
                BeginBurst(local);
                return;
            }

            if (_player == null || local == null || !ReferenceEquals(local, _player))
            {
                AbortBurst("local player changed");
                return;
            }

            switch (_state)
            {
                case BurstState.StartingHit:
                    if (Time.realtimeSinceStartup >= _nextStartAttempt)
                    {
                        if (!TryStartBurstHit(_pendingHitIndex, _pendingHitIndex > 1))
                        {
                            _nextStartAttempt = Time.realtimeSinceStartup + Mathf.Max(0.001f, _retrySeconds.Value);
                        }
                    }
                    break;

                case BurstState.WaitingForHit:
                    if (_trackedAttack == null)
                    {
                        AbortBurst("tracked attack missing");
                        break;
                    }

                    if (IsAttackDone(_trackedAttack))
                    {
                        AbortBurst("attack ended without OnAttackTrigger");
                        break;
                    }

                    if (Time.realtimeSinceStartup - _stateStartedRealtime >
                        Mathf.Max(0.25f, _hitTimeoutSeconds.Value))
                    {
                        AbortBurst("hit trigger timeout");
                    }
                    break;

                case BurstState.ShowingBlock:
                    if (Time.frameCount > _blockStartedFrame)
                        _blockFramesSeen++;

                    if (_blockFramesSeen >= Mathf.Max(1, _blockVisualFrames.Value))
                    {
                        SetSyntheticBlock(_player, false);
                        _pendingHitIndex = 2;
                        _state = BurstState.StartingHit;
                        _nextStartAttempt = Time.realtimeSinceStartup;
                        DebugLog("block flash complete -> hit 2");
                    }
                    break;

                case BurstState.Completed:
                    if (!down)
                        ResetToIdle();
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
            _getAttackStaminaMethod = AccessTools.Method(_attackType, "GetAttackStamina", Type.EmptyTypes);

            _startAttackMethod = _humanoidType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "StartAttack")
                        return false;
                    ParameterInfo[] p = m.GetParameters();
                    return p.Length == 2 && p[1].ParameterType == typeof(bool);
                });

            if (_zsyncAnimationType != null)
            {
                _zanimSetBoolMethod = _zsyncAnimationType
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "SetBool")
                            return false;
                        ParameterInfo[] p = m.GetParameters();
                        return p.Length == 2 &&
                               p[0].ParameterType == typeof(int) &&
                               p[1].ParameterType == typeof(bool);
                    });
            }

            bool ok =
                _localPlayerField != null &&
                _currentAttackField != null &&
                _previousAttackField != null &&
                _timeSinceLastAttackField != null &&
                _getCurrentWeaponMethod != null &&
                _inAttackMethod != null &&
                _startAttackMethod != null &&
                _attackTriggerMethod != null &&
                _attackStopMethod != null &&
                _getAttackStaminaMethod != null;

            Logger.LogInfo(
                "[5HBC] Hooks: currentAttack=" + Found(_currentAttackField) +
                ", previousAttack=" + Found(_previousAttackField) +
                ", StartAttack=" + Found(_startAttackMethod) +
                ", OnAttackTrigger=" + Found(_attackTriggerMethod) +
                ", GetAttackStamina=" + Found(_getAttackStaminaMethod) +
                ", blockVisual=" + ((_zanimField != null && _blockingHashField != null) ? "available" : "fallback") +
                ".");

            return ok;
        }

        private void BeginBurst(object player)
        {
            _player = player;
            _burstAttackIndices.Clear();
            _hitIndex = 0;
            _pendingHitIndex = 1;
            _trackedAttack = null;
            SetSyntheticBlock(player, false);
            _state = BurstState.StartingHit;
            _nextStartAttempt = Time.realtimeSinceStartup;
            DebugLog("burst armed -> hit 1");
        }

        private bool TryStartBurstHit(int hitNumber, bool bypassAnimatorAttackState)
        {
            if (_player == null || hitNumber < 1 || hitNumber > BurstHits)
                return false;

            try
            {
                object weapon = _getCurrentWeaponMethod?.Invoke(_player, null);
                if (weapon == null)
                {
                    AbortBurst("no primary weapon");
                    return false;
                }

                _startingBurstHitIndex = hitNumber;
                _forceInAttackFalse = bypassAnimatorAttackState;

                object result = _startAttackMethod.Invoke(_player, new object[] { null, false });
                if (!(result is bool) || !(bool)result)
                    return false;

                object attack = GetCurrentAttack(_player);
                if (attack == null)
                    return false;

                _burstAttackIndices[attack] = hitNumber;
                _trackedAttack = attack;
                _hitIndex = hitNumber;
                _state = BurstState.WaitingForHit;
                _stateStartedRealtime = Time.realtimeSinceStartup;

                DebugLog("hit " + hitNumber + " started" + (hitNumber >= 3 ? " (stamina-free)" : ""));
                return true;
            }
            catch (TargetInvocationException tie)
            {
                Exception inner = tie.InnerException ?? tie;
                Logger.LogWarning("[5HBC] StartAttack failed: " +
                                  inner.GetType().Name + ": " + inner.Message);
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[5HBC] StartAttack reflection failed: " +
                                  ex.GetType().Name + ": " + ex.Message);
                return false;
            }
            finally
            {
                _forceInAttackFalse = false;
                _startingBurstHitIndex = 0;
            }
        }

        private static void AttackTriggerPostfix(object __instance)
        {
            _instance?.OnAttackTrigger(__instance);
        }

        private void OnAttackTrigger(object attack)
        {
            if (_state != BurstState.WaitingForHit ||
                attack == null ||
                _trackedAttack == null ||
                !ReferenceEquals(attack, _trackedAttack))
                return;

            int hitNumber = _hitIndex;
            if (_burstAttackIndices.TryGetValue(attack, out int mapped))
                hitNumber = mapped;

            DebugLog("hit " + hitNumber + " trigger fired");

            if (hitNumber >= BurstHits)
            {
                _trackedAttack = null;
                _state = BurstState.Completed;
                Logger.LogInfo("[5HBC] Five-hit burst completed.");
                return;
            }

            if (!FinishAttackForImmediateChain(_player, attack))
            {
                AbortBurst("could not finish current attack");
                return;
            }

            FastForwardRecoveryAnimation(_player);

            if (hitNumber == 1)
            {
                SetSyntheticBlock(_player, true);
                _blockStartedFrame = Time.frameCount;
                _blockFramesSeen = 0;
                _state = BurstState.ShowingBlock;
                DebugLog("hit 1 -> block flash");
                return;
            }

            int next = hitNumber + 1;
            _pendingHitIndex = next;

            if (!TryStartBurstHit(next, true))
            {
                _state = BurstState.StartingHit;
                _nextStartAttempt = Time.realtimeSinceStartup +
                                    Mathf.Max(0.001f, _retrySeconds.Value);
            }
        }

        private bool FinishAttackForImmediateChain(object player, object attack)
        {
            if (player == null || attack == null)
                return false;

            try
            {
                object current = GetCurrentAttack(player);
                if (current == null || !ReferenceEquals(current, attack))
                    return false;

                _attackStopMethod.Invoke(attack, null);
                _previousAttackField.SetValue(player, attack);
                _currentAttackField.SetValue(player, null);
                _timeSinceLastAttackField.SetValue(player, 0f);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[5HBC] Chain handoff failed: " +
                                  ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static void GetAttackStaminaPostfix(object __instance, ref float __result)
        {
            DaggerPerfectCancelPlugin inst = _instance;
            if (inst == null || __instance == null)
                return;

            int hitNumber = 0;

            if (inst._burstAttackIndices.TryGetValue(__instance, out int mapped))
            {
                hitNumber = mapped;
            }
            else if (inst._startingBurstHitIndex >= 3 && inst._startingBurstHitIndex <= BurstHits)
            {
                object owner = inst.GetAttackCharacter(__instance);
                if (owner != null && inst._player != null && ReferenceEquals(owner, inst._player))
                    hitNumber = inst._startingBurstHitIndex;
            }

            if (hitNumber >= 3 && hitNumber <= BurstHits)
                __result = 0f;
        }

        private object GetAttackCharacter(object attack)
        {
            try { return _attackCharacterField?.GetValue(attack); }
            catch { return null; }
        }

        private static void InAttackPostfix(object __instance, ref bool __result)
        {
            DaggerPerfectCancelPlugin inst = _instance;
            if (inst == null || !inst._forceInAttackFalse || __instance == null)
                return;

            object local = inst.GetLocalPlayer();
            if (local != null && ReferenceEquals(local, __instance))
                __result = false;
        }

        private void FastForwardRecoveryAnimation(object player)
        {
            if (_animatorField == null)
                return;

            try
            {
                Animator animator = _animatorField.GetValue(player) as Animator;
                if (animator == null || !animator.isActiveAndEnabled)
                    return;

                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash == 0)
                    return;

                float normalized = Mathf.Clamp(
                    _fastForwardNormalizedTime.Value, 0.85f, 0.999f);

                animator.Play(state.fullPathHash, 0, normalized);
            }
            catch (Exception ex)
            {
                if (!_warnedFastForward)
                {
                    _warnedFastForward = true;
                    Logger.LogWarning("[5HBC] Recovery fast-forward unavailable: " + ex.Message);
                }
            }
        }

        private void SetSyntheticBlock(object player, bool value)
        {
            if (player == null)
                return;

            try
            {
                _blockingInputField?.SetValue(player, value);
                _internalBlockingStateField?.SetValue(player, value);

                if (_zanimField != null &&
                    _blockingHashField != null &&
                    _zanimSetBoolMethod != null)
                {
                    object zanim = _zanimField.GetValue(player);
                    object hash = _blockingHashField.GetValue(null);

                    if (zanim != null && hash is int)
                        _zanimSetBoolMethod.Invoke(zanim, new object[] { (int)hash, value });
                }
            }
            catch (Exception ex)
            {
                if (!_warnedBlockVisual)
                {
                    _warnedBlockVisual = true;
                    Logger.LogWarning("[5HBC] Block visual partially failed: " + ex.Message);
                }
            }
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
                object result = _attackIsDoneMethod.Invoke(attack, null);
                return result is bool && (bool)result;
            }
            catch
            {
                return false;
            }
        }

        private object GetLocalPlayer()
        {
            try { return _localPlayerField?.GetValue(null); }
            catch { return null; }
        }

        private bool IsTriggerDown(out string source)
        {
            source = string.Empty;
            try
            {
                int primary = _triggerVirtualKey != null
                    ? _triggerVirtualKey.Value
                    : VkXButton2;

                if ((GetAsyncKeyState(primary) & KeyDownMask) != 0)
                {
                    source = "Win32 VK 0x" + primary.ToString("X2");
                    return true;
                }

                if (_acceptEitherSideButton != null && _acceptEitherSideButton.Value)
                {
                    int alternate = primary == VkXButton1 ? VkXButton2 : VkXButton1;
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

        private void AbortBurst(string reason)
        {
            if (_player != null)
                SetSyntheticBlock(_player, false);

            _forceInAttackFalse = false;
            _startingBurstHitIndex = 0;
            _trackedAttack = null;
            _burstAttackIndices.Clear();
            _player = null;
            _hitIndex = 0;
            _pendingHitIndex = 0;
            _blockFramesSeen = 0;
            _blockStartedFrame = 0;
            _nextStartAttempt = 0f;
            _stateStartedRealtime = 0f;
            _state = BurstState.Idle;

            DebugLog("burst aborted: " + reason);
        }

        private void ResetToIdle()
        {
            if (_player != null)
                SetSyntheticBlock(_player, false);

            _forceInAttackFalse = false;
            _startingBurstHitIndex = 0;
            _trackedAttack = null;
            _burstAttackIndices.Clear();
            _player = null;
            _hitIndex = 0;
            _pendingHitIndex = 0;
            _blockFramesSeen = 0;
            _blockStartedFrame = 0;
            _nextStartAttempt = 0f;
            _stateStartedRealtime = 0f;
            _state = BurstState.Idle;
        }

        private static string Found(MemberInfo member)
        {
            return member != null ? "found" : "missing";
        }

        private static FieldInfo FindFieldInHierarchy(Type type, string name)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo field = t.GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly);

                if (field != null)
                    return field;
            }

            return null;
        }

        private void DebugLog(string message)
        {
            if (_verbose != null && _verbose.Value)
                Logger.LogInfo("[5HBC] " + message);
        }
    }
}
