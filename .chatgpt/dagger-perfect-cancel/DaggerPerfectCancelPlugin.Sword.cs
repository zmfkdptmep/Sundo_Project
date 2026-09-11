using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Goni.DaggerPerfectCancel
{
    public sealed partial class DaggerPerfectCancelPlugin
    {
        private Harmony _swordHarmony;
        private bool _swordReady;
        private ConfigEntry<bool> _swordEnabled;
        private ConfigEntry<float> _swordHoldTimeout;
        private FieldInfo _currentAttack, _currentAttackIsSecondary, _secondaryInput, _secondaryHoldInput;
        private Attack _secondSwordAttack;
        private int _swordStaminaCalls;
        private float _swordStaminaSpent, _lastSecondaryMultiplier;

        private void TrySetupSwordFollowup()
        {
            try
            {
                _swordEnabled = Config.Bind("Sword", "Enabled", true,
                    "Swords only: primary -> block -> one primary -> hold primary + secondary until three secondary melee events.");
                _swordHoldTimeout = Config.Bind("Sword", "DualHoldTimeoutSeconds", 6f,
                    "Abort limit only, not attack timing. Release both inputs if three secondary melee events never arrive.");
                _currentAttack = AccessTools.Field(typeof(Humanoid), "m_currentAttack");
                _currentAttackIsSecondary = AccessTools.Field(typeof(Humanoid), "m_currentAttackIsSecondary");
                _secondaryInput = AccessTools.Field(typeof(Character), "m_secondaryAttack");
                _secondaryHoldInput = AccessTools.Field(typeof(Character), "m_secondaryAttackHold");
                var start = AccessTools.Method(typeof(Humanoid), "StartAttack", new[] { typeof(Character), typeof(bool) });
                var melee = AccessTools.Method(typeof(Attack), "DoMeleeAttack", Type.EmptyTypes);
                var stamina = AccessTools.Method(typeof(Player), "UseStamina", new[] { typeof(float) });
                if (_currentAttack == null || _currentAttack.FieldType != typeof(Attack)
                    || _currentAttackIsSecondary == null || _currentAttackIsSecondary.FieldType != typeof(bool)
                    || _secondaryInput == null || _secondaryInput.FieldType != typeof(bool)
                    || _secondaryHoldInput == null || _secondaryHoldInput.FieldType != typeof(bool)
                    || start == null || start.ReturnType != typeof(bool)
                    || melee == null || melee.ReturnType != typeof(void)
                    || stamina == null || stamina.ReturnType != typeof(void))
                    throw new InvalidOperationException("Unsupported sword observation API.");

                // Independently removable observations; failure cannot unpatch the
                // already-working core input / block-cancel hooks.
                _swordHarmony = new Harmony(PluginGuid + ".sword");
                _swordHarmony.Patch(start, postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SwordAttackStarted)));
                _swordHarmony.Patch(melee, postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SwordMeleeCompleted)));
                _swordHarmony.Patch(stamina,
                    prefix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SwordStaminaBefore)),
                    postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SwordStaminaAfter)));
                _swordReady = true;
                Logger.LogInfo("[SDBC] Sword follow-up ready: one post-block primary, then dual hold; three secondary melee events end the input.");
            }
            catch (Exception ex)
            {
                _swordHarmony?.UnpatchSelf();
                _swordReady = false;
                Logger.LogWarning("[SDBC] Sword extension unavailable; original 3.1 input flow remains available: " + ex.Message);
            }
        }

        private bool PrepareSwordFollowup(ItemDrop.ItemData weapon)
        {
            _secondSwordAttack = null;
            _swordStaminaCalls = 0;
            _swordStaminaSpent = _lastSecondaryMultiplier = 0f;
            if (!_swordReady || !_swordEnabled.Value || weapon.m_shared.m_skillType != Skills.SkillType.Swords
                || !weapon.HaveSecondaryAttack()) return false;
            var secondary = weapon.m_shared.m_secondaryAttack;
            if (secondary == null || (secondary.m_attackType != Attack.AttackType.Horizontal
                && secondary.m_attackType != Attack.AttackType.Vertical))
            {
                Logger.LogWarning("[SDBC] Sword secondary is not a supported melee attack; using original 3.1 input flow.");
                return false;
            }
            _sequence.SwordHoldTimeout = Mathf.Clamp(_swordHoldTimeout.Value, .5f, 30f);
            return true;
        }

        private static DaggerPerfectCancelPlugin ActiveSword()
        {
            var self = _instance;
            return self != null && self._ready && self._swordReady && self._sequence.SwordMode
                && self._sequence.Running && self._owner != null && self._owner == Player.m_localPlayer
                ? self : null;
        }

        private static void SwordAttackStarted(Humanoid __instance, bool secondaryAttack, bool __result)
        {
            var self = ActiveSword();
            if (self == null || __instance != self._owner || secondaryAttack || !__result) return;
            try
            {
                if (self._sequence.PrimaryAttackAccepted(Time.realtimeSinceStartup))
                    self._secondSwordAttack = (Attack)self._currentAttack.GetValue(__instance);
            }
            catch (Exception ex) { self.CancelAndRelease("sword start observation failed: " + ex.Message); }
        }

        private static void SwordMeleeCompleted(Attack __instance)
        {
            var self = ActiveSword();
            if (self == null) return;
            try
            {
                // DoMeleeAttack runs once per swing, even on a miss. Do not count
                // damaged targets, foreign players, or stale attack instances.
                if (!ReferenceEquals(self._currentAttack.GetValue(self._owner), __instance)) return;
                if (!self.CanContinue(self._owner)) { self.CancelAndRelease("sword owner interrupted"); return; }
                var secondary = ReadBool(self._currentAttackIsSecondary, self._owner);
                if (!secondary && ReferenceEquals(__instance, self._secondSwordAttack))
                    self._sequence.SecondPrimaryMeleeObserved();
                else if (secondary && self._sequence.Current == BlockCancelSequence.Stage.SwordDualHold)
                {
                    self._lastSecondaryMultiplier = __instance.m_damageMultiplier;
                    if (self._sequence.SecondaryMeleeObserved(Time.realtimeSinceStartup)
                        && self._sequence.Current == BlockCancelSequence.Stage.Releasing)
                    {
                        // Clear both holds via normal SetControls immediately;
                        // don't leave them active until the next rendering frame.
                        self.CancelAndRelease("three sword secondary melee events observed");
                    }
                }
            }
            catch (Exception ex) { self.CancelAndRelease("sword melee observation failed: " + ex.Message); }
        }

        private static void SwordStaminaBefore(Player __instance, out float __state)
        {
            var self = ActiveSword();
            __state = self != null && __instance == self._owner
                && self._sequence.Current == BlockCancelSequence.Stage.SwordDualHold
                ? __instance.GetStamina() : float.NaN;
        }

        private static void SwordStaminaAfter(Player __instance, float __state)
        {
            var self = _instance;
            if (float.IsNaN(__state) || self == null || __instance != self._owner) return;
            var spent = Mathf.Max(0f, __state - __instance.GetStamina());
            if (spent > 0f) { ++self._swordStaminaCalls; self._swordStaminaSpent += spent; }
        }

        private void LogSwordTransition(BlockCancelSequence.Stage stage, string reason)
        {
            if (!_sequence.SwordMode) return;
            if (stage == BlockCancelSequence.Stage.FirstPress)
                Logger.LogInfo("[SDBC #" + _sequenceNumber + "] Sword mode: primary -> block -> ONE primary -> primary+secondary hold.");
            else if (stage == BlockCancelSequence.Stage.Releasing)
                Logger.LogInfo("[SDBC #" + _sequenceNumber + "] Sword observations: secondaryMeleeEvents="
                    + _sequence.SecondarySwingCount + "/3; lastSecondaryDamageMultiplier=" + _lastSecondaryMultiplier.ToString("F3")
                    + "; staminaSpentDuringDualHold=" + _swordStaminaSpent.ToString("F3")
                    + "; staminaUseCalls=" + _swordStaminaCalls
                    + ". Stamina observation includes movement/other UseStamina calls; events do not prove hits or visual animation. " + reason);
        }
    }
}
