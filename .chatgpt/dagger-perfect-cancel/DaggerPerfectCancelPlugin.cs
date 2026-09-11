using System;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Goni.DaggerPerfectCancel
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class DaggerPerfectCancelPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "goni.valheim.daggerperfectcancel";
        public const string PluginName = "Goni State Driven Block Cancel";
        public const string PluginVersion = "3.0.0";

        private const int VkXButton1 = 0x05;
        private const int VkXButton2 = 0x06;
        private const int KeyDownMask = 0x8000;

        private static DaggerPerfectCancelPlugin _instance;

        private Harmony _harmony;
        private FieldInfo _queuedAttackTimerField;
        private FieldInfo _attackHoldField;
        private FieldInfo _blockingField;

        private ConfigEntry<bool> _enabled;
        private ConfigEntry<int> _triggerVirtualKey;
        private ConfigEntry<bool> _acceptEitherSideButton;
        private ConfigEntry<int> _secondAttackHoldMs;
        private ConfigEntry<float> _queueSeconds;
        private ConfigEntry<float> _attackStartTimeout;
        private ConfigEntry<float> _attackEndTimeout;
        private ConfigEntry<float> _blockStartTimeout;
        private ConfigEntry<bool> _verbose;

        private bool _triggerDown;
        private bool _triggerWasDown;
        private State _state = State.Idle;
        private float _stateStarted;
        private float _holdUntil;

        private enum State
        {
            Idle,
            QueueFirstAttack,
            WaitFirstAttackStart,
            WaitFirstAttackEnd,
            RaiseBlock,
            WaitBlockObserved,
            HoldSecondAttack,
            WaitRelease
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private void Awake()
        {
            _instance = this;

            _enabled = Config.Bind("General", "Enabled", true,
                "Enable the Mouse5 state-driven block-cancel sequence.");
            _triggerVirtualKey = Config.Bind("Input", "TriggerVirtualKey", VkXButton2,
                "Win32 virtual-key. 6 / 0x06 is normally Mouse5 (XBUTTON2).");
            _acceptEitherSideButton = Config.Bind("Input", "AcceptEitherSideButtonFallback", false,
                "Also accept the other side button. Leave false unless your mouse software swaps Mouse4/Mouse5.");
            _secondAttackHoldMs = Config.Bind("Timing", "SecondAttackHoldMs", 1200,
                "How long the post-block primary attack is held. Equivalent to the AHK Attack2_Hold value.");
            _queueSeconds = Config.Bind("Timing", "AttackQueueSeconds", 0.50f,
                "Internal primary-attack queue duration while waiting for Valheim to accept the click.");
            _attackStartTimeout = Config.Bind("Safety", "FirstAttackStartTimeoutSeconds", 1.5f,
                "Abort if the first primary attack cannot start.");
            _attackEndTimeout = Config.Bind("Safety", "FirstAttackEndTimeoutSeconds", 4.0f,
                "Abort if the first attack never leaves the attack animation state. Hit-stop is naturally included.");
            _blockStartTimeout = Config.Bind("Safety", "BlockStartTimeoutSeconds", 1.5f,
                "Abort if the actual blocking state never becomes active.");
            _verbose = Config.Bind("Debug", "VerboseLogging", false,
                "Log every state transition.");

            _queuedAttackTimerField = AccessTools.Field(typeof(Player), "m_queuedAttackTimer");
            _attackHoldField = FindFieldInHierarchy(typeof(Player), "m_attackHold");
            _blockingField = FindFieldInHierarchy(typeof(Player), "m_blocking");

            if (_queuedAttackTimerField == null || _attackHoldField == null || _blockingField == null)
            {
                Logger.LogError("[SDBC] Required Valheim control fields were not found. Mod disabled.");
                _enabled.Value = false;
                return;
            }

            var setControls = AccessTools.Method(typeof(Player), "SetControls");
            if (setControls == null)
            {
                Logger.LogError("[SDBC] Player.SetControls was not found. Mod disabled.");
                _enabled.Value = false;
                return;
            }

            _harmony = new Harmony(PluginGuid);
            _harmony.Patch(setControls,
                postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SetControlsPostfix)));

            Logger.LogInfo(
                PluginName + " " + PluginVersion +
                " loaded. Mouse5 sequence = first attack -> real attack end -> real block observed -> primary hold.");
        }

        private void Update()
        {
            if (_enabled == null || !_enabled.Value)
                return;

            _triggerDown = ReadTriggerDown();
            var pressed = _triggerDown && !_triggerWasDown;
            _triggerWasDown = _triggerDown;

            if (pressed && _state == State.Idle)
            {
                SetState(State.QueueFirstAttack, "Mouse5 pressed");
                Logger.LogInfo("[SDBC] Mouse5 sequence started.");
            }

            if (_state == State.WaitRelease && !_triggerDown)
                SetState(State.Idle, "Mouse5 released; re-armed");
        }

        private void OnDestroy()
        {
            try { _harmony?.UnpatchSelf(); } catch { }
            try { ReleaseInjectedControls(Player.m_localPlayer); } catch { }
            _instance = null;
        }

        private static void SetControlsPostfix(Player __instance)
        {
            var inst = _instance;
            if (inst == null || inst._enabled == null || !inst._enabled.Value || __instance == null)
                return;
            if (Player.m_localPlayer == null || !ReferenceEquals(__instance, Player.m_localPlayer))
                return;

            inst.AfterVanillaControls(__instance);
        }

        private void AfterVanillaControls(Player player)
        {
            var now = Time.realtimeSinceStartup;

            switch (_state)
            {
                case State.Idle:
                    return;

                case State.QueueFirstAttack:
                    // Equivalent to AHK's first short LMB click, but use Valheim's own queued-click
                    // field instead of an OS-level mouse event.
                    ForceBlock(player, false);
                    ForceAttackHold(player, false);
                    QueuePrimaryClick(player);
                    SetState(State.WaitFirstAttackStart, "first primary click queued");
                    break;

                case State.WaitFirstAttackStart:
                    ForceBlock(player, false);
                    ForceAttackHold(player, false);

                    if (player.InAttack())
                    {
                        SetState(State.WaitFirstAttackEnd, "first attack actually started");
                        break;
                    }

                    // Keep the click alive until the engine consumes it. This is a state wait, not
                    // an animation timing guess.
                    QueuePrimaryClick(player);
                    if (Elapsed(now) > _attackStartTimeout.Value)
                        Abort(player, "first attack start timeout");
                    break;

                case State.WaitFirstAttackEnd:
                    ForceBlock(player, false);
                    ForceAttackHold(player, false);

                    // This is the key difference from AHK: hit-stop, FPS and weapon animation speed
                    // can extend the swing arbitrarily. We do not raise block until Valheim itself
                    // says the attack animation/tag has actually ended.
                    if (!player.InAttack())
                    {
                        ForceBlock(player, true);
                        SetState(State.RaiseBlock, "first attack ended; raising block");
                        break;
                    }

                    if (Elapsed(now) > _attackEndTimeout.Value)
                        Abort(player, "first attack end timeout");
                    break;

                case State.RaiseBlock:
                    ForceAttackHold(player, false);
                    ForceBlock(player, true);
                    SetState(State.WaitBlockObserved, "block input injected");
                    break;

                case State.WaitBlockObserved:
                    ForceAttackHold(player, false);
                    ForceBlock(player, true);

                    // Release on the first control frame after the real blocking state becomes true.
                    // This corresponds to: 'shield starts coming up -> immediately release RMB'.
                    if (player.IsBlocking())
                    {
                        ForceBlock(player, false);
                        QueuePrimaryClick(player);
                        ForceAttackHold(player, true);
                        _holdUntil = now + Mathf.Max(1, _secondAttackHoldMs.Value) / 1000f;
                        SetState(State.HoldSecondAttack, "real block observed; released block + holding primary");
                        break;
                    }

                    if (Elapsed(now) > _blockStartTimeout.Value)
                        Abort(player, "block start timeout");
                    break;

                case State.HoldSecondAttack:
                    ForceBlock(player, false);

                    if (now < _holdUntil)
                    {
                        // Equivalent to the AHK second LMB being physically held. Valheim's normal
                        // combo/queue code decides how many attacks happen during this hold.
                        ForceAttackHold(player, true);
                        QueuePrimaryClick(player);
                    }
                    else
                    {
                        // Do not forcibly write false here: vanilla SetControls has already applied
                        // the user's real LMB state this frame. Simply stop overriding it.
                        _holdUntil = 0f;
                        SetState(State.WaitRelease, "primary hold finished");
                    }
                    break;

                case State.WaitRelease:
                    // One sequence per Mouse5 press, matching an AHK hotkey press. The physical
                    // button may already be released; Update() will re-arm Idle in that case.
                    break;
            }
        }

        private void QueuePrimaryClick(Player player)
        {
            try
            {
                var current = (float)_queuedAttackTimerField.GetValue(player);
                var target = Mathf.Max(current, Mathf.Clamp(_queueSeconds.Value, 0.05f, 1.0f));
                _queuedAttackTimerField.SetValue(player, target);
            }
            catch (Exception ex)
            {
                Abort(player, "queue reflection failed: " + ex.Message);
            }
        }

        private void ForceAttackHold(Player player, bool value)
        {
            try { _attackHoldField.SetValue(player, value); }
            catch (Exception ex) { Abort(player, "attackHold reflection failed: " + ex.Message); }
        }

        private void ForceBlock(Player player, bool value)
        {
            try { _blockingField.SetValue(player, value); }
            catch (Exception ex) { Abort(player, "blocking reflection failed: " + ex.Message); }
        }

        private void Abort(Player player, string reason)
        {
            ReleaseInjectedControls(player);
            Logger.LogWarning("[SDBC] Sequence aborted: " + reason);
            _holdUntil = 0f;
            _state = _triggerDown ? State.WaitRelease : State.Idle;
            _stateStarted = Time.realtimeSinceStartup;
        }

        private void ReleaseInjectedControls(Player player)
        {
            if (player == null)
                return;

            try { _blockingField?.SetValue(player, false); } catch { }
            try { _attackHoldField?.SetValue(player, false); } catch { }
        }

        private bool ReadTriggerDown()
        {
            try
            {
                var primary = _triggerVirtualKey != null ? _triggerVirtualKey.Value : VkXButton2;
                if ((GetAsyncKeyState(primary) & KeyDownMask) != 0)
                    return true;

                if (_acceptEitherSideButton != null && _acceptEitherSideButton.Value)
                {
                    var alternate = primary == VkXButton1 ? VkXButton2 : VkXButton1;
                    return (GetAsyncKeyState(alternate) & KeyDownMask) != 0;
                }
            }
            catch { }

            return false;
        }

        private void SetState(State next, string reason)
        {
            _state = next;
            _stateStarted = Time.realtimeSinceStartup;
            if (_verbose != null && _verbose.Value)
                Logger.LogInfo("[SDBC] " + next + " <- " + reason);
        }

        private float Elapsed(float now)
        {
            return Mathf.Max(0f, now - _stateStarted);
        }

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
    }
}
