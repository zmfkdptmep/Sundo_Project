using Valheim.Foresight.Services.Hud;

var assertions = 0;
void Check(bool value, string message)
{
    assertions++;
    if (!value) throw new Exception(message);
}

// 0 = success, 1 = native FALSE, 2 = unavailable entry point.
// Both APIs must be attempted, including when the first call fails or throws.
for (var primary = 0; primary < 3; primary++)
for (var secondary = 0; secondary < 3; secondary++)
{
    var calls = new List<string>();
    var error = 0;
    var errorReads = 0;
    bool Call(string name, int behavior, int code)
    {
        calls.Add(name);
        error = code;
        if (behavior == 2) throw new EntryPointNotFoundException(name);
        return behavior == 0;
    }
    var result = CaptureExclusionPolicy.Apply(
        () => Call("affinity", primary, 8),
        () => Call("dda", secondary, 87),
        () => { errorReads++; return error; });

    Check(string.Join(",", calls) == "affinity,dda", "An API path was skipped or called twice.");
    Check(result.DisplayAffinityApplied == (primary == 0), "Incorrect affinity result.");
    Check(result.DesktopDuplicationApplied == (secondary == 0), "Incorrect DDA result.");
    Check(result.CanPublish == (primary == 0 || secondary == 0), "Publishing gate is incorrect.");
    Check(errorReads == (primary == 1 ? 1 : 0) + (secondary == 1 ? 1 : 0), "Last error was read on a successful call.");
    if (primary == 1) Check(result.DisplayAffinityStatus == "failed (Win32 8)", "Primary error overwritten by secondary call.");
    if (secondary == 1) Check(result.DesktopDuplicationStatus == "failed (Win32 87)", "Secondary error missing.");
    if (primary == 2) Check(result.DisplayAffinityStatus.Contains("EntryPointNotFoundException"), "Missing primary exception diagnostic.");
    if (secondary == 2) Check(result.DesktopDuplicationStatus.Contains("EntryPointNotFoundException"), "Missing secondary exception diagnostic.");
}

// Exact user-log regression: primary reports Win32 8, legacy DDA succeeds.
var recovered = CaptureExclusionPolicy.Apply(() => false, () => true, () => 8);
Check(recovered.CanPublish && !recovered.DisplayAffinityApplied && recovered.DesktopDuplicationApplied,
    "Win32 8 must allow the existing DDA fallback.");
Check(recovered.Diagnostic == "DisplayAffinity=failed (Win32 8); DDA=applied", "Fallback log is misleading.");

// A failed window/attempt must not poison another window or a later retry.
var failed = CaptureExclusionPolicy.Apply(() => false, () => false, () => 8);
Check(!failed.CanPublish, "A window with no successful exclusion must remain hidden.");
var retry = CaptureExclusionPolicy.Apply(() => false, () => true, () => 8);
Check(retry.CanPublish, "Failure persisted into the next attempt.");
var otherWindow = CaptureExclusionPolicy.Apply(() => true, () => false, () => 87);
Check(otherWindow.CanPublish, "One window disabled another window.");

Console.WriteLine($"PASS: {assertions} assertions; 9 API outcome combinations, Win32 8 fallback, retry and independent windows.");
