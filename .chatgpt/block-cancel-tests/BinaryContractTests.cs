using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

static class BinaryContractTests
{
    internal static void Run(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        if (metadata.GetAssemblyDefinition().Version != new Version(3, 3, 1, 0))
            throw new Exception("Wrong plugin version in binary contract test.");
        foreach (var handle in metadata.MemberReferences)
        {
            string name = metadata.GetString(metadata.GetMemberReference(handle).Name);
            // Regression for the user's 1,423 repeated MissingMethodException
            // messages. Verify the SHIPPED DLL, not only source text or config.
            if (name == "Message" || name == "GetAllCharacters" || name == "IsEnemy")
                throw new Exception("Removed guard/message dependency still present in DLL: " + name);
            if (name == "m_damageMultiplierByTotalHealthMissing" || name == "m_damageMultiplierPerMissingHP")
                throw new Exception("Version-sensitive optional attack field is statically linked: " + name);
        }
        foreach (var handle in metadata.TypeReferences)
        {
            string name = metadata.GetString(metadata.GetTypeReference(handle).Name);
            if (name == "MessageHud" || name == "MessageType")
                throw new Exception("Removed message API type still present: " + name);
        }
        var methods = new HashSet<string>();
        foreach (var handle in metadata.MethodDefinitions)
        {
            string name = metadata.GetString(metadata.GetMethodDefinition(handle).Name);
            methods.Add(name);
            if (name.Contains("SwordGuard") || name == "TickGuardHotkey" || name == "HasMeleeThreat")
                throw new Exception("Automatic guard code is still compiled: " + name);
        }
        foreach (string name in new[] { "TryBeginSwordSkill", "TickSwordSkill", "SkillPrepareAttack", "HasBlockClip",
            "DisableSwordSkill", "ClearSwordReferences", "SkillMeleePrefix", "CancelSwordSkill" })
            if (!methods.Contains(name)) throw new Exception("Missing sword integration method: " + name);
        Console.WriteLine("PASS: shipped DLL has no Character.Message, MessageHud, auto-guard logic or static optional health-modifier field dependencies; sword integration retained.");
    }
}
