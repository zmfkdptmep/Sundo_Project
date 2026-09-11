using System;

namespace Valheim.Foresight.Services.Hud;

// Keep the two existing native paths independent. A failed first call must not
// prevent the legacy desktop-duplication path from being attempted (r2 regression).
internal static class CaptureExclusionPolicy
{
    internal readonly struct Result
    {
        internal readonly bool DisplayAffinityApplied;
        internal readonly bool DesktopDuplicationApplied;
        internal readonly string DisplayAffinityStatus;
        internal readonly string DesktopDuplicationStatus;

        internal Result(bool affinity, bool dda, string affinityStatus, string ddaStatus)
        {
            DisplayAffinityApplied = affinity;
            DesktopDuplicationApplied = dda;
            DisplayAffinityStatus = affinityStatus;
            DesktopDuplicationStatus = ddaStatus;
        }

        // API success permits publishing; the actual capture result still depends
        // on the user's Windows/capture backend and needs an OBS check.
        internal bool CanPublish => DisplayAffinityApplied || DesktopDuplicationApplied;
        internal string Diagnostic => $"DisplayAffinity={DisplayAffinityStatus}; DDA={DesktopDuplicationStatus}";
    }

    internal static Result Apply(Func<bool> displayAffinity, Func<bool> desktopDuplication, Func<int> lastError)
    {
        var affinity = Attempt(displayAffinity, lastError, out var affinityStatus);
        var dda = Attempt(desktopDuplication, lastError, out var ddaStatus);
        return new Result(affinity, dda, affinityStatus, ddaStatus);
    }

    private static bool Attempt(Func<bool> apply, Func<int> lastError, out string status)
    {
        try
        {
            if (apply())
            {
                status = "applied";
                return true;
            }
            status = $"failed (Win32 {lastError()})";
        }
        catch (Exception ex)
        {
            status = $"unavailable ({ex.GetType().Name}: {ex.Message})";
        }
        return false;
    }
}
