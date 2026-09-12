using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Reflection.Emit;

static class BinaryContractTests
{
    internal static void Run(string path)
    {
        using var stream=File.OpenRead(path);
        using var pe=new PEReader(stream);
        var md=pe.GetMetadataReader();
        if(md.GetAssemblyDefinition().Version!=new Version(3,5,0,0)) throw new Exception("Wrong DLL version");
        var prohibited=new HashSet<string>{"Message","GetAllCharacters","IsEnemy","set_speed","set_timeScale",
            "SetTrigger","CrossFade","Play","Clone","StartAttack","Abort","ResetChain","ModifyDamage","HaveQueuedChain"};
        foreach(var h in md.MemberReferences)
        {
            var name=md.GetString(md.GetMemberReference(h).Name);
            if(prohibited.Contains(name)) throw new Exception("Input-only DLL has a prohibited engine call: "+name);
        }
        var opcodes=typeof(OpCodes).GetFields().Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null))
            .ToDictionary(o=>unchecked((ushort)o.Value));
        var methods=new HashSet<string>();
        foreach(var h in md.MethodDefinitions)
        {
            var method=md.GetMethodDefinition(h); var name=md.GetString(method.Name); methods.Add(name);
            if(name.Contains("SwordGuard") || name.Contains("SwordTempo") || name=="SkillPrepareAttack")
                throw new Exception("Removed custom skill remains in binary: "+name);
            if(method.RelativeVirtualAddress==0) continue;
            var il=pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
            for(int i=0;i<il.Length;)
            {
                ushort code=il[i++]; if(code==0xfe) code=(ushort)(0xfe00|il[i++]);
                var op=opcodes[code]; int size;
                switch(op.OperandType)
                {
                    case OperandType.InlineNone:size=0;break;
                    case OperandType.ShortInlineI:case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineVar:size=1;break;
                    case OperandType.InlineVar:size=2;break;
                    case OperandType.InlineI8:case OperandType.InlineR:size=8;break;
                    case OperandType.InlineSwitch:size=4+4*BitConverter.ToInt32(il,i);break;
                    default:size=4;break;
                }
                if(op.OperandType==OperandType.InlineField || op.OperandType==OperandType.InlineMethod)
                {
                    var token=MetadataTokens.EntityHandle(BitConverter.ToInt32(il,i));
                    if(token.Kind==HandleKind.MemberReference)
                    {
                        var r=md.GetMemberReference((MemberReferenceHandle)token); var member=md.GetString(r.Name);
                        // Harmony patch priority is plugin setup metadata, not
                        // game state. Keep the exception exact and reject every
                        // other external field store.
                        bool patchPriority = false;
                        if(r.Parent.Kind==HandleKind.TypeReference)
                        {
                            var owner=md.GetTypeReference((TypeReferenceHandle)r.Parent);
                            patchPriority=md.GetString(owner.Namespace)=="HarmonyLib"
                                && md.GetString(owner.Name)=="HarmonyMethod" && member=="priority";
                        }
                        if((op==OpCodes.Stfld || op==OpCodes.Stsfld) && !patchPriority)
                            throw new Exception("External state field write in input-only DLL: "+name+" -> "+member);
                        if(member=="SetValue" && name!="ClearSwordQueues")
                            throw new Exception("Reflection write outside two input queue cleanups: "+name);
                    }
                }
                i+=size;
            }
        }
        foreach(var needed in new[]{"TryBeginSwordInput","SwordConsumerPrefix","SwordConsumerPostfix","SwordAttackAccepted",
            "SwordBlockProcessed","SwordMeleeAfter","DisableSwordInput","ReleaseSwordInput"})
            if(!methods.Contains(needed)) throw new Exception("Missing native input method "+needed);
        Console.WriteLine("PASS: shipped DLL is input-only: no direct attack creation/start, animation/speed/chain/damage mutation, game field stores, guard or Message dependency. Reflection writes confined to input queue cleanup; Harmony patch priority metadata allowed.");
    }
}
