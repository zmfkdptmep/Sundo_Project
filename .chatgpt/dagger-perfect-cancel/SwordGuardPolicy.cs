namespace Goni.DaggerPerfectCancel
{
    internal sealed class SwordGuardPolicy
    {
        private double _raisedAt;
        private bool _raised;
        internal bool Step(double now, bool eligible, bool threat)
        {
            if (!eligible) { Reset(); return false; }
            if (threat && !_raised) { _raised = true; _raisedAt = now; }
            if (_raised && !threat && now - _raisedAt >= .20) _raised = false;
            return _raised;
        }
        internal void Reset() { _raised = false; _raisedAt = 0; }
        internal static bool IsThreat(float distance, float height, float frontDot, float attackerDot, float range, bool melee)
            => melee && distance <= range && height <= 3f && frontDot >= .15f && attackerDot >= .15f;
    }
}
