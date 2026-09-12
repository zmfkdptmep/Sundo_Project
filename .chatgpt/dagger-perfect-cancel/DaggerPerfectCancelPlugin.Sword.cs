using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Goni.DaggerPerfectCancel
{
    public sealed partial class DaggerPerfectCancelPlugin
    {
        private Harmony _swordHarmony;
        private bool _swordReady, _startingSkillAttack, _startingSkillSlash, _allowSkillChain;
        private ConfigEntry<bool> _swordEnabled;
        private ConfigEntry<float> _swordTimeout, _blockPoseSeconds;
        private readonly SwordSkillSequence _skill = new SwordSkillSequence();
        private FieldInfo _currentAttack, _animatorField, _animEventField, _queuedPrimary, _queuedSecondary;
        private MethodInfo _getCurrentBlocker;
        private Player _skillOwner;
        private ItemDrop.ItemData _skillWeapon;
        private Attack _skillAttack, _preparedAttack;
        private Animator _skillAnimator;
        private CharacterAnimEvent _skillAnimEvent;
        private string[] _slashTriggers;
        private int _skillNumber;
        private readonly List<AnimatorClipInfo> _blockClips = new List<AnimatorClipInfo>(8);
        private sealed class SkillAttackStamp { internal bool Consumed, Cancelled; }
        // Weak keys retain deduplication after completion/cancellation without
        // retaining attacks/players over a long play session.
        private readonly ConditionalWeakTable<Attack, SkillAttackStamp> _skillAttacks = new ConditionalWeakTable<Attack, SkillAttackStamp>();

        private void TrySetupSwordSkill()
        {
            try
            {
                _swordEnabled = Config.Bind("SwordSkill", "Enabled", true,
                    "Mouse5 with a sword: one primary -> visible block -> three sword slashes using secondary damage. Replaces the old dual-input exploit.");
                _swordTimeout = Config.Bind("SwordSkill", "StageTimeoutSeconds", 8f, "Abort watchdog per stage; not attack timing.");
                _blockPoseSeconds = Config.Bind("SwordSkill", "VisibleBlockSeconds", .18f, "Minimum observed block animation before the triple slash.");
                _currentAttack = RequiredField(typeof(Humanoid), "m_currentAttack", typeof(Attack));
                _animatorField = RequiredField(typeof(Character), "m_animator", typeof(Animator));
                _animEventField = RequiredField(typeof(Character), "m_animEvent", typeof(CharacterAnimEvent));
                _queuedPrimary = RequiredField(typeof(Player), "m_queuedAttackTimer", typeof(float));
                _queuedSecondary = RequiredField(typeof(Player), "m_queuedSecondAttackTimer", typeof(float));
                _getCurrentBlocker = AccessTools.Method(typeof(Humanoid), "GetCurrentBlocker", Type.EmptyTypes);
                if (_getCurrentBlocker == null) throw new MissingMethodException("Humanoid.GetCurrentBlocker");
                _swordHarmony = new Harmony(PluginGuid + ".swordskill");
                PatchSkill(typeof(Player), "PlayerAttackInput", nameof(SkillAttackInputPrefix), null);
                PatchSkill(typeof(Humanoid), "StartAttack", nameof(SkillStartGate), null);
                PatchSkill(typeof(Attack), "Start", nameof(SkillPrepareAttack), null);
                PatchSkill(typeof(Player), "HaveQueuedChain", nameof(SkillChainGate), null);
                PatchSkill(typeof(Attack), "DoMeleeAttack", nameof(SkillMeleePrefix), nameof(SkillMeleePostfix));
                _skill.Transition = (stage, reason) =>
                {
                    if (_verbose.Value || stage == SwordSkillSequence.Stage.WaitRelease)
                        Logger.LogInfo("[SwordSkill #" + _skillNumber + "] " + stage + ": " + reason);
                };
                SetupSwordGuard();
                _swordReady = true;
                Logger.LogInfo("[SwordSkill] Ready: Mouse5 = primary -> visible block -> three secondary-damage sword slashes. F9 toggles visible sword guard.");
            }
            catch (Exception ex)
            {
                _swordHarmony?.UnpatchSelf();
                _swordReady = false;
                Logger.LogError("[SwordSkill] Disabled; legacy block cancel remains available: " + ex.Message);
            }
        }
        private static FieldInfo RequiredField(Type type, string name, Type expected)
        {
            var field = AccessTools.Field(type, name);
            if (field == null || field.FieldType != expected) throw new MissingFieldException(type.Name, name);
            return field;
        }
        private void PatchSkill(Type type, string name, string prefix, string postfix)
        {
            var method = AccessTools.Method(type, name);
            if (method == null) throw new MissingMethodException(type.Name, name);
            _swordHarmony.Patch(method,
                prefix: prefix == null ? null : new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), prefix) { priority = Priority.Last },
                postfix: postfix == null ? null : new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), postfix));
        }
        private bool TryBeginSwordSkill(Player player, double now)
        {
            var weapon = player == null ? null : player.GetCurrentWeapon();
            if (!_swordReady || !_swordEnabled.Value || !IsSword(weapon)) return false;
            // Returning true means this press belongs to the sword feature, even
            // if it is rejected. Never silently run the obsolete sword macro.
            if (_skill.Current != SwordSkillSequence.Stage.Idle || !CanUsePlayer(player)
                || player.InAttack() || (player.IsBlocking() && !_guardApplied)) return true;
            if (!weapon.HaveSecondaryAttack() || weapon.m_shared.m_attack == null
                || weapon.m_shared.m_secondaryAttack == null
                || !IsMelee(weapon.m_shared.m_attack) || !IsMelee(weapon.m_shared.m_secondaryAttack))
            {
                Logger.LogWarning("[SwordSkill] Unsupported sword attack profile."); return true;
            }
            var animator = (Animator)_animatorField.GetValue(player);
            var animEvent = (CharacterAnimEvent)_animEventField.GetValue(player);
            var primary = weapon.m_shared.m_attack;
            if (animator == null || animEvent == null || primary.m_attackChainLevels < 3)
            {
                Logger.LogWarning("[SwordSkill] This sword needs a three-stage primary animation."); return true;
            }
            var triggers = new[] { primary.m_attackAnimation + "0", primary.m_attackAnimation + "1", primary.m_attackAnimation + "2" };
            var parameters = animator.parameters;
            foreach (var trigger in triggers)
            {
                if (!Array.Exists(parameters, p => p.type == AnimatorControllerParameterType.Trigger && p.name == trigger))
                { Logger.LogWarning("[SwordSkill] Missing animation trigger: " + trigger); return true; }
            }
            StopSwordGuard(false);
            _skillOwner = player; _skillWeapon = weapon; _skillAnimator = animator; _skillAnimEvent = animEvent;
            _skillAttack = _preparedAttack = null; _slashTriggers = triggers;
            _skill.Timeout = Mathf.Clamp(_swordTimeout.Value, 1f, 30f);
            _skill.BlockPoseSeconds = Mathf.Clamp(_blockPoseSeconds.Value, .10f, 1f);
            ++_skillNumber;
            _lastControlTime = Time.realtimeSinceStartup;
            ClearSkillQueues(player);
            SetCombatNeutral(player);
            _skill.Begin(now);
            Logger.LogInfo("[SwordSkill #" + _skillNumber + "] weapon=" + weapon.m_shared.m_name
                + "; secondaryDamageMultiplier=" + weapon.m_shared.m_secondaryAttack.m_damageMultiplier
                + "; slashAnimations=" + string.Join(",", triggers) + "; slashStamina=0");
            return true;
        }
        private static bool IsSword(ItemDrop.ItemData weapon) => weapon?.m_shared != null && weapon.m_shared.m_skillType == Skills.SkillType.Swords;
        private static bool IsMelee(Attack attack) => attack != null &&
            (attack.m_attackType == Attack.AttackType.Horizontal || attack.m_attackType == Attack.AttackType.Vertical);
        private bool SkillOwnerValid() => _enabled.Value && _swordEnabled.Value && CanUsePlayer(_skillOwner)
            && ReferenceEquals(_skillOwner.GetCurrentWeapon(), _skillWeapon);

        private void TickSwordSkill(bool triggerHeld)
        {
            if (!_swordReady) return;
            double now = Time.time;
            _skill.Rearm(triggerHeld, now);
            if (!_skill.Running) return;
            if (!SkillOwnerValid()) { CancelSwordSkill("player/UI/focus/weapon interruption"); return; }
            if (Time.realtimeSinceStartup - _lastControlTime > 1f)
            { CancelSwordSkill("control updates stopped"); return; }
            bool inAttack = _skillOwner.InAttack();
            bool currentMatches = ReferenceEquals(_currentAttack.GetValue(_skillOwner), _skillAttack);
            if (_skillAttack != null && !currentMatches && inAttack)
            { CancelSwordSkill("another attack replaced this skill"); return; }
            bool canChain = currentMatches && _skill.EventSeen && _skillAnimEvent.CanChain();
            bool blocking = _skillOwner.IsBlocking() || ReadBool(_engineBlocking, _skillOwner);
            bool visibleBlock = _skill.WantsBlock && ReadBool(_engineBlocking, _skillOwner)
                && _skillAnimator.GetBool("blocking") && HasBlockClip(_skillAnimator);
            _skill.Tick(now, inAttack, canChain, blocking, visibleBlock, Time.frameCount);
            if (!_skill.Running)
            {
                if (_skill.Result != null && _skill.Result.Contains("watchdog at Block"))
                    LogBlockClips();
                ReleaseSwordSkill(); return;
            }
            if (!_skill.WantsStart) return;
            _startingSkillAttack = true;
            _startingSkillSlash = _skill.IsSlash;
            _allowSkillChain = _startingSkillSlash && canChain;
            _preparedAttack = null;
            try
            {
                // Normal game attack startup creates a fresh Attack clone and
                // plays its networked animation. Only that clone is configured.
                bool accepted = _skillOwner.StartAttack(null, false);
                if (!accepted) return;
                var actual = (Attack)_currentAttack.GetValue(_skillOwner);
                if (actual == null || !ReferenceEquals(actual, _preparedAttack))
                { CancelSwordSkill("attack startup was replaced by another patch"); return; }
                _skillAttack = actual;
                _skillAttacks.Add(actual, new SkillAttackStamp());
                _skill.Accepted(now);
            }
            catch (Exception ex) { CancelSwordSkill("attack startup failed: " + ex.Message); }
            finally { _startingSkillAttack = _startingSkillSlash = _allowSkillChain = false; }
        }
        private void LogBlockClips()
        {
            if (_skillAnimator == null) return;
            var names = new List<string>();
            for (int layer = 0; layer < _skillAnimator.layerCount; ++layer)
            {
                _blockClips.Clear();
                _skillAnimator.GetCurrentAnimatorClipInfo(layer, _blockClips);
                foreach (var clip in _blockClips)
                    if (clip.clip != null) names.Add(layer + ":" + clip.clip.name + "@" + clip.weight.ToString("F2"));
            }
            Logger.LogWarning("[SwordSkill] Block motion not acknowledged. Current clips: " + string.Join(", ", names));
        }
        private bool HasBlockClip(Animator animator)
        {
            for (int layer = 0; layer < animator.layerCount; ++layer)
            {
                if (layer != 0 && animator.GetLayerWeight(layer) < .01f) continue;
                _blockClips.Clear();
                animator.GetCurrentAnimatorClipInfo(layer, _blockClips);
                foreach (var clip in _blockClips)
                    if (clip.weight > .05f && clip.clip != null
                        && clip.clip.name.IndexOf("block", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }
        private bool ApplySwordSkillControls(Player player, ref bool attack, ref bool attackHold,
            ref bool secondary, ref bool secondaryHold, ref bool block, ref bool blockHold, bool jump, bool dodge)
        {
            if (!_skill.Running || player != _skillOwner) return false;
            if (jump || dodge || !SkillOwnerValid()) CancelSwordSkill("jump/dodge/input interruption");
            attack = attackHold = secondary = secondaryHold = false;
            block = blockHold = _skill.Running && _skill.WantsBlock;
            return true;
        }
        private static bool SkillAttackInputPrefix(Player __instance)
        {
            var self = _instance;
            if (self == null || !self._swordReady || !self._skill.Running || __instance != self._skillOwner) return true;
            self.ClearSkillQueues(__instance);
            return false; // The skill owns attack startup; no parallel input queue.
        }
        private static bool SkillStartGate(Humanoid __instance, ref bool __result)
        {
            var self = _instance;
            if (self == null || !self._skill.Running || __instance != self._skillOwner || self._startingSkillAttack) return true;
            __result = false; return false;
        }
        private static bool SkillChainGate(Player __instance, ref bool __result)
        {
            var self = _instance;
            if (self == null || !self._startingSkillAttack || __instance != self._skillOwner) return true;
            __result = self._allowSkillChain; return false;
        }
        private static void SkillPrepareAttack(Attack __instance, Humanoid character, ItemDrop.ItemData weapon,
            ref Attack previousAttack, ref float timeSinceLastAttack)
        {
            var self = _instance;
            if (self == null || !self._startingSkillAttack || character != self._skillOwner
                || !ReferenceEquals(weapon, self._skillWeapon)) return;
            // Shared item data/prefabs are never edited. Attack.Start sees the
            // fresh clone already allocated by vanilla Humanoid.StartAttack.
            self._preparedAttack = __instance;
            previousAttack = null; timeSinceLastAttack = 0f;
            if (!self._startingSkillSlash) return;
            var secondary = weapon.m_shared.m_secondaryAttack;
            __instance.m_attackAnimation = self._slashTriggers[self._skill.SlashesStarted];
            __instance.m_attackChainLevels = 0; // Explicit 0/1/2 animation triggers; no vanilla final-hit 2x bonus.
            __instance.m_attackRandomAnimations = 1;
            __instance.m_damageMultiplier = secondary.m_damageMultiplier;
            __instance.m_damageMultiplierPerMissingHP = secondary.m_damageMultiplierPerMissingHP;
            __instance.m_damageMultiplierByTotalHealthMissing = secondary.m_damageMultiplierByTotalHealthMissing;
            __instance.m_forceMultiplier = secondary.m_forceMultiplier;
            __instance.m_staggerMultiplier = secondary.m_staggerMultiplier;
            __instance.m_lowerDamagePerHit = secondary.m_lowerDamagePerHit;
            __instance.m_lastChainDamageMultiplier = 1f;
            __instance.m_attackStamina = 0f;
            __instance.m_staminaReturnPerMissingHP = 0f;
        }
        private static bool SkillMeleePrefix(Attack __instance)
        {
            var self = _instance;
            if (self == null || !self._skillAttacks.TryGetValue(__instance, out var stamp)) return true;
            if (stamp.Consumed || stamp.Cancelled || !self._skill.Running
                || !ReferenceEquals(__instance, self._skillAttack)) return false;
            if (!self.SkillOwnerValid()) { self.CancelSwordSkill("interrupted before melee event"); return false; }
            // Several targets in a swing are processed by vanilla in ONE call.
            // A duplicate animation event must not deal a fourth hit.
            stamp.Consumed = true;
            return true;
        }
        private static void SkillMeleePostfix(Attack __instance, bool __runOriginal)
        {
            var self = _instance;
            if (!__runOriginal || self == null || !self._skill.Running || !ReferenceEquals(__instance, self._skillAttack)) return;
            if (self._skill.MeleeEvent() && self._verbose.Value)
                self.Logger.LogInfo("[SwordSkill #" + self._skillNumber + "] melee: opening=" + self._skill.OpeningEvents
                    + "; slashes=" + self._skill.SlashesCompleted + "/3; damageMultiplier=" + __instance.m_damageMultiplier
                    + "; attackStamina=" + __instance.m_attackStamina);
        }
        private void ClearSkillQueues(Player player)
        {
            _queuedPrimary.SetValue(player, 0f); _queuedSecondary.SetValue(player, 0f);
        }
        private void SetCombatNeutral(Player player)
        {
            if (player == null || player != Player.m_localPlayer) return;
            bool old = _releasing;
            try
            {
                _releasing = true;
                bool focused = Application.isFocused && TakesInput(player);
                player.SetControls(focused ? _rawMove : Vector3.zero, false, false, false, false,
                    false, false, false, false, focused && _rawRun, false, false);
            }
            finally { _releasing = old; }
        }
        private void CancelSwordSkill(string reason)
        {
            if (!_skill.Running) return;
            if (_skillAttack != null && _skillAttacks.TryGetValue(_skillAttack, out var stamp)) stamp.Cancelled = true;
            if (_skillAttack != null && _skillOwner != null
                && ReferenceEquals(_currentAttack.GetValue(_skillOwner), _skillAttack)) _skillAttack.Abort();
            _skill.Cancel(Time.time, reason);
            ReleaseSwordSkill();
        }
        private void ReleaseSwordSkill()
        {
            if (_skillOwner != null && _skillOwner == Player.m_localPlayer)
            { ClearSkillQueues(_skillOwner); SetCombatNeutral(_skillOwner); }
            _skillAttack = _preparedAttack = null;
            _skillOwner = null; _skillWeapon = null; _skillAnimator = null; _skillAnimEvent = null;
        }
    }
}
