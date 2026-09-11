using System;

namespace Goni.DaggerPerfectCancel
{
    // Pure input state machine: no game objects, attack creation or game-state writes.
    internal sealed class BlockCancelSequence
    {
        internal enum Stage { Idle, FirstPress, WaitFirstStart, WaitFirstEnd, WaitBlockApplied, SecondHold, Releasing, WaitRelease }
        internal struct Controls
        {
            internal bool Override, Attack, AttackHold, Block, BlockHold;
        }
        internal Stage Current { get; private set; }
        internal bool Running => Current != Stage.Idle && Current != Stage.WaitRelease;
        internal double HoldSeconds = 1.2, StartTimeout = 1.5, EndTimeout = 6.0, BlockTimeout = 1.5;
        internal Action<Stage, string> Transition;
        private double _since;
        private bool _blockApplied, _secondPressConsumed, _releaseConsumed;
        private Controls _last;

        internal bool Begin(double now)
        {
            if (Current != Stage.Idle) return false;
            _blockApplied = _secondPressConsumed = _releaseConsumed = false;
            _last = default;
            Move(Stage.FirstPress, now, "Mouse5: sending one primary click");
            return true;
        }
        internal void Rearm(bool triggerDown, double now)
        {
            if (Current == Stage.WaitRelease && !triggerDown)
                Move(Stage.Idle, now, "trigger released; re-armed");
        }
        internal Controls Step(double now, bool inAttack)
        {
            ObserveAttack(inAttack, now);
            var age = now - _since;
            if ((Current == Stage.FirstPress || Current == Stage.WaitFirstStart) && age > StartTimeout)
                Cancel(now, "first attack was not accepted");
            else if (Current == Stage.WaitFirstEnd && age > EndTimeout)
                Cancel(now, "first attack did not finish before watchdog timeout");
            else if (Current == Stage.WaitBlockApplied && age > BlockTimeout)
                Cancel(now, "game did not apply blocking");
            if (Current == Stage.WaitBlockApplied && _blockApplied)
                Move(Stage.SecondHold, now, "engine UpdateBlock applied blocking; release RMB + hold LMB");
            if (Current == Stage.SecondHold && now - _since >= HoldSeconds)
                Move(Stage.Releasing, now, "primary hold elapsed; releasing inputs (combo result not inferred)");
            if (Current == Stage.Releasing && _releaseConsumed)
                Move(Stage.WaitRelease, now, "neutral controls consumed");

            var controls = new Controls { Override = Running };
            if (Current == Stage.FirstPress) { controls.Attack = true; controls.AttackHold = true; }
            else if (Current == Stage.WaitBlockApplied) { controls.Block = true; controls.BlockHold = true; }
            else if (Current == Stage.SecondHold) { controls.Attack = !_secondPressConsumed; controls.AttackHold = true; }
            return _last = controls;
        }
        // Acknowledge only AFTER vanilla PlayerAttackInput processed the emitted input.
        // Repeated SetControls calls cannot erase a click before its consumer sees it.
        internal void AttackInputProcessed(bool attackInput, bool attackHoldInput, bool blockInput, bool inAttack, double now)
        {
            if (Current == Stage.FirstPress && _last.Attack && attackInput)
                Move(Stage.WaitFirstStart, now, "vanilla consumed first primary input");
            else if (Current == Stage.SecondHold && _last.Attack && attackInput)
                _secondPressConsumed = true;
            else if (Current == Stage.Releasing && _last.Override && !attackInput && !attackHoldInput && !blockInput)
                _releaseConsumed = true;
            ObserveAttack(inAttack, now);
        }
        internal void ObserveAttack(bool inAttack, double now)
        {
            if (Current == Stage.WaitFirstStart && inAttack)
                Move(Stage.WaitFirstEnd, now, "InAttack became true");
            else if (Current == Stage.WaitFirstEnd && !inAttack)
                Move(Stage.WaitBlockApplied, now, "InAttack became false; block requested");
        }
        internal void BlockProcessed(bool isBlocking, bool internalBlockingState)
        {
            // IsBlocking is a predicate on held input. Also require the engine-owned
            // flag set by a real UpdateBlock call, following our emitted block input.
            if (Current == Stage.WaitBlockApplied && _last.BlockHold && isBlocking && internalBlockingState)
                _blockApplied = true;
        }
        internal void Cancel(double now, string reason)
        {
            if (!Running || Current == Stage.Releasing) return;
            _releaseConsumed = false;
            Move(Stage.Releasing, now, "ABORT: " + reason);
        }
        internal void OwnerGone(double now)
        {
            // There is no longer a local entity whose controls we own. Do not wait
            // for a callback from a destroyed player or inject into a new player.
            if (Running) Move(Stage.WaitRelease, now, "owner gone; awaiting trigger release");
        }
        private void Move(Stage next, double now, string reason)
        {
            Current = next;
            _since = now;
            Transition?.Invoke(next, reason);
        }
    }
}
