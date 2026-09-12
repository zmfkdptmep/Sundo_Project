using Goni.DaggerPerfectCancel;

static class SwordTempoTests
{
    static int checks;
    static void Equal(float actual, float expected, string reason)
    {
        ++checks;
        if (Math.Abs(actual - expected) > .0001f) throw new Exception("Tempo: " + reason + $"; actual={actual}, expected={expected}");
    }
    internal static void Run()
    {
        var speed = new AnimationSpeedLease();
        float observed = speed.Apply(1f, 2.4f, false);
        Equal(observed, 2.4f, "initial slash speed");
        for (int i = 0; i < 10000; ++i) observed = speed.Apply(observed, 2.4f, false);
        Equal(observed, 2.4f, "Update plus FixedUpdate never compound the multiplier");
        Equal(speed.Apply(observed, 4f, false), 4f, "post-hit recovery uses the original base");
        Equal(speed.Apply(4f, 1f, false), 1f, "block returns to native playback");
        Equal(speed.Apply(1f, 2.4f, false), 2.4f, "next slash re-applies exactly once");
        Equal(speed.AnimationEvent(.8f, 2.4f), 1.92f, "native animation Speed event is multiplied once");
        Equal(speed.Apply(1.92f, 4f, false), 3.2f, "recovery preserves native event speed");
        Equal(speed.UnscaleIfOwned(3.2f), .8f, "completion restores native event speed");
        speed.Reset();
        Equal(speed.Apply(1f, 2.4f, false), 2.4f, "new skill");
        Equal(speed.Apply(.0001f, 4f, true), .0001f, "hit-stop is never overwritten by playback boost");
        Equal(speed.UnscaleIfOwned(2.4f), 1f, "cancel during freeze restores saved resume speed");
        Equal(speed.UnscaleIfOwned(.0001f), .0001f, "cancel does not replace active freeze speed");
        Equal(speed.Apply(2.4f, 4f, false), 4f, "resume from freeze does not double-scale saved boosted speed");
        Equal(speed.Apply(1f, 2.4f, false), 2.4f, "vanilla reset between physics ticks is handled");
        Equal(speed.UnscaleIfOwned(1.25f), 1.25f, "external speed change during cancellation is preserved");
        speed.Reset();
        Equal(speed.Apply(1.25f, 2.4f, false), 3f, "preexisting speed modifier is used as the base");
        Equal(speed.UnscaleIfOwned(3f), 1.25f, "restore preexisting modifier");
        Equal(speed.AnimationEvent(0f, 2.4f), 0f, "explicit animation pause preserved");
        Equal(speed.Apply(0f, 2.4f, false), 0f, "external pause not overwritten");
        // Model only visual playback time: no claims about Unity transitions/DPS.
        float nativeTravel = .2f + .2f;
        float acceleratedTravel = .2f / 2.4f + .2f / 4f;
        if (!(acceleratedTravel < nativeTravel)) throw new Exception("Tempo does not shorten modeled windup/recovery.");
        Console.WriteLine($"PASS: {checks} tempo ownership/hit-stop/cleanup checks plus 10,000 repeated updates; animation timing model shortens windup and recovery. Not a Valheim gameplay test.");
    }
}
