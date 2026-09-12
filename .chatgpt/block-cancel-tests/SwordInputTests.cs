using Goni.DaggerPerfectCancel;
using Stage = Goni.DaggerPerfectCancel.SwordInputSequence.Stage;

static class SwordInputTests
{
    static int _checks;
    static void Check(bool ok, string why) { ++_checks; if (!ok) throw new Exception("Native input: " + why); }
    static void Neutral(SwordInputSequence s, string why) => Check(s.Output().Neutral, why);
    static void Both(SwordInputSequence s, bool edges)
    {
        var c=s.Output();
        Check(c.Override && c.AttackHold && c.SecondaryHold && c.Attack==edges && c.Secondary==edges && !c.Block,
            "both buttons delivered together, with matching press edges");
    }
    static SwordInputSequence PostBlock(double end=.4)
    {
        var s=new SwordInputSequence(); s.Begin(0);
        for(int i=0;i<10;i++) Check(s.Output().Attack && !s.Output().Secondary,"render calls cannot erase click");
        var first=s.Output();
        s.AttackAccepted(false,.01);
        s.ConsumerCompleted(first,.01);
        Check(s.OpeningStarts==1,"one accepted opening");
        for(double t=.02;t<end;t+=.01) { s.Tick(t,true); Neutral(s,"hit-stop does not advance opening"); }
        s.BlockProcessed(true,true,true,end-.001);
        Check(s.Current==Stage.OpeningSwing,"stale block cannot arm follow-up");
        s.Tick(end,false);
        Check(s.Output().Block,"block follows native InAttack boundary");
        s.BlockProcessed(true,true,false,end+.001);
        Check(s.Current==Stage.Block,"input predicate alone insufficient");
        s.BlockProcessed(true,true,true,end+.002);
        var c=s.Output();
        Check(c.Attack && c.AttackHold && !c.Secondary && !c.Block,"single primary after engine block");
        return s;
    }
    static SwordInputSequence Dual(double end=.4)
    {
        var s=PostBlock(end);
        var post=s.Output();
        s.AttackAccepted(false,end+.01);
        Check(s.PostBlockStarts==1 && s.Current==Stage.PostBlockAccepted,"one click acknowledged at START, not hit");
        Neutral(s,"do not change secondary half-way through native consumer");
        s.ConsumerCompleted(post,end+.01);
        Both(s,true); // Crucial regression: no melee notification was sent.
        Check(s.PostBlockStarts==1 && s.SecondaryEvents==0,"dual starts before any post-block impact");
        return s;
    }
    static void NativeConsumerModel()
    {
        // Model the inspected game's TWO sequential StartAttack calls. During
        // dual input secondary clears the primary queue, but primary HOLD must
        // remain true so the native primary branch and HaveQueuedChain work.
        for(int scenario=0;scenario<90;scenario++)
        {
            var s=Dual(.1+scenario*.021);
            var now=.2+scenario*.021;
            for(int tick=0;tick<8;tick++)
            {
                var c=s.Output();
                double primaryQueue=0, secondaryQueue=0;
                if(c.Attack) { primaryQueue=.5; secondaryQueue=0; }
                if(c.Secondary) { secondaryQueue=.5; primaryQueue=0; }
                primaryQueue-=.02; secondaryQueue-=.02;
                Check(primaryQueue>0 || c.AttackHold,"primary remains attempted despite simultaneous secondary edge");
                Check(secondaryQueue>0 || c.SecondaryHold,"secondary attempted in same consumer");
                s.AttackAccepted(false,now); s.AttackAccepted(true,now);
                s.ConsumerCompleted(c,now);
                Both(s,false);
                Check(s.PostBlockStarts==1,"dual primary attempts are not another post-block single click");
                now+=.02;
            }
            // Three native events may belong to a SINGLE secondary Attack.
            for(int eventIndex=1;eventIndex<=3;eventIndex++)
            {
                s.Tick(now,true);
                Check(s.SecondaryMelee(now),"count native event, including a miss");
                Check(s.SecondaryEvents==eventIndex,"one event per callback, not targets");
                now+=.1+scenario*.005;
            }
            Check(s.Current==Stage.Releasing,"release on third observed secondary event");
            Neutral(s,"release both buttons immediately");
            Check(!s.SecondaryMelee(now),"late event cannot extend automation");
            s.Neutralized(now); s.Rearm(true,now);
            Check(!s.Begin(now),"held Mouse5 does not loop");
            s.Rearm(false,now);
            Check(s.Begin(now),"next press may rearm");
        }
    }
    static void RejectionAndInterruptions()
    {
        var noStart=new SwordInputSequence(); noStart.Begin(0);
        var c=noStart.Output(); noStart.ConsumerCompleted(c,.01);
        Neutral(noStart,"a rejected click is not repeatedly held");
        noStart.Tick(2,false);
        Check(noStart.Result.StartsWith("ABORT"),"reject timeout cannot fabricate a hit");
        var queued=PostBlock(); c=queued.Output(); queued.ConsumerCompleted(c,.41);
        Neutral(queued,"only one post-block click even if startup is queued");
        queued.AttackAccepted(false,.43);
        queued.ConsumerCompleted(queued.Output(),.43);
        Both(queued,true);
        var missingConsumer=PostBlock(); missingConsumer.AttackAccepted(false,.42); missingConsumer.Tick(3,false);
        Check(missingConsumer.Current==Stage.Releasing,"accepted click without returning consumer times out");
        var two=Dual(); two.ConsumerCompleted(two.Output(),.42);
        two.SecondaryMelee(.5); two.SecondaryMelee(.8); two.Tick(10,true);
        Check(two.SecondaryEvents==2 && two.Result.StartsWith("ABORT"),"do not replace missing third hit");
        foreach(var s in new[]{PostBlock(),Dual()})
        {
            s.Cancel(.5,"F12/focus/weapon interruption"); Neutral(s,"cancel clears both inputs");
            s.AttackAccepted(false,.51); s.AttackAccepted(true,.51);
            s.BlockProcessed(true,true,true,.51); s.ConsumerCompleted(default,.51);
            Check(!s.SecondaryMelee(.51) && s.Current==Stage.Releasing,"late callbacks cannot restart cancellation");
            s.Neutralized(.52); Check(!s.Running,"cleanup retires state even without another consumer");
        }
    }
    internal static void Run()
    {
        NativeConsumerModel(); RejectionAndInterruptions();
        Console.WriteLine($"PASS: {_checks} native-input assertions; 90 startup/timing traces, native dual-input branch model, queued acceptance and cancellation. Native animation exploit is NOT gameplay-tested.");
    }
}
