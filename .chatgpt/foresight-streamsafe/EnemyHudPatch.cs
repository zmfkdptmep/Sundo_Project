using System.Collections;
using TMPro;
using UnityEngine;
using Valheim.Foresight.HarmonyRefs;
using Valheim.Foresight.Models;
using Valheim.Foresight.Services.Hud;

namespace Valheim.Foresight.Patches;

internal class EnemyHudPatch
{
    private static readonly Color SafeColor = Color.white;
    private static readonly Color CautionColor = new(1f, 0.75f, 0.25f);
    private static readonly Color BlockLethalColor = new(1f, 0.5f, 0.1f);
    private static readonly Color DangerColor = new(1f, 0.2f, 0.2f);

    internal static void LateUpdatePostfix(EnemyHud __instance)
    {
        var streamSafe = StreamSafeOverlay.IsAvailable;
        if (streamSafe)
            StreamSafeOverlay.BeginFrame();

        try
        {
            var player = Player.m_localPlayer;
            if (player == null)
                return;

            var huds = EnemyHudPrivateAccess.GetHudsAsDictionary(__instance);
            if (huds == null || huds.Count == 0)
                return;

            ValheimForesightPlugin.ActiveAttackTracker?.CleanupExpired();

            foreach (DictionaryEntry entry in huds)
            {
                var character = entry.Key as Character;
                var hudObj = entry.Value;
                if (character == null || hudObj == null)
                    continue;

                var nameLabel = EnemyHudPrivateAccess.TryGetNameLabel(hudObj);
                if (nameLabel == null)
                    continue;

                var holder = nameLabel.GetComponent<OriginalNameHolder>();
                if (holder is null)
                {
                    holder = nameLabel.gameObject.AddComponent<OriginalNameHolder>();
                    holder.originalName = nameLabel.text;
                }

                if (!ValheimForesightPlugin.TryGetThreatAssessment(character, out var assessment) || assessment == null)
                    continue;

                ColorizeByThreatLevel(nameLabel, assessment.Level);

                ThreatResponseHint hint;
                try { hint = ValheimForesightPlugin.ThreatResponseHintService.GetHint(assessment); }
                catch { hint = ThreatResponseHint.None; }

                ValheimForesightPlugin.HudIconRenderer?.RenderIcon(nameLabel, hint);

                var activeAttack = ValheimForesightPlugin.ActiveAttackTracker?.GetActiveAttack(character);
                var hudParent = nameLabel.transform.parent ?? nameLabel.transform;
                ValheimForesightPlugin.CastbarRenderer?.RenderCastbar(hudParent, activeAttack, character);

                if (ValheimForesightPlugin.InstanceDebugHudEnabled)
                    AppendDebugInfo(nameLabel, holder.originalName, assessment);
                else
                    nameLabel.text = holder.originalName;

                if (streamSafe)
                {
                    // EnemyHud keeps pooled HUD objects alive for characters that are not currently
                    // visible.  Those objects can sit at a sentinel/off-screen UI position and are
                    // normally hidden by the vanilla hierarchy.  Never clone such labels into the
                    // native overlay: doing so makes every nearby/off-camera creature name pile up
                    // at that sentinel position.
                    if (IsActuallyVisibleHud(character, nameLabel))
                    {
                        StreamSafeOverlay.CaptureAndHide(character, nameLabel, hudParent, holder.originalName);
                    }
                    else
                    {
                        // Leave the vanilla hidden HUD hierarchy alone, but make sure no Foresight
                        // colour/text leaks into a future vanilla frame before the object is reused.
                        nameLabel.text = holder.originalName;
                        nameLabel.color = Color.white;
                    }
                }
            }
        }
        finally
        {
            if (streamSafe)
                StreamSafeOverlay.EndFrame();
        }
    }

    private static bool IsActuallyVisibleHud(Character character, TextMeshProUGUI nameLabel)
    {
        if (character == null || nameLabel == null)
            return false;

        if (!character.gameObject.activeInHierarchy || character.IsDead())
            return false;

        if (!nameLabel.enabled || !nameLabel.gameObject.activeInHierarchy)
            return false;

        // Respect the same effective visibility the vanilla UI hierarchy uses.
        // The pooled EnemyHud entries for off-camera creatures commonly have alpha 0 upstream.
        try
        {
            if (nameLabel.canvasRenderer != null && nameLabel.canvasRenderer.GetAlpha() <= 0.01f)
                return false;
        }
        catch { }

        float effectiveAlpha = 1f;
        for (Transform t = nameLabel.transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeInHierarchy)
                return false;

            var groups = t.GetComponents<CanvasGroup>();
            if (groups == null)
                continue;

            foreach (var group in groups)
            {
                if (group == null || !group.enabled)
                    continue;
                effectiveAlpha *= group.alpha;
                if (effectiveAlpha <= 0.01f)
                    return false;
            }
        }

        // A pooled HUD can still own a perfectly valid RectTransform while its character is behind
        // the camera or outside the viewport.  Verify the world object itself, not only the UI rect.
        var camera = Camera.main;
        if (camera != null)
        {
            var worldPoint = character.transform.position + Vector3.up * 1.2f;
            var sp = camera.WorldToScreenPoint(worldPoint);
            if (sp.z <= 0.01f)
                return false;

            const float edgeTolerance = 32f;
            if (sp.x < -edgeTolerance || sp.x > Screen.width + edgeTolerance ||
                sp.y < -edgeTolerance || sp.y > Screen.height + edgeTolerance)
                return false;
        }

        // Finally require the actual name rect itself to overlap the screen. Do not clamp an invalid
        // off-screen/sentinel rect into the visible area.
        var rect = GetScreenRect(nameLabel.rectTransform);
        if (rect.width <= 0.5f || rect.height <= 0.5f)
            return false;
        if (rect.xMax <= 0f || rect.yMax <= 0f || rect.xMin >= Screen.width || rect.yMin >= Screen.height)
            return false;

        return true;
    }

    private static Rect GetScreenRect(RectTransform rect)
    {
        if (rect == null)
            return default;

        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        var canvas = rect.GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        var bl = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
        var tr = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);
        return Rect.MinMaxRect(
            Mathf.Min(bl.x, tr.x),
            Mathf.Min(bl.y, tr.y),
            Mathf.Max(bl.x, tr.x),
            Mathf.Max(bl.y, tr.y)
        );
    }

    private static void ColorizeByThreatLevel(TextMeshProUGUI nameLabel, ThreatLevel level)
    {
        nameLabel.color = level switch
        {
            ThreatLevel.Safe => SafeColor,
            ThreatLevel.Caution => CautionColor,
            ThreatLevel.BlockLethal => BlockLethalColor,
            ThreatLevel.Danger => DangerColor,
            _ => SafeColor,
        };
    }

    private static void AppendDebugInfo(TextMeshProUGUI nameLabel, string originalName, ThreatAssessment assessment)
    {
        var mode = assessment.UsedRangedAttack ? "R" : "M";
        var levelCode = assessment.Level switch
        {
            ThreatLevel.Safe => "SAFE",
            ThreatLevel.Caution => "CAUT",
            ThreatLevel.BlockLethal => "BLCK",
            ThreatLevel.Danger => "DNG",
            _ => "UNK",
        };

        nameLabel.text = originalName
            + $" [{levelCode}-{mode} "
            + $"r={assessment.DamageToHealthRatio:F2} "
            + $"raw={assessment.DamageInfo.RawDamage:F1} "
            + $"eff={assessment.DamageInfo.EffectiveDamageWithBlock:F1}]";
    }

    private sealed class OriginalNameHolder : MonoBehaviour
    {
        public string originalName = string.Empty;
    }
}
