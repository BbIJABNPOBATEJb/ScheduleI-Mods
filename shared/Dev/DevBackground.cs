using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace S1Shared.Dev;

/// <summary>
/// Smoke tests that stay out of the player's way (tools/run-smoke.ps1 does this by default):
///   --s1dev-desktops {guid},{guid}...   the game window moves to another virtual desktop
///   --s1dev-background                  no other desktop: the window stays behind everything
///   --s1dev-focus HWND                  the window that had the keyboard before the launch gets it back
///   --s1dev-mute                        no sound
/// Either way the game runs in a small window: it ignores the display settings both games share
/// (fullscreen), and the test window never keeps the keyboard focus.
/// </summary>
internal static class DevBackground
{
    private const int Width = 1280;
    private const int Height = 720;

    private static readonly List<Guid> Desktops = new();
    private static IntPtr _window;
    private static IntPtr _focus;
    private static bool _behind;
    private static bool _mute;
    private static bool _moved;
    private static float _nextCheck;
    private static int _moveAttempts;

    public static bool Active { get; private set; }

    public static void Start(string modName, string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--s1dev-background":
                    _behind = true;
                    break;
                case "--s1dev-mute":
                    _mute = true;
                    break;
                case "--s1dev-focus" when i + 1 < args.Length:
                    if (long.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hwnd))
                        _focus = new IntPtr(hwnd);
                    break;
                case "--s1dev-desktops" when i + 1 < args.Length:
                    foreach (var part in args[++i].Split(','))
                    {
                        if (Guid.TryParse(part.Trim(), out var guid))
                            Desktops.Add(guid);
                    }
                    break;
            }
        }
        Active = _behind || _mute || Desktops.Count > 0 || _focus != IntPtr.Zero;
        if (!Active)
            return;
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
        if (_mute)
            AudioListener.volume = 0f;
        var now = Time.realtimeSinceStartup;
        if (now < _nextCheck)
            return;
        _nextCheck = now + (now < 180f ? 0.25f : 2f);
        if (Screen.fullScreenMode != FullScreenMode.Windowed || Screen.width > Width)
            Screen.SetResolution(Width, Height, FullScreenMode.Windowed);
        if (Application.platform != RuntimePlatform.WindowsPlayer)
            return;
        var ownPid = Process.GetCurrentProcess().Id;
        if (_window == IntPtr.Zero)
            _window = FindGameWindow(ownPid, own: true);
        if (_window == IntPtr.Zero)
            return;

        if (Desktops.Count > 0 && !_moved && _moveAttempts < 120)
        {
            _moveAttempts++;
            _moved = MoveToOtherDesktop(_window);
        }
        if (GetForegroundWindow() == _window)
        {
            // Give the keyboard back: to the window that had it before the launch, else to a game being played.
            var back = _focus != IntPtr.Zero && IsWindow(_focus) ? _focus : FindGameWindow(ownPid, own: false);
            if (back != IntPtr.Zero)
                SetForegroundWindow(back);
        }
        if (_behind || (Desktops.Count > 0 && !_moved))
            SetWindowPos(_window, new IntPtr(1) /* HWND_BOTTOM */, 0, 0, 0, 0, SwpNoSize | SwpNoMove | SwpNoActivate);
    }

    // --- virtual desktops (IVirtualDesktopManager, called through its vtable: works on CoreCLR and Mono) ---

    private static readonly Guid ManagerClass = new("aa509086-5ca9-4c25-8f95-589d3c07b48a");
    private static readonly Guid ManagerInterface = new("a5cd92ff-29be-454c-8d04-d82879fb3f1b");
    private static IntPtr _manager;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDesktopDelegate(IntPtr self, IntPtr window, out Guid desktop);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int MoveDelegate(IntPtr self, IntPtr window, ref Guid desktop);

    /// <summary>To the last desktop that is not the one the window opened on (the player's).</summary>
    private static bool MoveToOtherDesktop(IntPtr window)
    {
        try
        {
            if (_manager == IntPtr.Zero)
            {
                CoInitializeEx(IntPtr.Zero, 2 /* COINIT_APARTMENTTHREADED */);
                var clsid = ManagerClass;
                var iid = ManagerInterface;
                var hr = CoCreateInstance(ref clsid, IntPtr.Zero, 0x17 /* CLSCTX_ALL */, ref iid, out _manager);
                if (hr != 0 || _manager == IntPtr.Zero)
                {
                    MelonLogger.Warning($"Virtual desktops unavailable (0x{hr:X8}): the test window stays behind");
                    Desktops.Clear();
                    _behind = true;
                    return false;
                }
            }
            var vtable = Marshal.ReadIntPtr(_manager);
            var getDesktop = (GetDesktopDelegate)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(vtable, 4 * IntPtr.Size), typeof(GetDesktopDelegate));
            var move = (MoveDelegate)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(vtable, 5 * IntPtr.Size), typeof(MoveDelegate));
            if (getDesktop(_manager, window, out var current) != 0 || current == Guid.Empty)
                return false; // the window is not on a desktop yet
            var target = Guid.Empty;
            foreach (var desktop in Desktops)
            {
                if (desktop != current)
                    target = desktop;
            }
            if (target == Guid.Empty)
            {
                Desktops.Clear();
                _behind = true;
                return false;
            }
            var result = move(_manager, window, ref target);
            if (result == 0)
                MelonLogger.Msg($"Test window moved to virtual desktop {target}");
            return result == 0;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning("Virtual desktops: " + ex.Message);
            Desktops.Clear();
            _behind = true;
            return false;
        }
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

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr reserved, uint coInit);

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint context, ref Guid iid, out IntPtr instance);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

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
