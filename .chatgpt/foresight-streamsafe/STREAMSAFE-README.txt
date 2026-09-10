Valheim Foresight 2.0.1 - StreamSafe custom build

Purpose
- Keeps Foresight combat calculation, attack learning, castbar timing, settings, and server compatibility unchanged.
- Moves the visible enemy name/castbar/threat-icon assistance to capture-excluded Windows overlay windows.
- The Valheim framebuffer is restored to vanilla before normal game rendering, so supported window/game/desktop capture paths do not include the Foresight assistance overlay.

Capture exclusion used
- SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)
- WINDOWCOMPOSITIONATTRIB WCA_EXCLUDED_FROM_DDA (Desktop Duplication exclusion)

Install
1. Back up your current Valheim.Foresight.dll.
2. Replace only Valheim.Foresight.dll in your BepInEx/plugins Foresight folder with this build.
3. Keep the same dependencies as original Foresight 2.0.1 (BepInEx and YamlDotNet).
4. Existing coffeenova.valheim.foresight.cfg and learned attack timing data are reused.

Notes
- Windows only. On non-Windows platforms the mod falls back to the original in-game HUD renderer.
- Borderless/windowed fullscreen is recommended. True exclusive fullscreen can prevent top-level overlays from being visible.
- Capture exclusion depends on the capture software respecting the Windows exclusion APIs. OBS/Discord capture modes using Windows Graphics Capture or Desktop Duplication are the intended targets.
- The F7 timing editor itself remains an in-game diagnostic/editor UI. Do not open it on-stream if you need that editor hidden too.
