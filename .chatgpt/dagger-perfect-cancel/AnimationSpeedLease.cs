using System;

namespace Goni.DaggerPerfectCancel
{
    // Own only our multiplier. Repeated Update/FixedUpdate calls must never
    // multiply an already boosted speed; preserve native hit-stop and resets.
    internal sealed class AnimationSpeedLease
    {
        internal bool Owns { get; private set; }
        internal float BaseSpeed { get; private set; } = 1f;
        internal float LastApplied { get; private set; } = 1f;
        private static bool Near(float a, float b) => Math.Abs(a - b) <= .0001f * Math.Max(1f, Math.Abs(b));
        private static bool Valid(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > .01f;
        internal float Apply(float observed, float factor, bool frozen)
        {
            if (frozen || !Valid(observed)) return observed;
            if (!Owns || !Near(observed, LastApplied)) BaseSpeed = observed;
            return Scale(factor);
        }
        internal float AnimationEvent(float nativeSpeed, float factor)
        {
            if (!Valid(nativeSpeed)) { Owns = false; return nativeSpeed; }
            BaseSpeed = nativeSpeed;
            return Scale(factor);
        }
        private float Scale(float factor)
        {
            if (!Valid(factor)) factor = 1f;
            LastApplied = BaseSpeed * factor;
            Owns = !Near(factor, 1f);
            return LastApplied;
        }
        internal float UnscaleIfOwned(float value) => Owns && Near(value, LastApplied) ? BaseSpeed : value;
        internal void Reset() { Owns = false; BaseSpeed = LastApplied = 1f; }
    }
}
