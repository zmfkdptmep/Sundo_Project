using Goni.DaggerPerfectCancel;
using Stage = Goni.DaggerPerfectCancel.BlockCancelSequence.Stage;

static class Program
{
    static int _checks;
    static void Check(bool condition, string message)
    {
        ++_checks;
        if (!condition) throw new Exception(message);
    }
    static void Neutral(BlockCancelSequence.Controls c, string message) =>
        Check(c.Override && !c.Attack && !c.AttackHold && !c.Block && !c.BlockHold
            && !c.SecondaryAttack && !c.SecondaryAttackHold, message);

    static BlockCancelSequence Started()
    {
        var s = new BlockCancelSequence();
        Check(s.Begin(0), "begin when idle");
        for (int i = 0; i < 4; ++i)
        {
            var c = s.Step(0.01 * i, false);
            Check(c.Attack && c.AttackHold && !c.BlockHold, "click survives repeated control calls until consumed");
        }
        s.AttackInputProcessed(true, true, false, false, .04);
        Neutral(s.Step(.06, false), "single click released while waiting for animation");
        s.Step(.08, true);
        Check(s.Current == Stage.WaitFirstEnd, "must observe real attack start");
        return s;
    }

    static void CompleteWithDuration(double duration)
    {
        var s = Started();
        for (double now = .10; now < duration; now += .02)
            Neutral(s.Step(now, true), "no block during attack, including hit-stop");
        // A stale block observation before we emit RMB must not count.
        s.BlockProcessed(true, true);
        var block = s.Step(duration, false);
        Check(block.BlockHold && block.Block && !block.AttackHold, "block only after attack actually ends");
        s.BlockProcessed(true, false);
        Check(s.Step(duration + .02, false).BlockHold, "input predicate alone is not engine block acknowledgement");
        s.BlockProcessed(true, true);
        var holdAt = duration + .04;
        var c = s.Step(holdAt, false);
        Check(c.Attack && c.AttackHold && !c.BlockHold && !c.Block, "release block and begin normal second hold");
        s.AttackInputProcessed(true, true, false, true, holdAt);
        c = s.Step(holdAt + .02, true);
        Check(c.AttackHold && !c.Attack, "held LMB does not repeatedly synthesize new click edges");
        Check(s.Step(holdAt + 1.19, false).AttackHold, "1.2 second real-time hold retained");
        Neutral(s.Step(holdAt + 1.21, false), "release final hold");
        s.AttackInputProcessed(false, false, false, false, holdAt + 1.22);
        Check(!s.Step(holdAt + 1.23, false).Override, "restore physical input after neutral controls processed");
        s.Rearm(true, holdAt + 1.24);
        Check(!s.Begin(holdAt + 1.25), "held Mouse5 must not loop");
        s.Rearm(false, holdAt + 1.26);
        Check(s.Begin(holdAt + 1.27), "release re-arms next press");
    }

    static void Main(string[] args)
    {
        CompleteWithDuration(.45);
        CompleteWithDuration(.90);
        CompleteWithDuration(1.80);
        // Sweep different first-swing durations across physics tick boundaries.
        for (int i = 0; i < 60; ++i) CompleteWithDuration(.12 + i * .037);
        var noStart = new BlockCancelSequence();
        noStart.Begin(0); noStart.Step(0, false);
        noStart.AttackInputProcessed(true, true, false, false, .02);
        Neutral(noStart.Step(2, false), "failed attack aborts; does not force attack/block");
        var noConsumer = new BlockCancelSequence();
        noConsumer.Begin(0); noConsumer.Step(0, false);
        noConsumer.AttackInputProcessed(false, false, false, false, .02);
        Neutral(noConsumer.Step(2, false), "unconsumed input watchdog");
        var noEnd = Started();
        Neutral(noEnd.Step(7, true), "non-ending attack aborts without block");
        var noBlock = Started();
        noBlock.Step(.45, false);
        Neutral(noBlock.Step(2.1, false), "missing block acknowledgement aborts without follow-up attack");
        var cancelled = Started();
        cancelled.Cancel(.1, "focus lost");
        Neutral(cancelled.Step(.11, true), "cancellation releases all injected combat input");
        cancelled.BlockProcessed(true, true);
        Neutral(cancelled.Step(.12, false), "late engine callback cannot restart cancelled sequence");
        var departed = Started();
        departed.OwnerGone(.2);
        Check(!departed.Step(.21, false).Override, "no controls injected into replacement player");
        departed.Rearm(false, .22);
        Check(departed.Begin(.23), "player replacement does not leave state machine stuck");
        Console.WriteLine($"PASS: {_checks} state/input assertions; 63 swing-duration scenarios. This is not a Valheim gameplay test.");
        SwordTests.Run();
        SwordSkillTests.Run();
        SwordTempoTests.Run();
        if (args.Length == 1) BinaryContractTests.Run(args[0]);
    }
}
