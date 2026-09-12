using System;
using BepInEx.Configuration;
using UnityEngine;

namespace Goni.DaggerPerfectCancel
{
    public sealed partial class DaggerPerfectCancelPlugin
    {
        private ConfigEntry<bool> _guardEnabled;
        private ConfigEntry<float> _guardRange, _guardTail;
        private bool _guardApplied, _guardToggleWasDown;
        private Player _guardOwner;
        private float _nextGuardScan, _guardUntil, _guardSuppressedUntil;
        private readonly SwordGuardPolicy _guardPolicy = new SwordGuardPolicy();

        private void SetupSwordGuard()
        {
            _guardEnabled = Config.Bind("SwordGuard", "Enabled", true, "F9 toggles auto guard. Uses real block input and vanilla block/parry animations; no invisible damage immunity.");
            _guardRange = Config.Bind("SwordGuard", "MeleeWatchRange", 6f, "Watch attacking hostile creatures within this distance, in front of the player.");
            _guardTail = Config.Bind("SwordGuard", "HoldAfterThreatSeconds", .25f, "Keep the visible block pose briefly after an observed attack.");
        }
        private void TickGuardHotkey()
        {
            if (!_swordReady) return;
            bool down = ReadKey(0x78); // F9
            bool pressed = down && !_guardToggleWasDown;
            _guardToggleWasDown = down;
            if (pressed && Application.isFocused && Player.m_localPlayer != null && TakesInput(Player.m_localPlayer))
            {
                _guardEnabled.Value = !_guardEnabled.Value;
                StopSwordGuard(true);
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, _guardEnabled.Value ? "검막 ON" : "검막 OFF");
            }
            if (_guardApplied && (!_enabled.Value || !_guardEnabled.Value || !CanUsePlayer(_guardOwner)
                || !CanSwordGuard(_guardOwner))) StopSwordGuard(true);
        }
        private bool CanSwordGuard(Player player)
        {
            if (player == null || !IsSword(player.GetCurrentWeapon()) || player.IsEncumbered()) return false;
            // A shield/torch in the left hand would play a different block. Auto
            // sword guard runs only when vanilla will actually block with the sword.
            return ReferenceEquals(_getCurrentBlocker.Invoke(player, null), player.GetCurrentWeapon());
        }
        private void ApplySwordGuard(Player player, bool attack, bool attackHold, bool secondary, bool secondaryHold,
            bool jump, bool dodge, ref bool block, ref bool blockHold)
        {
            if (!_swordReady) return;
            float now = Time.time;
            bool eligible = _enabled.Value && _guardEnabled.Value && !_skill.Running && !_sequence.Running
                && now >= _guardSuppressedUntil && CanUsePlayer(player) && CanSwordGuard(player)
                && !player.InAttack() && !attack && !attackHold && !secondary && !secondaryHold && !jump && !dodge;
            if (!eligible)
            {
                _guardApplied = false; _guardOwner = null; _guardUntil = 0f;
                _guardPolicy.Reset(); return;
            }
            if (now >= _nextGuardScan)
            {
                _nextGuardScan = now + .05f;
                if (HasMeleeThreat(player))
                    _guardUntil = now + Mathf.Clamp(_guardTail.Value, .10f, .75f);
            }
            bool raised = _guardPolicy.Step(now, true, now < _guardUntil);
            _guardApplied = raised;
            _guardOwner = raised ? player : null;
            if (raised) block = blockHold = true;
            // No IsBlocking/RPC_Damage/BlockAttack patch and no block-timer writes.
            // Humanoid.UpdateBlock raises the networked 'blocking' animation;
            // normal BlockAttack plays the impact/parry motion and spends stamina.
        }
        private bool HasMeleeThreat(Player player)
        {
            float range = Mathf.Clamp(_guardRange.Value, 2f, 12f);
            var position = player.transform.position;
            var forward = player.transform.forward;
            var characters = Character.GetAllCharacters();
            for (int i = 0; i < characters.Count; ++i)
            {
                var enemy = characters[i];
                if (enemy == null || enemy == player || enemy.IsPlayer() || enemy.IsDead()
                    || !enemy.InAttack() || !BaseAI.IsEnemy(player, enemy)) continue;
                var offset = enemy.transform.position - position;
                float height = Mathf.Abs(offset.y);
                offset.y = 0f;
                float distance = offset.magnitude;
                float facingPlayer = distance < .01f ? 1f : Vector3.Dot(forward, offset / distance);
                float enemyFacing = distance < .01f ? 1f : Vector3.Dot(enemy.transform.forward, -offset / distance);
                bool melee = true;
                if (enemy is Humanoid humanoid)
                {
                    var active = (Attack)_currentAttack.GetValue(humanoid);
                    if (active != null) melee = IsMelee(active);
                }
                if (SwordGuardPolicy.IsThreat(distance, height, facingPlayer, enemyFacing, range, melee)) return true;
            }
            return false;
        }
        private void StopSwordGuard(bool release)
        {
            var owner = _guardOwner;
            bool applied = _guardApplied;
            _guardApplied = false; _guardOwner = null; _guardUntil = _nextGuardScan = 0f;
            _guardPolicy.Reset();
            if (release && applied && owner != null && owner == Player.m_localPlayer && !_skill.Running && !_sequence.Running)
                SetCombatNeutral(owner);
        }
        private void EmergencySwordRelease()
        {
            if (!_swordReady) return;
            _guardSuppressedUntil = Time.time + 2f;
            CancelSwordSkill("F12 emergency cancel");
            StopSwordGuard(true);
        }
    }
}
