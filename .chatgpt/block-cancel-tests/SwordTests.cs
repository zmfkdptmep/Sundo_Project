using Goni.DaggerPerfectCancel;
using Legacy = Goni.LegacyBlockCancel31.BlockCancelSequence;
using Stage = Goni.DaggerPerfectCancel.BlockCancelSequence.Stage;

static class SwordTests
{
    static int _checks;
    static void Check(bool condition, string message)
    {
        ++_checks;
        if (!condition) throw new Exception("Sword: " + message);
    }
    static void Neutral(BlockCancelSequence.Controls c, string message) =>
        Check(c.Override && !c.Attack && !c.AttackHold && !c.SecondaryAttack
            && !c.SecondaryAttackHold && !c.Block && !c.BlockHold, message);

    static BlockCancelSequence SecondClick(double firstEnd = .4)
    {
        var s = new BlockCancelSequence();
        s.Begin(0, true);
        Check(!s.PrimaryAttackAccepted(.001), "first attack must not arm the post-block observer");
        s.SecondPrimaryMeleeObserved();
        Check(!s.SecondaryMeleeObserved(.002), "early secondary callback ignored");
        var c = s.Step(.01, false);
        Check(c.Attack && !c.SecondaryAttackHold, "ordinary first primary");
        s.AttackInputProcessed(true, true, false, false, .02);
        s.Step(.03, true);
        c = s.Step(firstEnd, false);
        Check(c.BlockHold && !c.AttackHold && !c.SecondaryAttackHold, "original block boundary");
        s.BlockProcessed(true, true);
        c = s.Step(firstEnd + .02, false);
        Check(c.Attack && c.AttackHold && !c.BlockHold && !c.SecondaryAttackHold, "one post-block primary click");
        return s;
    }

    static BlockCancelSequence Dual(double secondHit = .8)
    {
        var s = SecondClick();
        Check(s.PrimaryAttackAccepted(.43), "observe vanilla successful StartAttack before consumer returns");
        s.AttackInputProcessed(true, true, false, true, .44);
        Neutral(s.Step(.45, true), "post-block click released; no primary auto-repeat while awaiting hit");
        Check(!s.SecondaryMeleeObserved(.46), "no follow-up event counted before dual controls");
        s.SecondPrimaryMeleeObserved();
        var c = s.Step(secondHit, true);
        Check(c.Attack && c.AttackHold && c.SecondaryAttack && c.SecondaryAttackHold && !c.BlockHold,
            "primary and secondary pressed together only after the second melee event");
        return s;
    }

    static void EventTimingSweep()
    {
        for (int i = 0; i < 30; ++i)
        {
            var firstEnd = .2 + i * .031;
            var s = SecondClick(firstEnd);
            var accepted = firstEnd + .03;
            Check(s.PrimaryAttackAccepted(accepted), "second primary accepted");
            s.AttackInputProcessed(true, true, false, true, accepted + .01);
            var secondHit = accepted + .10 + i * .073;
            for (var t = accepted + .02; t < secondHit; t += .02)
                Neutral(s.Step(t, true), "hit-stop must not trigger the follow-up or repeat the primary");
            s.SecondPrimaryMeleeObserved();
            var c = s.Step(secondHit, true);
            Check(c.AttackHold && c.SecondaryAttackHold, "follow-up tracks melee event instead of milliseconds");
            for (int k = 0; k < 4; ++k)
            {
                c = s.Step(secondHit + k * .001, true);
                Check(c.Attack && c.SecondaryAttack, "both click edges survive until consumer acknowledgement");
            }
            s.AttackInputProcessed(true, true, false, true, secondHit + .005, true, true);
            for (int eventNumber = 1; eventNumber <= 3; ++eventNumber)
            {
                var eventTime = secondHit + eventNumber * (.10 + i * .02);
                c = s.Step(eventTime, eventNumber % 2 == 0);
                Check(c.AttackHold && c.SecondaryAttackHold && !c.Attack && !c.SecondaryAttack,
                    "two held inputs without repeated click edges");
                Check(s.SecondaryMeleeObserved(eventTime), "count one secondary melee event, not individual targets");
                Check(s.SecondarySwingCount == eventNumber, "exact secondary event count");
                if (eventNumber < 3) Check(s.Current == Stage.SwordDualHold, "do not stop before three events");
            }
            Check(s.Current == Stage.Releasing, "release as soon as third event returns");
            Check(!s.SecondaryMeleeObserved(secondHit + 3), "fourth or stale event cannot restart/extend automation");
            Neutral(s.Step(secondHit + 3.01, true), "both inputs released");
            s.AttackInputProcessed(false, false, false, true, secondHit + 3.02, false, true);
            Neutral(s.Step(secondHit + 3.03, true), "secondary hold must also be neutral before relinquishing input");
            s.AttackInputProcessed(false, false, false, true, secondHit + 3.04, false, false);
            Check(!s.Step(secondHit + 3.05, true).Override, "restore physical controls after full neutral acknowledgement");
            s.Rearm(true, secondHit + 3.06);
            Check(!s.Begin(secondHit + 3.07, true), "holding trigger cannot loop sword combo");
            s.Rearm(false, secondHit + 3.08);
            Check(s.Begin(secondHit + 3.09, false) && !s.SwordMode && s.SecondarySwingCount == 0,
                "sword state does not leak into a later non-sword activation");
        }
    }

    static void FailureAndCancellation()
    {
        var noStart = SecondClick();
        noStart.AttackInputProcessed(true, true, false, false, .43);
        Neutral(noStart.Step(2, false), "rejected second primary aborts instead of forcing a sword attack");
        var noHit = SecondClick();
        noHit.PrimaryAttackAccepted(.43);
        noHit.AttackInputProcessed(true, true, false, true, .44);
        Neutral(noHit.Step(6.5, true), "missing second melee event watchdog");
        var noAck = SecondClick();
        noAck.PrimaryAttackAccepted(.43);
        noAck.SecondPrimaryMeleeObserved();
        Neutral(noAck.Step(6.5, true), "missing input acknowledgement cannot leave an endless primary press");
        var noThird = Dual();
        noThird.SecondaryMeleeObserved(.9);
        noThird.SecondaryMeleeObserved(1);
        Neutral(noThird.Step(7, true), "two events do not become a fabricated third on timeout");
        Check(noThird.SecondarySwingCount == 2, "watchdog preserves actual observed count");

        var partial = Dual();
        partial.AttackInputProcessed(false, true, false, true, .81, true, true);
        var c = partial.Step(.82, true);
        Check(c.Attack && !c.SecondaryAttack && c.AttackHold && c.SecondaryAttackHold,
            "each press edge requires its own consumer acknowledgement");

        foreach (var s in new[] { SecondClick(), Dual() })
        {
            s.Cancel(.9, "focus loss / F12 / weapon change");
            Neutral(s.Step(.91, true), "cancel releases primary and secondary");
            Check(!s.PrimaryAttackAccepted(.92), "late start cannot re-arm cancelled state");
            s.SecondPrimaryMeleeObserved();
            Check(!s.SecondaryMeleeObserved(.93), "late melee cannot restart cancelled state");
        }
        var gone = Dual();
        gone.OwnerGone(.9);
        Check(!gone.Step(.91, true).Override, "never inject sword inputs into replacement owner");
    }

    static void LegacyDifferential()
    {
        // Replay identical control/observation traces against the confirmed 3.1
        // source. Non-sword behavior must match, not just approximate its timings.
        var rng = new Random(320);
        for (int trace = 0; trace < 64; ++trace)
        {
            var old = new Legacy();
            var current = new BlockCancelSequence();
            for (int tick = 0; tick < 100; ++tick)
            {
                double now = tick * .08;
                bool down = rng.Next(3) != 0;
                old.Rearm(down, now); current.Rearm(down, now);
                if (rng.Next(6) == 0) Check(old.Begin(now) == current.Begin(now), "legacy begin/rearm compatibility");
                bool inAttack = rng.Next(3) != 0;
                var a = old.Step(now, inAttack);
                var b = current.Step(now, inAttack);
                Check(a.Override == b.Override && a.Attack == b.Attack && a.AttackHold == b.AttackHold
                    && a.Block == b.Block && a.BlockHold == b.BlockHold
                    && !b.SecondaryAttack && !b.SecondaryAttackHold
                    && old.Current.ToString() == current.Current.ToString(), "non-sword output differs from 3.1");
                bool attack = rng.Next(2) == 0, hold = rng.Next(2) == 0, block = rng.Next(2) == 0;
                old.AttackInputProcessed(attack, hold, block, inAttack, now);
                current.AttackInputProcessed(attack, hold, block, inAttack, now);
                bool blocking = rng.Next(2) == 0, applied = rng.Next(2) == 0;
                old.BlockProcessed(blocking, applied); current.BlockProcessed(blocking, applied);
                current.PrimaryAttackAccepted(now); current.SecondPrimaryMeleeObserved(); current.SecondaryMeleeObserved(now);
                if (rng.Next(40) == 0) { old.Cancel(now, "cancel"); current.Cancel(now, "cancel"); }
            }
        }
    }

    internal static void Run()
    {
        EventTimingSweep();
        FailureAndCancellation();
        LegacyDifferential();
        Console.WriteLine($"PASS: {_checks} sword/compatibility assertions; 30 event-timing scenarios and 64 traces against frozen 3.1. No game damage/stamina result is inferred.");
    }
}
