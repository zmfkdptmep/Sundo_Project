using System;

namespace Goni.DaggerPerfectCancel
{
    // One accepted opening attack, a rendered block, then exactly three attacks.
    // Never advances on elapsed swing delays; animation/hit/chain observations drive it.
    internal sealed class SwordSkillSequence
    {
        internal enum Stage { Idle, OpeningReady, OpeningSwing, Block, ReleaseBlock, SlashReady, SlashSwing, WaitRelease }
        internal Stage Current { get; private set; }
        internal bool Running => Current != Stage.Idle && Current != Stage.WaitRelease;
        internal bool WantsStart => Current == Stage.OpeningReady || Current == Stage.SlashReady;
        internal bool WantsBlock => Current == Stage.Block;
        internal bool IsSlash => Current == Stage.SlashReady || Current == Stage.SlashSwing;
        internal int SlashesStarted { get; private set; }
        internal int SlashesCompleted { get; private set; }
        internal int OpeningEvents { get; private set; }
        internal bool EventSeen { get; private set; }
        internal double Timeout = 8, BlockPoseSeconds = .18;
        internal string Result { get; private set; }
        internal Action<Stage, string> Transition;
        private double _since, _poseSince;
        private int _poseFrame, _lastPoseFrame;
        private bool _sawAttack, _poseSeen;

        internal bool Begin(double now)
        {
            if (Current != Stage.Idle) return false;
            SlashesStarted = SlashesCompleted = OpeningEvents = 0;
            EventSeen = _sawAttack = _poseSeen = false;
            Result = null;
            Move(Stage.OpeningReady, now, "opening primary requested");
            return true;
        }
        internal void Rearm(bool held, double now)
        {
            if (!held && Current == Stage.WaitRelease) Move(Stage.Idle, now, "Mouse5 released");
        }
        internal void Accepted(double now)
        {
            if (!WantsStart) return;
            bool slash = Current == Stage.SlashReady;
            if (slash) ++SlashesStarted;
            EventSeen = _sawAttack = false;
            Move(slash ? Stage.SlashSwing : Stage.OpeningSwing, now, slash ? "slash " + SlashesStarted + "/3 accepted" : "opening accepted");
        }
        internal bool MeleeEvent()
        {
            if ((Current != Stage.OpeningSwing && Current != Stage.SlashSwing) || EventSeen) return false;
            EventSeen = _sawAttack = true;
            if (Current == Stage.OpeningSwing) ++OpeningEvents;
            else ++SlashesCompleted;
            return true;
        }
        internal void Tick(double now, bool inAttack, bool canChain, bool blocking, bool visibleBlock, int frame)
        {
            if (!Running) return;
            if (now - _since > Timeout) { Cancel(now, "watchdog at " + Current); return; }
            if (Current == Stage.OpeningSwing || Current == Stage.SlashSwing)
            {
                _sawAttack |= inAttack;
                if (_sawAttack && !inAttack && !EventSeen) { Cancel(now, "animation ended without melee event"); return; }
                if (!EventSeen) return;
                if (Current == Stage.OpeningSwing)
                {
                    if (!inAttack) Move(Stage.Block, now, "opening finished; raise guard");
                }
                else if (SlashesCompleted == 3)
                {
                    if (!inAttack) Finish(now, "complete: opening=1, slashes=3");
                }
                else if (!inAttack || canChain) Move(Stage.SlashReady, now, "animation chain window/recovery reached");
            }
            else if (Current == Stage.Block)
            {
                if (!blocking || !visibleBlock) { _poseSeen = false; return; }
                if (!_poseSeen) { _poseSeen = true; _poseSince = now; _poseFrame = frame; }
                _lastPoseFrame = frame;
                if (_lastPoseFrame > _poseFrame && now - _poseSince >= BlockPoseSeconds)
                    Move(Stage.ReleaseBlock, now, "block animation observed; lower guard");
            }
            else if (Current == Stage.ReleaseBlock && !blocking)
                Move(Stage.SlashReady, now, "guard released; begin triple slash");
        }
        internal void Cancel(double now, string reason)
        {
            if (Running) Finish(now, "ABORT: " + reason);
        }
        private void Finish(double now, string reason)
        {
            Result = reason;
            Move(Stage.WaitRelease, now, reason);
        }
        private void Move(Stage next, double now, string reason)
        {
            Current = next; _since = now; Transition?.Invoke(next, reason);
        }
    }
}
