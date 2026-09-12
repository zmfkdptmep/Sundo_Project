using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Goni.DaggerPerfectCancel
{
    public sealed partial class DaggerPerfectCancelPlugin
    {
        private Harmony _swordHarmony;
        private bool _swordReady, _swordFaulted, _insideSwordConsumer;
        private ConfigEntry<bool> _swordEnabled;
        private ConfigEntry<float> _dualTimeout;
        private readonly SwordInputSequence _swordInput = new SwordInputSequence();
        private FieldInfo _currentAttack, _currentSecondary, _secondaryInput, _secondaryHoldInput;
        private FieldInfo _queuedPrimary, _queuedSecondary, _animatorField;
        private Player _swordOwner;
        private ItemDrop.ItemData _swordWeapon;
        private Attack _postBlockAttack;
        private Animator _swordAnimator;
        private int _swordNumber, _postBlockMelee, _dualPrimaryMelee, _dualPrimaryStarts, _dualSecondaryStarts;
        private float _swordBeganAt, _dualBeganAt, _dualStaminaSpent;
        private bool _reported;
        private readonly List<AnimatorClipInfo> _swordClips = new List<AnimatorClipInfo>(8);

        private void TrySetupSwordInput()
        {
            try
            {
                _swordEnabled = Config.Bind("SwordInput", "Enabled", true,
                    "Mouse5: primary -> block -> ONE primary click -> hold primary+secondary together. Native inputs only.");
                _dualTimeout = Config.Bind("SwordInput", "DualHoldTimeoutSeconds", 8f,
                    "Abort watchdog only. Stop after three observed secondary-profile melee events; never synthesize hits.");
                _currentAttack = RequiredField(typeof(Humanoid), "m_currentAttack", typeof(Attack));
                _currentSecondary = RequiredField(typeof(Humanoid), "m_currentAttackIsSecondary", typeof(bool));
                _secondaryInput = RequiredField(typeof(Character), "m_secondaryAttack", typeof(bool));
                _secondaryHoldInput = RequiredField(typeof(Character), "m_secondaryAttackHold", typeof(bool));
                _queuedPrimary = RequiredField(typeof(Player), "m_queuedAttackTimer", typeof(float));
                _queuedSecondary = RequiredField(typeof(Player), "m_queuedSecondAttackTimer", typeof(float));
                _animatorField = RequiredField(typeof(Character), "m_animator", typeof(Animator));
                _swordHarmony = new Harmony(PluginGuid + ".swordinput");
                PatchSword(typeof(Player), "PlayerAttackInput", new[] { typeof(float) }, nameof(SwordConsumerPrefix), nameof(SwordConsumerPostfix), nameof(SwordConsumerFinalizer));
                PatchSword(typeof(Humanoid), "StartAttack", new[] { typeof(Character), typeof(bool) }, null, nameof(SwordAttackAccepted));
                PatchSword(typeof(Humanoid), "UpdateBlock", new[] { typeof(float) }, null, nameof(SwordBlockProcessed));
                PatchSword(typeof(Attack), "DoMeleeAttack", Type.EmptyTypes, nameof(SwordMeleeBefore), nameof(SwordMeleeAfter));
                PatchSword(typeof(Player), "UseStamina", new[] { typeof(float) }, nameof(SwordStaminaBefore), nameof(SwordStaminaAfter));
                _swordInput.Transition = (stage, reason) =>
                {
                    if (stage == SwordInputSequence.Stage.DualHold) _dualBeganAt = Time.realtimeSinceStartup;
                    if (_verbose.Value || stage == SwordInputSequence.Stage.DualHold || stage == SwordInputSequence.Stage.Releasing)
                        Logger.LogInfo("[SwordInput #" + _swordNumber + "] " + stage + " @" + Time.realtimeSinceStartup.ToString("F3") + ": " + reason);
                };
                _swordReady = true;
                Logger.LogInfo("[SwordInput] Ready: primary -> block -> ONE primary accepted -> immediate dual input. Damage, stamina, animation speed and chain rules are native.");
            }
            catch (Exception ex)
            {
                _swordHarmony?.UnpatchSelf();
                _swordReady = false;
                Logger.LogError("[SwordInput] Disabled: " + ex.Message);
            }
        }
        private static FieldInfo RequiredField(Type type, string name, Type expected)
        {
            var field = AccessTools.Field(type, name);
            if (field == null || field.FieldType != expected) throw new MissingFieldException(type.Name, name);
            return field;
        }
        private void PatchSword(Type type, string name, Type[] args, string prefix, string postfix, string finalizer = null)
        {
            var method = AccessTools.Method(type, name, args);
            if (method == null) throw new MissingMethodException(type.Name, name);
            _swordHarmony.Patch(method,
                prefix: prefix == null ? null : new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), prefix) { priority = Priority.Last },
                postfix: postfix == null ? null : new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), postfix) { priority = Priority.Last },
                finalizer: finalizer == null ? null : new HarmonyMethod(typeof(DaggerPerfectCancelPlugin), finalizer));
        }
        private static bool IsSword(ItemDrop.ItemData weapon) => weapon?.m_shared != null && weapon.m_shared.m_skillType == Skills.SkillType.Swords;
        private static bool IsMelee(Attack attack) => attack != null &&
            (attack.m_attackType == Attack.AttackType.Horizontal || attack.m_attackType == Attack.AttackType.Vertical);
        private bool SwordOwnerValid() => _enabled.Value && _swordEnabled.Value && CanUsePlayer(_swordOwner)
            && ReferenceEquals(_swordOwner.GetCurrentWeapon(), _swordWeapon);
        private bool TryBeginSwordInput(Player player, double now)
        {
            var weapon = player == null ? null : player.GetCurrentWeapon();
            if (!_swordReady || !_swordEnabled.Value || !IsSword(weapon)) return false;
            if (_swordInput.Current != SwordInputSequence.Stage.Idle || !CanUsePlayer(player)
                || player.InAttack() || player.IsBlocking() || ReadBool(_engineBlocking, player)) return true;
            if (!weapon.HaveSecondaryAttack() || !IsMelee(weapon.m_shared.m_attack) || !IsMelee(weapon.m_shared.m_secondaryAttack))
            { Logger.LogWarning("[SwordInput] Unsupported melee sword."); return true; }
            _swordOwner = player; _swordWeapon = weapon;
            _swordAnimator = (Animator)_animatorField.GetValue(player);
            _postBlockAttack = null;
            _postBlockMelee = _dualPrimaryMelee = _dualPrimaryStarts = _dualSecondaryStarts = 0;
            _dualStaminaSpent = 0; _dualBeganAt = 0;
            _reported = false; _swordBeganAt = Time.realtimeSinceStartup;
            _swordInput.StartTimeout = Mathf.Clamp(_startTimeout.Value, .1f, 30f);
            _swordInput.EndTimeout = Mathf.Clamp(_endTimeout.Value, .1f, 30f);
            _swordInput.BlockTimeout = Mathf.Clamp(_blockTimeout.Value, .1f, 30f);
            _swordInput.DualTimeout = Mathf.Clamp(_dualTimeout.Value, .5f, 30f);
            _lastControlTime = Time.realtimeSinceStartup;
            ++_swordNumber;
            ClearSwordQueues();
            WriteSwordControls(default);
            _swordInput.Begin(now);
            Logger.LogInfo("[SwordInput #" + _swordNumber + "] weapon=" + weapon.m_shared.m_name
                + "; nativeSecondaryMultiplier=" + weapon.m_shared.m_secondaryAttack.m_damageMultiplier
                + "; nativeSecondaryStamina=" + weapon.m_shared.m_secondaryAttack.m_attackStamina
                + "; inputOnly=true; postBlockWait=StartAttackAccepted (NOT melee event)");
            return true;
        }
        private void TickSwordInput(bool triggerHeld)
        {
            _swordInput.Rearm(triggerHeld, Time.realtimeSinceStartup);
            if (!_swordInput.Running) return;
            if (!SwordOwnerValid()) { CancelSwordInput("player/UI/focus/weapon interruption"); return; }
            if (Time.realtimeSinceStartup - _lastControlTime > 1f)
            { CancelSwordInput("control updates stopped"); return; }
            _swordInput.Tick(Time.realtimeSinceStartup, _swordOwner.InAttack());
            if (_swordInput.Current == SwordInputSequence.Stage.Releasing) ReleaseSwordInput();
        }
        private bool ApplySwordInputControls(Player player, ref bool attack, ref bool attackHold,
            ref bool secondary, ref bool secondaryHold, ref bool block, ref bool blockHold, bool jump, bool dodge)
        {
            if (!_swordInput.Running || player != _swordOwner) return false;
            if (jump || dodge || !SwordOwnerValid()) CancelSwordInput("jump/dodge/input interruption");
            else _swordInput.Tick(Time.realtimeSinceStartup, player.InAttack());
            var c = _swordInput.Output();
            attack = c.Attack; attackHold = c.AttackHold;
            secondary = c.Secondary; secondaryHold = c.SecondaryHold;
            block = blockHold = c.Block;
            return true;
        }
        private static void SwordConsumerPrefix(Player __instance, out SwordInputSequence.Controls __state)
        {
            __state = default;
            var self = _instance;
            if (self == null || !self._swordReady || !self._swordInput.Running || __instance != self._swordOwner) return;
            try
            {
                if (!self.SwordOwnerValid()) { self.CancelSwordInput("input consumer interruption"); return; }
                self._swordInput.Tick(Time.realtimeSinceStartup, __instance.InAttack());
                if (self._swordInput.Current == SwordInputSequence.Stage.Releasing) { self.ReleaseSwordInput(); return; }
                // Refresh at the actual physics consumer too: multiple physics
                // steps per rendered frame must not repeat a stale single click.
                self.WriteSwordControls(self._swordInput.Output());
                __state = new SwordInputSequence.Controls { Override = true,
                    Attack = ReadBool(self._attackInput, __instance), AttackHold = ReadBool(self._attackHoldInput, __instance),
                    Secondary = ReadBool(self._secondaryInput, __instance), SecondaryHold = ReadBool(self._secondaryHoldInput, __instance),
                    Block = ReadBool(self._blockInput, __instance) };
                self._insideSwordConsumer = true;
            }
            catch (Exception ex) { self.DisableSwordInput(ex); }
            // ALWAYS let vanilla PlayerAttackInput run; no forced StartAttack.
        }
        private static void SwordConsumerPostfix(Player __instance, SwordInputSequence.Controls __state, bool __runOriginal)
        {
            var self = _instance;
            if (self == null || !__state.Override || __instance != self._swordOwner) return;
            self._insideSwordConsumer = false;
            try
            {
                if (!__runOriginal) { self.CancelSwordInput("another patch skipped the native input consumer"); return; }
                self._swordInput.ConsumerCompleted(__state, Time.realtimeSinceStartup);
                // Publish both buttons together immediately after the ONE
                // accepted post-block click's consumer returns. The NEXT native
                // consumer receives both, without waiting for a hit or Update.
                self.WriteSwordControls(self._swordInput.Output());
            }
            catch (Exception ex) { self.DisableSwordInput(ex); }
        }
        private static Exception SwordConsumerFinalizer(Player __instance, Exception __exception)
        {
            var self = _instance;
            if (self != null && __instance == self._swordOwner)
            {
                self._insideSwordConsumer = false;
                if (__exception != null) self.DisableSwordInput(__exception);
            }
            return __exception; // Preserve unrelated game exceptions.
        }
        private static void SwordAttackAccepted(Humanoid __instance, bool secondaryAttack, bool __result, bool __runOriginal)
        {
            var self = _instance;
            if (self == null || !self._insideSwordConsumer || __instance != self._swordOwner || !__result || !__runOriginal) return;
            try
            {
                var before = self._swordInput.Current;
                if (before == SwordInputSequence.Stage.PostBlockPress && !secondaryAttack)
                    self._postBlockAttack = (Attack)self._currentAttack.GetValue(__instance);
                if (before == SwordInputSequence.Stage.DualHold)
                { if (secondaryAttack) ++self._dualSecondaryStarts; else ++self._dualPrimaryStarts; }
                self._swordInput.AttackAccepted(secondaryAttack, Time.realtimeSinceStartup);
            }
            catch (Exception ex) { self.DisableSwordInput(ex); }
        }
        private static void SwordBlockProcessed(Humanoid __instance, bool __runOriginal)
        {
            var self = _instance;
            if (self == null || !self._swordReady || !self._swordInput.Running || __instance != self._swordOwner || !__runOriginal) return;
            try { self._swordInput.BlockProcessed(ReadBool(self._blockInput, __instance), __instance.IsBlocking(),
                ReadBool(self._engineBlocking, __instance), Time.realtimeSinceStartup); }
            catch (Exception ex) { self.DisableSwordInput(ex); }
        }
        private struct SwordMeleeObservation { internal bool Active, Secondary, PostBlock; internal int Number; }
        private static void SwordMeleeBefore(Attack __instance, out SwordMeleeObservation __state)
        {
            __state = default;
            var self = _instance;
            if (self == null || !self._swordReady || !self._swordInput.Running || self._swordOwner == null) return;
            try
            {
                if (!ReferenceEquals(self._currentAttack.GetValue(self._swordOwner), __instance)) return;
                __state = new SwordMeleeObservation { Active = true, Number = self._swordNumber,
                    Secondary = ReadBool(self._currentSecondary, self._swordOwner), PostBlock = ReferenceEquals(self._postBlockAttack, __instance) };
            }
            catch (Exception ex) { self.DisableSwordInput(ex); }
            // No event suppression, deduplication, damage writes or synthetic hit.
        }
        private static void SwordMeleeAfter(Attack __instance, SwordMeleeObservation __state, bool __runOriginal)
        {
            var self = _instance;
            if (!__runOriginal || !__state.Active || self == null || !self._swordInput.Running || __state.Number != self._swordNumber) return;
            try
            {
                if (!self.SwordOwnerValid()) { self.CancelSwordInput("interrupted during melee"); return; }
                if (__state.PostBlock && !__state.Secondary) ++self._postBlockMelee;
                else if (self._swordInput.Current == SwordInputSequence.Stage.DualHold && !__state.Secondary) ++self._dualPrimaryMelee;
                bool counted = __state.Secondary && self._swordInput.SecondaryMelee(Time.realtimeSinceStartup);
                if (counted || self._verbose.Value)
                    self.Logger.LogInfo("[SwordInput #" + self._swordNumber + "] nativeMelee: secondary=" + __state.Secondary
                        + "; secondaryEvents=" + self._swordInput.SecondaryEvents + "/3; multiplier=" + __instance.m_damageMultiplier
                        + "; profileStamina=" + __instance.m_attackStamina + "; attackAnimation=" + __instance.m_attackAnimation
                        + "; clips=" + self.DescribeSwordClips());
                if (self._swordInput.Current == SwordInputSequence.Stage.Releasing) self.ReleaseSwordInput();
            }
            catch (Exception ex) { self.DisableSwordInput(ex); }
        }
        private string DescribeSwordClips()
        {
            if (_swordAnimator == null) return "unavailable";
            var names = new List<string>();
            for (int layer = 0; layer < _swordAnimator.layerCount; ++layer)
            {
                if (layer > 0 && _swordAnimator.GetLayerWeight(layer) < .01f) continue;
                _swordClips.Clear(); _swordAnimator.GetCurrentAnimatorClipInfo(layer, _swordClips);
                foreach (var c in _swordClips) if (c.weight > .05f && c.clip != null) names.Add(c.clip.name);
            }
            return string.Join(",", names);
        }
        private static void SwordStaminaBefore(Player __instance, out float __state)
        {
            var self = _instance;
            __state = self != null && __instance == self._swordOwner && self._swordInput.Current == SwordInputSequence.Stage.DualHold
                ? __instance.GetStamina() : float.NaN;
        }
        private static void SwordStaminaAfter(Player __instance, float __state)
        {
            var self = _instance;
            if (!float.IsNaN(__state) && self != null && __instance == self._swordOwner)
                self._dualStaminaSpent += Mathf.Max(0, __state - __instance.GetStamina());
        }
        private void WriteSwordControls(SwordInputSequence.Controls c)
        {
            if (_swordOwner == null || _swordOwner != Player.m_localPlayer) return;
            bool old = _releasing;
            try
            {
                _releasing = true;
                bool focused = Application.isFocused && TakesInput(_swordOwner);
                _swordOwner.SetControls(focused ? _rawMove : Vector3.zero, c.Attack, c.AttackHold, c.Secondary, c.SecondaryHold,
                    c.Block, c.Block, false, false, focused && _rawRun, false, false);
            }
            finally { _releasing = old; }
        }
        private void ClearSwordQueues()
        {
            // Only INPUT queues, at acquisition/release. Never alter attack,
            // stamina, animation, chain, blocking or damage state fields.
            if (_swordOwner == null || _swordOwner != Player.m_localPlayer) return;
            _queuedPrimary.SetValue(_swordOwner, 0f); _queuedSecondary.SetValue(_swordOwner, 0f);
        }
        private void CancelSwordInput(string reason)
        {
            if (!_swordInput.Running) return;
            _swordInput.Cancel(Time.realtimeSinceStartup, reason);
            ReleaseSwordInput();
        }
        private void DisableSwordInput(Exception ex)
        {
            if (_swordFaulted) return;
            _swordFaulted = true; _swordReady = false; _insideSwordConsumer = false;
            try { CancelSwordInput("runtime fault; disabled until restart"); }
            catch { }
            finally { ClearSwordInputReferences(); }
            Logger.LogError("[SwordInput] Disabled once for this session: " + ex.GetType().Name + ": " + ex.Message + ". " + ex.StackTrace);
        }
        private void ReleaseSwordInput()
        {
            try { try { ClearSwordQueues(); } finally { WriteSwordControls(default); } }
            finally
            {
                _swordInput.Neutralized(Time.realtimeSinceStartup);
                try
                {
                    if (!_reported)
                    {
                        _reported = true;
                        Logger.LogInfo("[SwordInput #" + _swordNumber + "] result=" + _swordInput.Result
                            + "; openingStarts=" + _swordInput.OpeningStarts + "; postBlockPrimaryStarts=" + _swordInput.PostBlockStarts
                            + "; postBlockPrimaryMelee=" + _postBlockMelee + "; primaryMeleeDuringDual=" + _dualPrimaryMelee
                            + "; dualPrimaryStarts=" + _dualPrimaryStarts + "; dualSecondaryStarts=" + _dualSecondaryStarts
                            + "; secondaryEvents=" + _swordInput.SecondaryEvents + "/3; staminaSpentDuringDual=" + _dualStaminaSpent.ToString("F3")
                            + "; duration=" + (Time.realtimeSinceStartup - _swordBeganAt).ToString("F3")
                            + "; dualDuration=" + (_dualBeganAt > 0 ? Time.realtimeSinceStartup - _dualBeganAt : 0).ToString("F3")
                            + ". Native observations, not a guarantee of the power-combo animation or enemy HP loss. Stamina includes other actions.");
                    }
                }
                finally { ClearSwordInputReferences(); }
            }
        }
        private void ClearSwordInputReferences()
        {
            _swordOwner = null; _swordWeapon = null; _postBlockAttack = null; _swordAnimator = null;
            _insideSwordConsumer = false; _swordClips.Clear();
        }
    }
}
