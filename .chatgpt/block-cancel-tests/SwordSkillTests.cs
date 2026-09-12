using Goni.DaggerPerfectCancel;
using SkillStage = Goni.DaggerPerfectCancel.SwordSkillSequence.Stage;

static class SwordSkillTests
{
    static int checks;
    static void Check(bool condition, string reason)
    {
        ++checks;
        if (!condition) throw new Exception("Sword skill: " + reason);
    }
    static SwordSkillSequence AtBlock()
    {
        var s = new SwordSkillSequence();
        Check(s.Begin(0), "begin");
        Check(s.WantsStart, "one opening requested");
        s.Accepted(.02);
        s.Tick(.04, false, false, false, false, 2);
        Check(s.Current == SkillStage.OpeningSwing, "asynchronous animation startup cannot skip opening");
        s.Tick(.1, true, false, false, false, 3);
        Check(s.MeleeEvent(), "opening actual event");
        Check(!s.MeleeEvent(), "duplicate opening event rejected");
        s.Tick(.2, true, true, false, false, 4);
        Check(!s.WantsBlock, "opening must recover before visible guard");
        s.Tick(.6, false, false, false, false, 5);
        Check(s.WantsBlock, "guard follows exactly one opening");
        return s;
    }
    static SwordSkillSequence AtSlashes()
    {
        var s = AtBlock();
        s.Tick(.7, false, false, true, false, 6);
        s.Tick(1.1, false, false, true, false, 7);
        Check(s.WantsBlock, "blocking flag without animation cannot start slashes");
        s.Tick(1.2, false, false, true, true, 8);
        s.Tick(1.5, false, false, true, true, 8);
        Check(s.WantsBlock, "same rendered frame cannot acknowledge visible guard");
        s.Tick(1.51, false, false, true, true, 9);
        Check(s.Current == SkillStage.ReleaseBlock, "visible guard held long enough");
        s.Tick(1.52, false, false, true, false, 10);
        Check(!s.WantsStart, "wait until actual guard lowered");
        s.Tick(1.54, false, false, false, false, 11);
        Check(s.WantsStart && s.IsSlash && s.SlashesStarted == 0, "no extra primary after guard");
        return s;
    }
    static void Complete(double step, double swing, bool chain)
    {
        var s = AtSlashes();
        double now = 1.55;
        int frame = 12, accepted = 1;
        for (int slash = 1; slash <= 3; ++slash)
        {
            Check(s.WantsStart && s.IsSlash, "only slashes requested after block");
            s.Accepted(now); ++accepted;
            s.Accepted(now); // duplicate adapter notification cannot create another slash
            Check(s.SlashesStarted == slash, "accepted identity once");
            s.Tick(now, false, false, false, false, frame++);
            for (double elapsed = step; elapsed < swing; elapsed += step)
            {
                s.Tick(now + elapsed, true, true, false, false, frame++);
                Check(!s.WantsStart, "chain flag cannot start another attack before melee event");
            }
            now += swing;
            Check(s.MeleeEvent(), "count miss/hit/multiple targets once per actual swing");
            Check(!s.MeleeEvent(), "duplicate slash event rejected");
            s.Tick(now, true, false, false, false, frame++);
            Check(!s.WantsStart, "hit-stop/recovery without chain permission cannot advance");
            now += .07;
            s.Tick(now, chain, chain, false, false, frame++);
            if (slash < 3) Check(s.WantsStart, "next slash at chain window or recovery");
            else
            {
                Check(!s.WantsStart, "never request fourth slash, even with chain permission");
                s.Tick(now + .1, false, false, false, false, frame++);
            }
            now += .15;
        }
        Check(accepted == 4 && s.OpeningEvents == 1 && s.SlashesCompleted == 3, "exactly one opening plus three slashes");
        Check(!s.Running && s.Result.StartsWith("complete"), "wait for final animation recovery then finish");
        s.Rearm(true, now);
        Check(!s.Begin(now), "held Mouse5 never loops");
        s.Rearm(false, now + .01);
        Check(s.Begin(now + .02), "new press rearmed after release");
    }
    internal static void Run()
    {
        foreach (double step in new[] { 1d/20, 1d/60, 1d/144 })
            foreach (double swing in new[] { .18, .5, 1.4 })
                foreach (bool chain in new[] { true, false }) Complete(step, swing, chain);
        var noPose = AtBlock();
        noPose.Tick(9, false, false, true, false, 999);
        Check(noPose.Result.StartsWith("ABORT") && noPose.SlashesStarted == 0, "missing block animation aborts");
        var noEvent = AtSlashes(); noEvent.Accepted(1.6);
        noEvent.Tick(1.7, true, false, false, false, 20);
        noEvent.Tick(1.9, false, false, false, false, 21);
        Check(noEvent.Result.StartsWith("ABORT") && noEvent.SlashesStarted == 1, "interrupted animation never silently skips a slash");
        foreach (bool duringSlash in new[] { false, true })
        {
            var s = duringSlash ? AtSlashes() : AtBlock();
            if (duringSlash) s.Accepted(1.6);
            s.Cancel(1.7, "focus/death/weapon/F12 interruption");
            Check(!s.Running && !s.WantsStart && !s.WantsBlock, "cancel releases attack and guard ownership");
            Check(!s.MeleeEvent(), "late callback cannot resurrect cancelled sequence");
        }
        var guard = new SwordGuardPolicy();
        Check(!guard.Step(0, true, false), "no threat no auto block");
        Check(guard.Step(.1, true, true), "threat raises guard");
        Check(guard.Step(.15, true, false), "guard motion kept visible despite brief threat");
        Check(!guard.Step(.4, true, false), "guard does not stick");
        Check(guard.Step(.5, true, true), "can guard again");
        Check(!guard.Step(.51, false, true), "manual attack/dodge/UI takes priority");
        Check(SwordGuardPolicy.IsThreat(3, 0, 1, 1, 6, true), "frontal incoming melee");
        Check(!SwordGuardPolicy.IsThreat(3, 0, -1, 1, 6, true), "no auto block behind player");
        Check(!SwordGuardPolicy.IsThreat(3, 0, 1, -1, 6, true), "enemy attacking away ignored");
        Check(!SwordGuardPolicy.IsThreat(10, 0, 1, 1, 6, true), "distant attacker ignored");
        Check(!SwordGuardPolicy.IsThreat(3, 4, 1, 1, 6, true), "different floor ignored");
        Check(!SwordGuardPolicy.IsThreat(3, 0, 1, 1, 6, false), "ranged startup is not melee threat");
        Console.WriteLine($"PASS: {checks} sword skill / visible guard assertions. Gameplay animation appearance, network behavior and mod compatibility still require Valheim.");
    }
}
