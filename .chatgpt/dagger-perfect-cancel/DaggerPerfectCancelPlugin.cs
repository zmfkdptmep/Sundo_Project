using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Goni.DaggerPerfectCancel
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed partial class DaggerPerfectCancelPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "goni.valheim.daggerperfectcancel";
        public const string PluginName = "Goni State Driven Block Cancel";
        public const string PluginVersion = "3.2.0";
        private static DaggerPerfectCancelPlugin _instance;
        private Harmony _harmony;
        private readonly BlockCancelSequence _sequence = new BlockCancelSequence();
        // Observations only: no SetValue calls or game-field writes.
        private FieldInfo _engineBlocking, _attackInput, _attackHoldInput, _blockInput;
        private MethodInfo _takeInput;
        private ConfigEntry<bool> _enabled, _verbose, _eitherSideButton;
        private ConfigEntry<int> _triggerKey, _holdMs;
        private ConfigEntry<float> _startTimeout, _endTimeout, _blockTimeout;
        private bool _triggerWasDown, _ready, _releasing;
        private Player _owner;
        private ItemDrop.ItemData _weapon;
        private Vector3 _rawMove;
        private bool _rawRun;
        private float _lastControlTime;
        private int _sequenceNumber;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private void Awake()
        {
            _instance = this;
            _enabled = Config.Bind("General", "Enabled", true, "Enable Mouse5 state-driven input automation.");
            _triggerKey = Config.Bind("Input", "TriggerVirtualKey", 6, "Mouse5 = Win32 XBUTTON2 (6).");
            _eitherSideButton = Config.Bind("Input", "AcceptEitherSideButtonFallback", false, "Also accept Mouse4 if mouse software swaps the buttons.");
            _holdMs = Config.Bind("Timing", "SecondAttackHoldMs", 1200, "Real-time duration of the final normal LMB hold, matching the AHK script.");
            _startTimeout = Config.Bind("Safety", "FirstAttackStartTimeoutSeconds", 1.5f, "Abort only; never force an attack on timeout.");
            _endTimeout = Config.Bind("Safety", "FirstAttackEndTimeoutSeconds", 6f, "Watchdog only. Detect attack completion from InAttack, not this timer.");
            _blockTimeout = Config.Bind("Safety", "BlockStartTimeoutSeconds", 1.5f, "Abort if the game does not process blocking.");
            _verbose = Config.Bind("Debug", "VerboseLogging", true, "Log transitions for hit-stop and multiplayer verification.");
            _sequence.Transition = (stage, reason) =>
            {
                var text = "[SDBC #" + _sequenceNumber + "] " + stage + " @" + Time.realtimeSinceStartup.ToString("F3") + " " + reason;
                if (reason.StartsWith("ABORT:")) Logger.LogWarning(text);
                else if (_verbose.Value) Logger.LogInfo(text);
                LogSwordTransition(stage, reason);
            };
            try
            {
                var controls = AccessTools.Method(typeof(Player), "SetControls");
                var attackConsumer = AccessTools.Method(typeof(Player), "PlayerAttackInput");
                var blockUpdate = AccessTools.Method(typeof(Humanoid), "UpdateBlock");
                _takeInput = AccessTools.Method(typeof(Player), "TakeInput");
                _engineBlocking = AccessTools.Field(typeof(Humanoid), "m_internalBlockingState");
                _attackInput = AccessTools.Field(typeof(Character), "m_attack");
                _attackHoldInput = AccessTools.Field(typeof(Character), "m_attackHold");
                _blockInput = AccessTools.Field(typeof(Character), "m_blocking");
                string[] names = { "movedir", "attack", "attackHold", "secondaryAttack", "secondaryAttackHold", "block", "blockHold", "jump", "crouch", "run", "autoRun", "dodge" };
                if (controls == null || !controls.GetParameters().Select(p => p.Name).SequenceEqual(names) ||
                    controls.GetParameters()[0].ParameterType != typeof(Vector3) ||
                    controls.GetParameters().Skip(1).Any(p => p.ParameterType != typeof(bool)) ||
                    attackConsumer == null || blockUpdate == null || _takeInput == null ||
                    _takeInput.ReturnType != typeof(bool) || _takeInput.GetParameters().Length != 0 ||
                    new[] { _engineBlocking, _attackInput, _attackHoldInput, _blockInput }.Any(f => f == null || f.FieldType != typeof(bool)))
                    throw new InvalidOperationException("Unsupported game control API; expected SetControls, PlayerAttackInput, UpdateBlock and read-only bool observations.");

                _harmony = new Harmony(PluginGuid);
                _harmony.Patch(controls, prefix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(ControlsPrefix)) { priority = Priority.Last });
                _harmony.Patch(attackConsumer, postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(AttackInputPostfix)));
                _harmony.Patch(blockUpdate, postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(BlockUpdatePostfix)));
                _ready = true;
                TrySetupSwordFollowup();
                Logger.LogInfo(PluginName + " " + PluginVersion + " loaded. Mouse5; F12 cancel. Normal input prefix + engine block acknowledgement. No attack/stamina mutation.");
                Logger.LogInfo("[SDBC] Game assembly: " + typeof(Player).Assembly.GetName().Version + "; Unity " + Application.unityVersion);
            }
            catch (Exception ex)
            {
                _harmony?.UnpatchSelf();
                Logger.LogError("[SDBC] Disabled: " + ex.Message);
            }
        }

        private void Update()
        {
            if (!_ready) return;
            var down = ReadTrigger();
            var pressed = down && !_triggerWasDown;
            _triggerWasDown = down;
            var now = Time.realtimeSinceStartup;
            _sequence.Rearm(down, now);
            if (_sequence.Running)
            {
                if (ReadKey(0x7B)) { CancelAndRelease("F12 emergency release"); return; }
                if (!_enabled.Value || !CanContinue(_owner)) { CancelAndRelease("input unavailable, interrupted, or weapon/player changed"); return; }
                if (now - _lastControlTime > 1f) { CancelAndRelease("control updates stopped"); return; }
            }
            if (!pressed || !_enabled.Value || _sequence.Current != BlockCancelSequence.Stage.Idle) return;
            var player = Player.m_localPlayer;
            if (!CanUsePlayer(player) || player.InAttack() || player.IsBlocking() || ReadBool(_engineBlocking, player))
            {
                Logger.LogInfo("[SDBC] Mouse5 ignored: player must be ready, with no active attack/block or open UI.");
                return;
            }
            var weapon = player.GetCurrentWeapon();
            if (weapon == null || weapon.m_shared == null || weapon.m_shared.m_attack == null ||
                (weapon.m_shared.m_attack.m_attackType != Attack.AttackType.Horizontal &&
                 weapon.m_shared.m_attack.m_attackType != Attack.AttackType.Vertical))
            {
                Logger.LogInfo("[SDBC] Mouse5 ignored: equip a melee primary weapon (knife is the reference weapon).");
                return;
            }
            _owner = player;
            _weapon = weapon;
            _sequence.HoldSeconds = Mathf.Clamp(_holdMs.Value, 1, 10000) / 1000.0;
            _sequence.StartTimeout = Mathf.Clamp(_startTimeout.Value, .1f, 30f);
            _sequence.EndTimeout = Mathf.Clamp(_endTimeout.Value, .1f, 30f);
            _sequence.BlockTimeout = Mathf.Clamp(_blockTimeout.Value, .1f, 30f);
            _lastControlTime = now;
            ++_sequenceNumber;
            var swordMode = PrepareSwordFollowup(weapon);
            _sequence.Begin(now, swordMode);
        }

        private static void ControlsPrefix(Player __instance, Vector3 movedir, bool run,
            ref bool attack, ref bool attackHold, ref bool secondaryAttack, ref bool secondaryAttackHold,
            ref bool block, ref bool blockHold, bool jump, bool dodge)
        {
            var self = _instance;
            if (self == null || !self._ready || self._releasing || __instance != Player.m_localPlayer) return;
            self._rawMove = movedir;
            self._rawRun = run;
            self._lastControlTime = Time.realtimeSinceStartup;
            if (!self._sequence.Running) return;
            try
            {
                if (!self._enabled.Value || !self.CanContinue(__instance) || jump || dodge)
                    self._sequence.Cancel(Time.realtimeSinceStartup, "player interrupted or input unavailable");
                var input = self._sequence.Step(Time.realtimeSinceStartup, __instance.InAttack());
                if (!input.Override) return;
                attack = input.Attack;
                attackHold = input.AttackHold;
                block = input.Block;
                blockHold = input.BlockHold;
                secondaryAttack = input.SecondaryAttack;
                secondaryAttackHold = input.SecondaryAttackHold;
            }
            catch (Exception ex)
            {
                self._sequence.Cancel(Time.realtimeSinceStartup, "control observation failed: " + ex.Message);
                attack = attackHold = block = blockHold = secondaryAttack = secondaryAttackHold = false;
            }
        }

        private static void AttackInputPostfix(Player __instance)
        {
            var self = _instance;
            if (self == null || !self._ready || __instance != self._owner || __instance != Player.m_localPlayer || !self._sequence.Running) return;
            self._sequence.AttackInputProcessed(ReadBool(self._attackInput, __instance), ReadBool(self._attackHoldInput, __instance),
                ReadBool(self._blockInput, __instance), __instance.InAttack(), Time.realtimeSinceStartup,
                self._swordReady && ReadBool(self._secondaryInput, __instance),
                self._swordReady && ReadBool(self._secondaryHoldInput, __instance));
        }

        private static void BlockUpdatePostfix(Humanoid __instance)
        {
            var self = _instance;
            if (self == null || !self._ready || __instance != self._owner || __instance != Player.m_localPlayer || !self._sequence.Running) return;
            self._sequence.BlockProcessed(__instance.IsBlocking(), ReadBool(self._engineBlocking, __instance));
            self._sequence.ObserveAttack(__instance.InAttack(), Time.realtimeSinceStartup);
        }

        private bool CanContinue(Player player) => CanUsePlayer(player) && player == _owner && ReferenceEquals(player.GetCurrentWeapon(), _weapon);
        private static bool CanUsePlayer(Player player) => Application.isFocused && Time.timeScale > 0f &&
            player != null && player == Player.m_localPlayer && player.gameObject.activeInHierarchy &&
            !player.IsDead() && TakesInput(player) && !player.InPlaceMode() && !player.InDodge() &&
            !player.IsStaggering() && !player.IsAttached() && !player.InMinorAction() && player.GetDoodadController() == null;
        private static bool ReadBool(FieldInfo field, object target) => (bool)field.GetValue(target);
        private static bool TakesInput(Player player) => (bool)_instance._takeInput.Invoke(player, null);
        private static bool ReadKey(int key)
        {
            try { return (GetAsyncKeyState(key) & 0x8000) != 0; }
            catch { return false; }
        }
        private bool ReadTrigger()
        {
            if (ReadKey(_triggerKey.Value)) return true;
            if (_eitherSideButton.Value && ReadKey(_triggerKey.Value == 5 ? 6 : 5)) return true;
            try { return _triggerKey.Value == 6 && Input.GetKey(KeyCode.Mouse4); }
            catch { return false; }
        }

        private void CancelAndRelease(string reason)
        {
            _sequence.Cancel(Time.realtimeSinceStartup, reason);
            if (_owner == null || _owner != Player.m_localPlayer)
            {
                _sequence.OwnerGone(Time.realtimeSinceStartup);
                return;
            }
            try
            {
                _releasing = true;
                var focused = Application.isFocused && TakesInput(_owner);
                // Cleanup also uses normal controls, never private-field writes.
                _owner.SetControls(focused ? _rawMove : Vector3.zero, false, false, false, false,
                    false, false, false, false, focused && _rawRun, false, false);
                _sequence.Step(Time.realtimeSinceStartup, _owner.InAttack());
            }
            catch (Exception ex) { Logger.LogWarning("[SDBC] Input release: " + ex.Message); }
            finally { _releasing = false; }
        }
        private void OnApplicationFocus(bool focused) { if (!focused && _sequence.Running) CancelAndRelease("window lost focus"); }
        private void OnDisable() { if (_ready && _sequence.Running) CancelAndRelease("plugin disabled"); }
        private void OnDestroy()
        {
            if (_ready && _sequence.Running) CancelAndRelease("plugin unloaded");
            _harmony?.UnpatchSelf();
            _swordHarmony?.UnpatchSelf();
            _ready = false;
            if (_instance == this) _instance = null;
        }
    }
}
