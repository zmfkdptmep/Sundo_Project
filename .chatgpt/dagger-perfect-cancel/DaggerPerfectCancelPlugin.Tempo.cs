using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Goni.DaggerPerfectCancel
{
    public sealed partial class DaggerPerfectCancelPlugin
    {
        private Harmony _tempoHarmony;
        private bool _tempoReady, _damageObservationReady, _skillReportPending;
        private ConfigEntry<bool> _tempoEnabled;
        private ConfigEntry<float> _slashSpeed, _recoverySpeed, _tempoBlockSeconds, _maxHitStop;
        private FieldInfo _pauseTimerField, _pauseSpeedField;
        private MethodInfo _hitTotalDamage;
        private readonly AnimationSpeedLease _speedLease = new AnimationSpeedLease();
        private float _skillStartedAt, _firstSlashAt;
        private readonly float[] _observedMultipliers = new float[3];
        private readonly float[] _observedStaminaCosts = new float[3];
        private int _outgoingDamageSamples;
        private float _outgoingDamageMin, _outgoingDamageMax;

        private void SetupSwordTempo()
        {
            _tempoEnabled = Config.Bind("SwordTempo", "Enabled", true, "Accelerate only this Mouse5 sword skill. False restores 3.3.1 animation timing.");
            _slashSpeed = Config.Bind("SwordTempo", "SlashSpeedMultiplier", 2.4f, "Animation playback multiplier for the three sword slashes (1 to 3.5).");
            _recoverySpeed = Config.Bind("SwordTempo", "RecoverySpeedMultiplier", 4f, "Playback multiplier after each confirmed melee event, including opening recovery (1 to 6).");
            _tempoBlockSeconds = Config.Bind("SwordTempo", "BlockPoseSeconds", .10f, "Short visible block within the skill. Separate from the old 3.3.1 timing setting.");
            _maxHitStop = Config.Bind("SwordTempo", "MaxHitStopSeconds", .025f, "Cap this skill's hit-stop; unrelated attacks keep their native freeze duration.");
            try
            {
                _pauseTimerField = RequiredField(typeof(CharacterAnimEvent), "m_pauseTimer", typeof(float));
                _pauseSpeedField = RequiredField(typeof(CharacterAnimEvent), "m_pauseSpeed", typeof(float));
                var speed = AccessTools.Method(typeof(CharacterAnimEvent), "Speed", new[] { typeof(float) });
                var fixedUpdate = AccessTools.Method(typeof(CharacterAnimEvent), "CustomFixedUpdate", new[] { typeof(float) });
                var freeze = AccessTools.Method(typeof(CharacterAnimEvent), "FreezeFrame", new[] { typeof(float) });
                if (speed == null || fixedUpdate == null || freeze == null) throw new MissingMethodException("CharacterAnimEvent tempo API");
                _tempoHarmony = new Harmony(PluginGuid + ".swordtempo");
                _tempoHarmony.Patch(speed, prefix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SkillSpeedEvent)) { priority = Priority.Last });
                _tempoHarmony.Patch(fixedUpdate, postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SkillFixedAnimationUpdate)));
                _tempoHarmony.Patch(freeze, prefix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SkillFreezeFrame)) { priority = Priority.Last });
                _tempoReady = true;
            }
            catch (Exception ex)
            {
                _tempoHarmony?.UnpatchSelf();
                _tempoReady = false;
                Logger.LogWarning("[SwordTempo] Timing extension unavailable; native skill timing retained: " + ex.Message);
            }
            // Optional, read-only damage sampling; reflected to tolerate API differences.
            var modifyDamage = AccessTools.Method(typeof(Attack), "ModifyDamage", new[] { typeof(HitData), typeof(float) });
            _hitTotalDamage = AccessTools.Method(typeof(HitData), "GetTotalDamage", Type.EmptyTypes);
            if (modifyDamage != null && _hitTotalDamage != null && _hitTotalDamage.ReturnType == typeof(float))
            {
                try
                {
                    _swordHarmony.Patch(modifyDamage, postfix: new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), nameof(SkillOutgoingDamage)));
                    _damageObservationReady = true;
                }
                catch (Exception ex) { Logger.LogWarning("[SwordSkill] Damage sampling unavailable: " + ex.Message); }
            }
        }
        private bool TempoActive => _tempoReady && _tempoEnabled.Value && _swordReady && _skill.Running
            && _skillOwner != null && _skillOwner == Player.m_localPlayer;
        private float CurrentTempoFactor()
        {
            if (!TempoActive || !SkillOwnerValid() || _skillAttack == null
                || !ReferenceEquals(_currentAttack.GetValue(_skillOwner), _skillAttack)) return 1f;
            var stage = _skill.Current;
            if (stage != SwordSkillSequence.Stage.OpeningSwing && stage != SwordSkillSequence.Stage.SlashSwing) return 1f;
            if (_skill.EventSeen)
                return _skillOwner.InAttack() ? Mathf.Clamp(_recoverySpeed.Value, 1f, 6f) : 1f;
            return stage == SwordSkillSequence.Stage.SlashSwing ? Mathf.Clamp(_slashSpeed.Value, 1f, 3.5f) : 1f;
        }
        private void ApplySwordTempo()
        {
            if (!_tempoReady || _skillAnimator == null || _skillAnimEvent == null) return;
            bool frozen = (float)_pauseTimerField.GetValue(_skillAnimEvent) > 0f;
            float current = _skillAnimator.speed;
            float next = _speedLease.Apply(current, CurrentTempoFactor(), frozen);
            if (Mathf.Abs(next - current) > .00001f) _skillAnimator.speed = next;
        }
        private void RestoreSwordTempo()
        {
            try
            {
                if (!_tempoReady || _skillAnimator == null) return;
                // A freeze saves the boosted speed for later restoration. Remove
                // our multiplier there too, without breaking the active freeze.
                if (_skillAnimEvent != null && (float)_pauseTimerField.GetValue(_skillAnimEvent) > 0f)
                {
                    float saved = (float)_pauseSpeedField.GetValue(_skillAnimEvent);
                    float native = _speedLease.UnscaleIfOwned(saved);
                    if (Mathf.Abs(saved - native) > .00001f) _pauseSpeedField.SetValue(_skillAnimEvent, native);
                }
                float current = _skillAnimator.speed;
                float restored = _speedLease.UnscaleIfOwned(current);
                if (Mathf.Abs(current - restored) > .00001f) _skillAnimator.speed = restored;
            }
            finally { _speedLease.Reset(); }
        }
        private static void SkillSpeedEvent(CharacterAnimEvent __instance, ref float __0)
        {
            var self = _instance;
            if (self == null || !self.TempoActive || __instance != self._skillAnimEvent) return;
            try
            {
                float scaled = self._speedLease.AnimationEvent(__0, self.CurrentTempoFactor());
                if ((float)self._pauseTimerField.GetValue(__instance) > 0f && scaled > .01f)
                {
                    self._pauseSpeedField.SetValue(__instance, scaled);
                    __0 = self._skillAnimator.speed; // Keep the current freeze until vanilla releases it.
                }
                else __0 = scaled;
            }
            catch (Exception ex) { self.DisableSwordSkill(ex); }
        }
        private static void SkillFixedAnimationUpdate(CharacterAnimEvent __instance, bool __runOriginal)
        {
            var self = _instance;
            if (!__runOriginal || self == null || !self.TempoActive || __instance != self._skillAnimEvent) return;
            try { self.ApplySwordTempo(); }
            catch (Exception ex) { self.DisableSwordSkill(ex); }
        }
        private static void SkillFreezeFrame(CharacterAnimEvent __instance, ref float __0)
        {
            var self = _instance;
            if (self == null || !self.TempoActive || __instance != self._skillAnimEvent) return;
            try
            {
                if (self.SkillOwnerValid() && self._skillAttack != null
                    && ReferenceEquals(self._currentAttack.GetValue(self._skillOwner), self._skillAttack))
                    __0 = Mathf.Min(__0, Mathf.Clamp(self._maxHitStop.Value, .01f, .10f));
            }
            catch (Exception ex) { self.DisableSwordSkill(ex); }
        }
        private void BeginSwordReport()
        {
            _skillStartedAt = Time.realtimeSinceStartup;
            _firstSlashAt = -1f;
            _outgoingDamageSamples = 0; _outgoingDamageMin = float.PositiveInfinity; _outgoingDamageMax = 0f;
            for (int i = 0; i < 3; ++i) { _observedMultipliers[i] = float.NaN; _observedStaminaCosts[i] = float.NaN; }
            _skillReportPending = true;
            _speedLease.Reset();
        }
        private void ObserveSwordMelee(Attack attack)
        {
            if (_skill.Current != SwordSkillSequence.Stage.SlashSwing) return;
            int index = _skill.SlashesCompleted - 1;
            if (index < 0 || index >= 3) return;
            _observedMultipliers[index] = attack.m_damageMultiplier;
            _observedStaminaCosts[index] = attack.m_attackStamina;
        }
        private static void SkillOutgoingDamage(Attack __instance, HitData __0, bool __runOriginal)
        {
            var self = _instance;
            if (!__runOriginal || self == null || !self._damageObservationReady || !self._skill.Running
                || self._skill.Current != SwordSkillSequence.Stage.SlashSwing || !ReferenceEquals(__instance, self._skillAttack)) return;
            try
            {
                float damage = (float)self._hitTotalDamage.Invoke(__0, null);
                ++self._outgoingDamageSamples;
                self._outgoingDamageMin = Mathf.Min(self._outgoingDamageMin, damage);
                self._outgoingDamageMax = Mathf.Max(self._outgoingDamageMax, damage);
            }
            catch (Exception ex)
            {
                self._damageObservationReady = false;
                self.Logger.LogWarning("[SwordSkill] Damage sampling stopped: " + ex.Message);
            }
        }
        private void ReportSwordSkill()
        {
            if (!_skillReportPending) return;
            _skillReportPending = false;
            string profile = string.Join(",", Array.ConvertAll(_observedMultipliers, x => float.IsNaN(x) ? "none" : x.ToString("F2")));
            string stamina = string.Join(",", Array.ConvertAll(_observedStaminaCosts, x => float.IsNaN(x) ? "none" : x.ToString("F2")));
            string outgoing = _outgoingDamageSamples == 0 ? "none" : _outgoingDamageMin.ToString("F1") + ".." + _outgoingDamageMax.ToString("F1");
            Logger.LogInfo("[SwordSkill #" + _skillNumber + "] summary: durationSeconds=" + (Time.realtimeSinceStartup - _skillStartedAt).ToString("F3")
                + "; tripleSeconds=" + (_firstSlashAt < 0f ? "none" : (Time.realtimeSinceStartup - _firstSlashAt).ToString("F3"))
                + "; slashDamageMultipliers=[" + profile + "]; slashAttackStamina=[" + stamina + "]"
                + "; outgoingDamageSamples=" + _outgoingDamageSamples + "; outgoingPreDefenseDamage=" + outgoing
                + ". Outgoing samples precede final status effects/target armor; not enemy HP loss.");
        }
    }
}
