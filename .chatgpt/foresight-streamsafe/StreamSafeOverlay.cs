using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.Foresight.Models;

namespace Valheim.Foresight.Services.Hud;

internal static class StreamSafeOverlay
{
    private const int OverlayLayer = 30;
    private const int Padding = 3;

    private static readonly Dictionary<int, NameClone> NameClones = new();
    private static readonly Dictionary<int, ReadbackSlot> Readbacks = new();
    private static readonly Dictionary<int, NativeRegionWindow> Windows = new();
    private static readonly List<MovedUi> Moved = new();
    private static readonly Dictionary<int, Rect> Regions = new();
    private static readonly HashSet<int> SeenThisFrame = new();

    private static GameObject? _root;
    private static Camera? _camera;
    private static Canvas? _canvas;
    private static RenderTexture? _renderTexture;
    private static int _renderWidth;
    private static int _renderHeight;
    private static bool _initialized;
    private static bool _failed;

    internal static bool IsAvailable => EnsureInitialized();

    internal static void BeginFrame()
    {
        if (!EnsureInitialized())
            return;

        RestoreMovedUi();
        Regions.Clear();
        SeenThisFrame.Clear();

        foreach (var clone in NameClones.Values)
        {
            if (clone.Component != null)
                clone.Component.gameObject.SetActive(false);
        }

        EnsureRenderTarget();
    }

    internal static void CaptureAndHide(Character character, TextMeshProUGUI nameLabel, Transform hudParent, string vanillaName)
    {
        if (!_initialized || _failed || _canvas == null)
            return;

        var key = character.GetInstanceID();
        SeenThisFrame.Add(key);
        Rect? union = null;

        var nameRect = GetScreenRect(nameLabel.rectTransform);
        if (IsUsableRect(nameRect))
        {
            var clone = GetOrCreateNameClone(key, nameLabel);
            if (clone != null)
            {
                CopyTextState(nameLabel, clone.Component);
                PlaceOnOverlay(nameLabel.rectTransform, clone.Component.rectTransform, nameRect);
                clone.Component.gameObject.SetActive(true);
                union = Union(union, nameRect);
            }
        }

        var icon = hudParent.Find("Foresight_ThreatIcon");
        if (icon != null && icon.gameObject.activeSelf)
        {
            var iconRect = GetScreenRect(icon as RectTransform ?? icon.GetComponent<RectTransform>());
            if (IsUsableRect(iconRect))
            {
                MoveToOverlay(icon, iconRect);
                union = Union(union, iconRect);
            }
        }

        var castbar = hudParent.Find("Foresight_Castbar");
        if (castbar != null && castbar.gameObject.activeSelf)
        {
            var castbarRect = GetScreenRect(castbar as RectTransform ?? castbar.GetComponent<RectTransform>());
            if (IsUsableRect(castbarRect))
            {
                MoveToOverlay(castbar, castbarRect);
                union = Union(union, castbarRect);
            }
        }

        nameLabel.text = vanillaName;
        nameLabel.color = Color.white;

        if (union.HasValue)
            Regions[key] = ClampAndPad(union.Value, Padding);
    }

    internal static void EndFrame()
    {
        if (!_initialized || _failed || _camera == null || _renderTexture == null)
        {
            HideAllNativeWindows();
            return;
        }

        try
        {
            if (Regions.Count == 0 || !NativeMethods.TryGetGameClient(out _, out _, out _, out _))
            {
                HideAllNativeWindows();
                return;
            }

            Canvas.ForceUpdateCanvases();
            _camera.targetTexture = _renderTexture;
            _camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            try
            {
                foreach (var pair in Regions)
                {
                    var key = pair.Key;
                    var rect = pair.Value;
                    if (!ReadRegion(key, rect, out var pixels, out var width, out var height))
                        continue;

                    if (!Windows.TryGetValue(key, out var window))
                    {
                        window = new NativeRegionWindow();
                        Windows[key] = window;
                    }

                    window.Update(rect, pixels, width, height);
                }
            }
            finally
            {
                RenderTexture.active = previous;
            }

            foreach (var pair in Windows)
            {
                if (!SeenThisFrame.Contains(pair.Key))
                    pair.Value.Hide();
            }
        }
        catch (Exception ex)
        {
            _failed = true;
            ValheimForesightPlugin.Log?.LogWarning($"[StreamSafeOverlay] Overlay disabled after rendering failure: {ex.Message}");
            HideAllNativeWindows();
            RestoreMovedUi();
        }
    }

    internal static void Shutdown()
    {
        RestoreMovedUi();
        foreach (var window in Windows.Values) window.Dispose();
        Windows.Clear();
        foreach (var slot in Readbacks.Values) if (slot.Texture != null) UnityEngine.Object.Destroy(slot.Texture);
        Readbacks.Clear();
        foreach (var clone in NameClones.Values) if (clone.Component != null) UnityEngine.Object.Destroy(clone.Component.gameObject);
        NameClones.Clear();
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            UnityEngine.Object.Destroy(_renderTexture);
            _renderTexture = null;
        }
        if (_root != null) UnityEngine.Object.Destroy(_root);
        _root = null;
        _camera = null;
        _canvas = null;
        _renderWidth = 0;
        _renderHeight = 0;
        _initialized = false;
        _failed = false;
    }

    private static bool EnsureInitialized()
    {
        if (_failed) return false;
        if (_initialized && _root != null && _canvas != null && _camera != null) return true;
        if (Application.platform != RuntimePlatform.WindowsPlayer)
        {
            _failed = true;
            return false;
        }

        try
        {
            _root = new GameObject("Foresight_StreamSafeOverlay");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            _root.layer = OverlayLayer;

            var cameraObject = new GameObject("Foresight_StreamSafeCamera");
            cameraObject.transform.SetParent(_root.transform, false);
            cameraObject.layer = OverlayLayer;
            _camera = cameraObject.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.cullingMask = 1 << OverlayLayer;
            _camera.orthographic = true;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 100f;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;

            var canvasObject = new GameObject("Foresight_StreamSafeCanvas");
            canvasObject.transform.SetParent(_root.transform, false);
            canvasObject.layer = OverlayLayer;
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _camera;
            _canvas.planeDistance = 1f;
            _canvas.sortingOrder = 32760;
            _canvas.pixelPerfect = false;

            EnsureRenderTarget();
            _initialized = true;
            ValheimForesightPlugin.Log?.LogInfo("[StreamSafeOverlay] Capture-excluded overlay enabled.");
            return true;
        }
        catch (Exception ex)
        {
            _failed = true;
            ValheimForesightPlugin.Log?.LogWarning($"[StreamSafeOverlay] Could not initialize: {ex.Message}. Falling back to original HUD.");
            return false;
        }
    }

    private static void EnsureRenderTarget()
    {
        if (_camera == null) return;
        var width = Math.Max(1, Screen.width);
        var height = Math.Max(1, Screen.height);
        if (_renderTexture != null && _renderWidth == width && _renderHeight == height) return;
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            UnityEngine.Object.Destroy(_renderTexture);
        }
        _renderWidth = width;
        _renderHeight = height;
        _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "Foresight_StreamSafeRT",
            antiAliasing = 1,
            useMipMap = false,
            autoGenerateMips = false,
            filterMode = FilterMode.Bilinear,
        };
        _renderTexture.Create();
        _camera.targetTexture = _renderTexture;
        _camera.aspect = (float)width / height;
        foreach (var slot in Readbacks.Values) if (slot.Texture != null) UnityEngine.Object.Destroy(slot.Texture);
        Readbacks.Clear();
    }

    private static NameClone? GetOrCreateNameClone(int key, TextMeshProUGUI source)
    {
        if (_canvas == null) return null;
        if (NameClones.TryGetValue(key, out var existing) && existing.Component != null) return existing;
        var copy = UnityEngine.Object.Instantiate(source);
        copy.gameObject.name = "Foresight_StreamSafeName";
        copy.transform.SetParent(_canvas.transform, false);
        copy.raycastTarget = false;
        SetLayerRecursively(copy.gameObject, OverlayLayer);
        var clone = new NameClone(copy);
        NameClones[key] = clone;
        return clone;
    }

    private static void CopyTextState(TextMeshProUGUI source, TextMeshProUGUI destination)
    {
        destination.text = source.text;
        destination.color = source.color;
        destination.font = source.font;
        destination.fontSharedMaterial = source.fontSharedMaterial;
        destination.fontSize = source.fontSize;
        destination.fontSizeMin = source.fontSizeMin;
        destination.fontSizeMax = source.fontSizeMax;
        destination.enableAutoSizing = source.enableAutoSizing;
        destination.fontStyle = source.fontStyle;
        destination.alignment = source.alignment;
        destination.margin = source.margin;
        destination.textWrappingMode = source.textWrappingMode;
        destination.overflowMode = source.overflowMode;
        destination.characterSpacing = source.characterSpacing;
        destination.wordSpacing = source.wordSpacing;
        destination.lineSpacing = source.lineSpacing;
        destination.paragraphSpacing = source.paragraphSpacing;
        destination.richText = source.richText;
        destination.alpha = source.alpha;
        destination.enabled = source.enabled;
    }

    private static void MoveToOverlay(Transform transform, Rect screenRect)
    {
        if (_canvas == null) return;
        var rect = transform as RectTransform ?? transform.GetComponent<RectTransform>();
        if (rect == null) return;
        var state = new MovedUi(rect);
        Moved.Add(state);
        rect.SetParent(_canvas.transform, false);
        PlaceOnOverlay(state.RectTransform, rect, screenRect, state.LocalRectSize, state.Pivot);
        SetLayerRecursively(rect.gameObject, OverlayLayer);
    }

    private static void RestoreMovedUi()
    {
        for (var i = Moved.Count - 1; i >= 0; --i)
        {
            var state = Moved[i];
            var rect = state.RectTransform;
            if (rect == null || state.Parent == null) continue;
            rect.SetParent(state.Parent, false);
            rect.anchorMin = state.AnchorMin;
            rect.anchorMax = state.AnchorMax;
            rect.pivot = state.Pivot;
            rect.anchoredPosition = state.AnchoredPosition;
            rect.sizeDelta = state.SizeDelta;
            rect.localScale = state.LocalScale;
            rect.localRotation = state.LocalRotation;
            rect.SetSiblingIndex(Mathf.Clamp(state.SiblingIndex, 0, Math.Max(0, state.Parent.childCount - 1)));
            SetLayerRecursively(rect.gameObject, state.Layer);
        }
        Moved.Clear();
    }

    private static void PlaceOnOverlay(RectTransform source, RectTransform destination, Rect screenRect)
        => PlaceOnOverlay(source, destination, screenRect, source.rect.size, source.pivot);

    private static void PlaceOnOverlay(RectTransform source, RectTransform destination, Rect screenRect, Vector2 localRectSize, Vector2 pivot)
    {
        var localW = Mathf.Max(0.001f, Mathf.Abs(localRectSize.x));
        var localH = Mathf.Max(0.001f, Mathf.Abs(localRectSize.y));
        var scaleX = screenRect.width / localW;
        var scaleY = screenRect.height / localH;
        destination.anchorMin = Vector2.zero;
        destination.anchorMax = Vector2.zero;
        destination.pivot = pivot;
        destination.sizeDelta = localRectSize;
        destination.anchoredPosition = new Vector2(screenRect.xMin + pivot.x * screenRect.width, screenRect.yMin + pivot.y * screenRect.height);
        destination.localScale = new Vector3(scaleX, scaleY, 1f);
        destination.localRotation = Quaternion.identity;
    }

    private static Rect GetScreenRect(RectTransform? rect)
    {
        if (rect == null) return default;
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var canvas = rect.GetComponentInParent<Canvas>();
        Camera? eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) eventCamera = canvas.worldCamera;
        var bl = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
        var tr = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);
        return Rect.MinMaxRect(Math.Min(bl.x, tr.x), Math.Min(bl.y, tr.y), Math.Max(bl.x, tr.x), Math.Max(bl.y, tr.y));
    }

    private static Rect ClampAndPad(Rect rect, int padding)
    {
        var xMin = Mathf.Clamp(Mathf.Floor(rect.xMin) - padding, 0, Math.Max(0, Screen.width - 1));
        var yMin = Mathf.Clamp(Mathf.Floor(rect.yMin) - padding, 0, Math.Max(0, Screen.height - 1));
        var xMax = Mathf.Clamp(Mathf.Ceil(rect.xMax) + padding, xMin + 1, Screen.width);
        var yMax = Mathf.Clamp(Mathf.Ceil(rect.yMax) + padding, yMin + 1, Screen.height);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static bool IsUsableRect(Rect rect) => rect.width > 0.5f && rect.height > 0.5f && rect.xMax > 0 && rect.yMax > 0 && rect.xMin < Screen.width && rect.yMin < Screen.height;

    private static Rect? Union(Rect? a, Rect b)
    {
        if (!a.HasValue) return b;
        var r = a.Value;
        return Rect.MinMaxRect(Math.Min(r.xMin, b.xMin), Math.Min(r.yMin, b.yMin), Math.Max(r.xMax, b.xMax), Math.Max(r.yMax, b.yMax));
    }

    private static bool ReadRegion(int key, Rect rect, out Color32[] pixels, out int width, out int height)
    {
        width = Math.Max(1, Mathf.RoundToInt(rect.width));
        height = Math.Max(1, Mathf.RoundToInt(rect.height));
        pixels = Array.Empty<Color32>();
        if (!Readbacks.TryGetValue(key, out var slot) || slot.Width != width || slot.Height != height)
        {
            if (slot?.Texture != null) UnityEngine.Object.Destroy(slot.Texture);
            slot = new ReadbackSlot(new Texture2D(width, height, TextureFormat.RGBA32, false, false), width, height);
            Readbacks[key] = slot;
        }
        slot.Texture.ReadPixels(new Rect(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), width, height), 0, 0, false);
        slot.Texture.Apply(false, false);
        pixels = slot.Texture.GetPixels32();
        return pixels.Length == width * height;
    }

    private static void HideAllNativeWindows() { foreach (var window in Windows.Values) window.Hide(); }
    private static void SetLayerRecursively(GameObject root, int layer) { root.layer = layer; foreach (Transform child in root.transform) SetLayerRecursively(child.gameObject, layer); }

    private sealed class NameClone { internal readonly TextMeshProUGUI Component; internal NameClone(TextMeshProUGUI component) => Component = component; }
    private sealed class ReadbackSlot { internal readonly Texture2D Texture; internal readonly int Width; internal readonly int Height; internal ReadbackSlot(Texture2D texture, int width, int height) { Texture = texture; Width = width; Height = height; } }

    private sealed class MovedUi
    {
        internal readonly RectTransform RectTransform; internal readonly Transform? Parent; internal readonly int SiblingIndex; internal readonly Vector2 AnchorMin; internal readonly Vector2 AnchorMax; internal readonly Vector2 Pivot; internal readonly Vector2 AnchoredPosition; internal readonly Vector2 SizeDelta; internal readonly Vector3 LocalScale; internal readonly Quaternion LocalRotation; internal readonly int Layer; internal readonly Vector2 LocalRectSize;
        internal MovedUi(RectTransform rect) { RectTransform = rect; Parent = rect.parent; SiblingIndex = rect.GetSiblingIndex(); AnchorMin = rect.anchorMin; AnchorMax = rect.anchorMax; Pivot = rect.pivot; AnchoredPosition = rect.anchoredPosition; SizeDelta = rect.sizeDelta; LocalScale = rect.localScale; LocalRotation = rect.localRotation; Layer = rect.gameObject.layer; LocalRectSize = rect.rect.size; }
    }

    private sealed class NativeRegionWindow : IDisposable
    {
        private IntPtr _hwnd; private IntPtr _memoryDc; private IntPtr _dib; private IntPtr _oldBitmap; private IntPtr _bits; private int _width; private int _height; private bool _disposed;
        internal void Update(Rect unityRect, Color32[] pixels, int width, int height)
        {
            if (_disposed) return;
            if (!NativeMethods.TryGetGameClient(out var gameWindow, out var origin, out var clientWidth, out var clientHeight)) { Hide(); return; }
            if (_hwnd == IntPtr.Zero) { _hwnd = NativeMethods.CreateOverlayWindow(gameWindow); if (_hwnd == IntPtr.Zero) return; }
            EnsureBitmap(width, height); if (_bits == IntPtr.Zero) return;
            CopyPixelsPremultipliedAndFlip(pixels, width, height, _bits);
            var x = origin.X + Mathf.RoundToInt(unityRect.xMin);
            var y = origin.Y + clientHeight - Mathf.RoundToInt(unityRect.yMax);
            if (!NativeMethods.IsGameForeground(gameWindow)) { Hide(); return; }
            NativeMethods.UpdateLayered(_hwnd, _memoryDc, x, y, width, height);
        }
        private void EnsureBitmap(int width, int height)
        {
            if (_memoryDc != IntPtr.Zero && _width == width && _height == height) return;
            ReleaseBitmap(); _width = width; _height = height; _memoryDc = NativeMethods.CreateCompatibleDC(IntPtr.Zero); if (_memoryDc == IntPtr.Zero) return;
            var bmi = new NativeMethods.BITMAPINFO(); bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(NativeMethods.BITMAPINFOHEADER)); bmi.bmiHeader.biWidth = width; bmi.bmiHeader.biHeight = -height; bmi.bmiHeader.biPlanes = 1; bmi.bmiHeader.biBitCount = 32; bmi.bmiHeader.biCompression = 0;
            _dib = NativeMethods.CreateDIBSection(_memoryDc, ref bmi, 0, out _bits, IntPtr.Zero, 0); if (_dib != IntPtr.Zero) _oldBitmap = NativeMethods.SelectObject(_memoryDc, _dib);
        }
        private static unsafe void CopyPixelsPremultipliedAndFlip(Color32[] source, int width, int height, IntPtr destination)
        {
            var dst = (uint*)destination.ToPointer();
            for (var y = 0; y < height; ++y)
            {
                var srcRow = y * width; var dstRow = (height - 1 - y) * width;
                for (var x = 0; x < width; ++x)
                {
                    var c = source[srcRow + x]; var a = c.a; var r = (byte)((c.r * a + 127) / 255); var g = (byte)((c.g * a + 127) / 255); var b = (byte)((c.b * a + 127) / 255);
                    dst[dstRow + x] = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
                }
            }
        }
        internal void Hide() { if (_hwnd != IntPtr.Zero) NativeMethods.ShowWindow(_hwnd, NativeMethods.SW_HIDE); }
        public void Dispose() { if (_disposed) return; _disposed = true; ReleaseBitmap(); if (_hwnd != IntPtr.Zero) { NativeMethods.DestroyWindow(_hwnd); _hwnd = IntPtr.Zero; } }
        private void ReleaseBitmap() { if (_memoryDc != IntPtr.Zero && _oldBitmap != IntPtr.Zero) NativeMethods.SelectObject(_memoryDc, _oldBitmap); if (_dib != IntPtr.Zero) NativeMethods.DeleteObject(_dib); if (_memoryDc != IntPtr.Zero) NativeMethods.DeleteDC(_memoryDc); _memoryDc = IntPtr.Zero; _dib = IntPtr.Zero; _oldBitmap = IntPtr.Zero; _bits = IntPtr.Zero; _width = 0; _height = 0; }
    }

    private static class NativeMethods
    {
        internal const int SW_HIDE = 0; private const int SW_SHOWNOACTIVATE = 4; private const uint WS_POPUP = 0x80000000; private const uint WS_EX_LAYERED = 0x00080000; private const uint WS_EX_TRANSPARENT = 0x00000020; private const uint WS_EX_TOOLWINDOW = 0x00000080; private const uint WS_EX_NOACTIVATE = 0x08000000; private const uint ULW_ALPHA = 0x00000002; private const byte AC_SRC_OVER = 0x00; private const byte AC_SRC_ALPHA = 0x01; private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011; private const int WCA_EXCLUDED_FROM_DDA = 24; private const uint SWP_NOACTIVATE = 0x0010; private const uint SWP_SHOWWINDOW = 0x0040; private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly WndProcDelegate WndProc = WindowProc; private static ushort _classAtom; private static string? _className;
        [StructLayout(LayoutKind.Sequential)] internal struct POINT { internal int X; internal int Y; }
        [StructLayout(LayoutKind.Sequential)] internal struct SIZE { internal int cx; internal int cy; }
        [StructLayout(LayoutKind.Sequential)] internal struct RECT { internal int Left; internal int Top; internal int Right; internal int Bottom; }
        [StructLayout(LayoutKind.Sequential)] internal struct BLENDFUNCTION { internal byte BlendOp; internal byte BlendFlags; internal byte SourceConstantAlpha; internal byte AlphaFormat; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct WNDCLASSEX { internal uint cbSize; internal uint style; internal IntPtr lpfnWndProc; internal int cbClsExtra; internal int cbWndExtra; internal IntPtr hInstance; internal IntPtr hIcon; internal IntPtr hCursor; internal IntPtr hbrBackground; internal string? lpszMenuName; internal string lpszClassName; internal IntPtr hIconSm; }
        [StructLayout(LayoutKind.Sequential)] internal struct BITMAPINFOHEADER { internal uint biSize; internal int biWidth; internal int biHeight; internal ushort biPlanes; internal ushort biBitCount; internal uint biCompression; internal uint biSizeImage; internal int biXPelsPerMeter; internal int biYPelsPerMeter; internal uint biClrUsed; internal uint biClrImportant; }
        [StructLayout(LayoutKind.Sequential)] internal struct BITMAPINFO { internal BITMAPINFOHEADER bmiHeader; internal uint bmiColors; }
        [StructLayout(LayoutKind.Sequential)] private struct WINDOWCOMPOSITIONATTRIBDATA { internal int Attrib; internal IntPtr pvData; internal int cbData; }
        [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern ushort RegisterClassExW(ref WNDCLASSEX lpwcx);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern IntPtr CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
        [DllImport("user32.dll")] internal static extern bool DestroyWindow(IntPtr hwnd);
        [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);
        [DllImport("user32.dll", SetLastError = true)] private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WINDOWCOMPOSITIONATTRIBDATA data);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint flags);
        [DllImport("gdi32.dll", SetLastError = true)] internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll", SetLastError = true)] internal static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll", SetLastError = true)] internal static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint usage, out IntPtr bits, IntPtr section, uint offset);
        [DllImport("gdi32.dll", SetLastError = true)] internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll", SetLastError = true)] internal static extern bool DeleteObject(IntPtr obj);

        internal static IntPtr CreateOverlayWindow(IntPtr owner)
        {
            EnsureClass(); if (_classAtom == 0 || string.IsNullOrEmpty(_className)) return IntPtr.Zero;
            var hwnd = CreateWindowExW(WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE, _className!, "Foresight StreamSafe Overlay", WS_POPUP, 0, 0, 1, 1, owner, IntPtr.Zero, Marshal.GetHINSTANCE(typeof(StreamSafeOverlay).Module), IntPtr.Zero);
            if (hwnd == IntPtr.Zero) return IntPtr.Zero;
            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
            try
            {
                var ptr = Marshal.AllocHGlobal(sizeof(int));
                try
                {
                    Marshal.WriteInt32(ptr, 1);
                    var data = new WINDOWCOMPOSITIONATTRIBDATA { Attrib = WCA_EXCLUDED_FROM_DDA, pvData = ptr, cbData = sizeof(int) };
                    SetWindowCompositionAttribute(hwnd, ref data);
                }
                finally { Marshal.FreeHGlobal(ptr); }
            }
            catch { }
            return hwnd;
        }

        internal static bool TryGetGameClient(out IntPtr gameWindow, out POINT origin, out int width, out int height)
        {
            gameWindow = Process.GetCurrentProcess().MainWindowHandle; origin = default; width = 0; height = 0;
            if (gameWindow == IntPtr.Zero || !GetClientRect(gameWindow, out var rect)) return false;
            origin = new POINT { X = 0, Y = 0 }; if (!ClientToScreen(gameWindow, ref origin)) return false;
            width = Math.Max(0, rect.Right - rect.Left); height = Math.Max(0, rect.Bottom - rect.Top); return width > 0 && height > 0;
        }
        internal static bool IsGameForeground(IntPtr gameWindow)
        {
            var foreground = GetForegroundWindow(); if (foreground == gameWindow) return true; if (foreground == IntPtr.Zero) return false;
            GetWindowThreadProcessId(foreground, out var foregroundPid); return foregroundPid == (uint)Process.GetCurrentProcess().Id;
        }
        internal static void UpdateLayered(IntPtr hwnd, IntPtr memoryDc, int x, int y, int width, int height)
        {
            var dst = new POINT { X = x, Y = y }; var src = new POINT { X = 0, Y = 0 }; var size = new SIZE { cx = width, cy = height }; var blend = new BLENDFUNCTION { BlendOp = AC_SRC_OVER, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = AC_SRC_ALPHA };
            UpdateLayeredWindow(hwnd, IntPtr.Zero, ref dst, ref size, memoryDc, ref src, 0, ref blend, ULW_ALPHA);
            SetWindowPos(hwnd, HWND_TOPMOST, x, y, width, height, SWP_NOACTIVATE | SWP_SHOWWINDOW); ShowWindow(hwnd, SW_SHOWNOACTIVATE);
        }
        private static void EnsureClass()
        {
            if (_classAtom != 0) return;
            _className = "ForesightStreamSafe_" + Process.GetCurrentProcess().Id;
            var wc = new WNDCLASSEX { cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)), lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProc), hInstance = Marshal.GetHINSTANCE(typeof(StreamSafeOverlay).Module), lpszClassName = _className };
            _classAtom = RegisterClassExW(ref wc);
        }
        private static IntPtr WindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const uint WM_NCHITTEST = 0x0084; const uint WM_MOUSEACTIVATE = 0x0021; const int HTTRANSPARENT = -1; const int MA_NOACTIVATE = 3;
            if (msg == WM_NCHITTEST) return new IntPtr(HTTRANSPARENT); if (msg == WM_MOUSEACTIVATE) return new IntPtr(MA_NOACTIVATE); return DefWindowProcW(hwnd, msg, wParam, lParam);
        }
    }
}
