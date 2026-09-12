using System;

namespace Goni.DaggerPerfectCancel
{
    // Input scheduling only. Acceptance is a successful vanilla StartAttack,
    // NOT a melee event. In particular, never wait for the post-block hit.
    internal sealed class SwordInputSequence
    {
        internal enum Stage { Idle, OpeningPress, OpeningSwing, Block, PostBlockPress, PostBlockAccepted, DualHold, Releasing, WaitRelease }
        internal struct Controls
        {
            internal bool Override, Attack, AttackHold, Secondary, SecondaryHold, Block;
            internal bool Neutral => !Attack && !AttackHold && !Secondary && !SecondaryHold && !Block;
        }
        internal Stage Current { get; private set; }
        internal bool Running => Current != Stage.Idle && Current != Stage.WaitRelease;
        internal double StartTimeout = 1.5, EndTimeout = 6, BlockTimeout = 1.5, DualTimeout = 8;
        internal int OpeningStarts { get; private set; }
        internal int PostBlockStarts { get; private set; }
        internal int SecondaryEvents { get; private set; }
        internal string Result { get; private set; }
        internal Action<Stage, string> Transition;
        private double _since;
        private bool _openingConsumed, _postConsumed, _dualConsumed, _openingSeen;

        internal bool Begin(double now)
        {
            if (Current != Stage.Idle) return false;
            _openingConsumed = _postConsumed = _dualConsumed = _openingSeen = false;
            OpeningStarts = PostBlockStarts = SecondaryEvents = 0;
            Result = null;
            Move(Stage.OpeningPress, now, "one primary click");
            return true;
        }
        internal void Rearm(bool down, double now)
        {
            if (!down && Current == Stage.WaitRelease) Move(Stage.Idle, now, "Mouse5 released");
        }
        internal void Tick(double now, bool inAttack)
        {
            if (Current == Stage.OpeningSwing)
            {
                if (inAttack) _openingSeen = true;
                else if (_openingSeen) Move(Stage.Block, now, "opening InAttack ended; request normal block");
            }
            var age = now - _since;
            if ((Current == Stage.OpeningPress || Current == Stage.PostBlockPress || Current == Stage.PostBlockAccepted) && age > StartTimeout)
                Cancel(now, "attack/input consumer did not acknowledge the click");
            else if (Current == Stage.OpeningSwing && age > EndTimeout)
                Cancel(now, "opening animation watchdog");
            else if (Current == Stage.Block && age > BlockTimeout)
                Cancel(now, "engine did not acknowledge blocking");
            else if (Current == Stage.DualHold && age > DualTimeout)
                Cancel(now, "dual-input watchdog; secondary events=" + SecondaryEvents + "/3");
        }
        internal Controls Output()
        {
            var c = new Controls { Override = Running };
            if (Current == Stage.OpeningPress) c.Attack = c.AttackHold = !_openingConsumed;
            else if (Current == Stage.Block) c.Block = true;
            else if (Current == Stage.PostBlockPress) c.Attack = c.AttackHold = !_postConsumed;
            else if (Current == Stage.DualHold)
            {
                c.Attack = c.Secondary = !_dualConsumed;
                c.AttackHold = c.SecondaryHold = true;
            }
            return c;
        }
        internal void AttackAccepted(bool secondary, double now)
        {
            if (secondary) return;
            if (Current == Stage.OpeningPress)
            {
                ++OpeningStarts;
                Move(Stage.OpeningSwing, now, "vanilla accepted opening primary");
            }
            else if (Current == Stage.PostBlockPress)
            {
                ++PostBlockStarts;
                Move(Stage.PostBlockAccepted, now, "vanilla accepted ONE post-block primary; no melee wait");
            }
        }
        internal void ConsumerCompleted(Controls delivered, double now)
        {
            if (!delivered.Override) return;
            if (delivered.Attack && !delivered.Secondary)
            {
                _openingConsumed = true;
                if (Current == Stage.PostBlockPress || Current == Stage.PostBlockAccepted) _postConsumed = true;
            }
            if (Current == Stage.PostBlockAccepted)
                Move(Stage.DualHold, now, "primary consumer returned; press primary+secondary TOGETHER now");
            else if (Current == Stage.DualHold && delivered.Attack && delivered.Secondary
                && delivered.AttackHold && delivered.SecondaryHold) _dualConsumed = true;
            else if (Current == Stage.Releasing && delivered.Neutral) Neutralized(now);
        }
        internal void BlockProcessed(bool blockInput, bool isBlocking, bool engineBlocking, double now)
        {
            if (Current == Stage.Block && blockInput && isBlocking && engineBlocking)
                Move(Stage.PostBlockPress, now, "normal UpdateBlock applied; release block + ONE primary click");
        }
        internal bool SecondaryMelee(double now)
        {
            if (Current != Stage.DualHold || !_dualConsumed) return false;
            // Native power combos may dispatch multiple events through the SAME
            // Attack object. Do not deduplicate by Attack or alter those events.
            if (++SecondaryEvents == 3)
            {
                Result = "three native secondary-profile melee events observed";
                Move(Stage.Releasing, now, Result);
            }
            return true;
        }
        internal void Cancel(double now, string reason)
        {
            if (!Running || Current == Stage.Releasing) return;
            Result = "ABORT: " + reason;
            Move(Stage.Releasing, now, Result);
        }
        internal void Neutralized(double now)
        {
            if (Current == Stage.Releasing) Move(Stage.WaitRelease, now, "neutral controls applied");
        }
        private void Move(Stage next, double now, string reason)
        {
            Current = next; _since = now; Transition?.Invoke(next, reason);
        }
    }
}
