using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace S1Shared.Dev;

/// <summary>
/// Smoke tests running next to someone playing (--s1dev-background, tools/run-smoke.ps1 -Parallel):
/// the test game stays a small silent window behind the player's game. It ignores the display
/// settings both games share (fullscreen), never keeps the keyboard focus, and plays no sound.
/// </summary>
internal static class DevBackground
{
    private const int Width = 1280;
    private const int Height = 720;

    private static IntPtr _window;
    private static float _nextCheck;

    public static bool Active { get; private set; }

    public static void Start(string modName)
    {
        Active = true;
        try
        {
            var harmony = new HarmonyLib.Harmony("S1Shared.Dev.Background." + modName);
            var apply = AccessTools.Method(typeof(S1.DevUtilities.Settings), "ApplyDisplaySettings");
            harmony.Patch(apply, prefix: new HarmonyMethod(typeof(DevBackground), nameof(DisplayPrefix)));
        }
        catch (Exception ex)
        {
            MelonLogger.Warning("Background mode: display settings are not overridden: " + ex.Message);
        }
    }

    /// <summary>The game applies the player's display settings (stored for both games): use a small window instead.</summary>
    private static void DisplayPrefix(ref S1.DevUtilities.DisplaySettings settings)
    {
        settings.DisplayMode = S1.DevUtilities.DisplaySettings.EDisplayMode.Windowed;
        settings.ActiveDisplayIndex = 0;
        var resolutions = UnityQuery.ToManaged(S1.DevUtilities.DisplaySettings.GetResolutions());
        var index = resolutions.FindIndex(r => r.width == Width && r.height == Height);
        if (index >= 0)
            settings.ResolutionIndex = index;
    }

    /// <summary>Every frame from DevSmoke.CheckTimeout.</summary>
    public static void Tick()
    {
        AudioListener.volume = 0f;
        var now = Time.realtimeSinceStartup;
        if (now < _nextCheck)
            return;
        _nextCheck = now + (now < 180f ? 0.25f : 2f);
        if (Screen.fullScreenMode != FullScreenMode.Windowed || Screen.width > Width)
            Screen.SetResolution(Width, Height, FullScreenMode.Windowed);
        if (Application.platform != RuntimePlatform.WindowsPlayer)
            return;
        if (_window == IntPtr.Zero)
            _window = FindGameWindow(Process.GetCurrentProcess().Id, own: true);
        if (_window == IntPtr.Zero)
            return;
        if (GetForegroundWindow() == _window)
        {
            // Give the keyboard back to the player's game.
            var other = FindGameWindow(Process.GetCurrentProcess().Id, own: false);
            if (other != IntPtr.Zero)
                SetForegroundWindow(other);
        }
        SetWindowPos(_window, new IntPtr(1) /* HWND_BOTTOM */, 0, 0, 0, 0, SwpNoSize | SwpNoMove | SwpNoActivate);
    }

    /// <summary>The Unity window of this process (own) or of another Schedule I process.</summary>
    private static IntPtr FindGameWindow(int ownPid, bool own)
    {
        var found = IntPtr.Zero;
        var name = new StringBuilder(64);
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;
            GetWindowThreadProcessId(hWnd, out var pid);
            if (((int)pid == ownPid) != own)
                return true;
            name.Length = 0;
            GetClassName(hWnd, name, name.Capacity);
            if (name.ToString() != "UnityWndClass")
                return true;
            if (!own && !IsGame((int)pid))
                return true;
            found = hWnd;
            return false;
        }, IntPtr.Zero);
        return found;
    }

    private static bool IsGame(int pid)
    {
        try
        {
            return Process.GetProcessById(pid).ProcessName == "Schedule I";
        }
        catch (Exception)
        {
            return false;
        }
    }

    private const uint SwpNoSize = 0x0001, SwpNoMove = 0x0002, SwpNoActivate = 0x0010;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder name, int capacity);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
}
