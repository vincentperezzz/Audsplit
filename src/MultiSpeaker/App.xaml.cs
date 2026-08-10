using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using MultiSpeaker.Services;
using MultiSpeaker.UI;

namespace MultiSpeaker;

public partial class App : Application
{
    private TaskbarIcon? _tray;
    private FlyoutWindow? _flyout;
    private DeviceService? _devices;
    private SessionService? _sessions;
    private RoutingService? _routing;
    private string? _tempIconPath;
    private Icon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Any(a => string.Equals(a, "--smoke", StringComparison.OrdinalIgnoreCase)))
        {
            RunSmokeTest();
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _routing = new RoutingService();
        _devices = new DeviceService();
        _sessions = new SessionService(_routing);
        _flyout = new FlyoutWindow(_devices, _sessions, _routing);

        _tempIconPath = TrayIconFactory.CreateTempIcon();
        using (var loaded = new Icon(_tempIconPath))
        {
            _trayIcon = (Icon)loaded.Clone();
        }

        _tray = new TaskbarIcon
        {
            ToolTipText = "AUDSPLIT — audio your way",
            Icon = _trayIcon,
            ContextMenu = BuildContextMenu(),
            MenuActivation = PopupActivationMode.RightClick,
        };
        _tray.TrayLeftMouseUp += (_, _) => ToggleFlyout();
    }

    private static void RunSmokeTest()
    {
        void Log(string msg)
        {
            Console.WriteLine(msg);
            System.Diagnostics.Debug.WriteLine(msg);
        }

        Log("AUDSPLIT smoke test");
        Log($"OS: {Environment.OSVersion}");

        try
        {
            var routing = new RoutingService();
            Log($"RoutingService available: {routing.IsAvailable}");

            using var devices = new DeviceService();
            var outs = devices.GetOutputDevices();
            Log($"Output devices ({outs.Count}):");
            foreach (var d in outs)
            {
                Log($"  - {(d.IsDefault ? "[default] " : "")}{d.Name}");
            }

            using var sessions = new SessionService(routing);
            var apps = sessions.GetAppSessions();
            Log($"Audio sessions ({apps.Count}):");
            foreach (var a in apps)
            {
                Log($"  - {a.DisplayName} (pid={a.ProcessId}, persisted={a.PersistedDeviceId ?? "default"})");
            }

            if (apps.Count > 0 && outs.Count > 1)
            {
                var target = outs.FirstOrDefault(d => !d.IsDefault) ?? outs[0];
                var app = apps[0];
                var before = routing.GetPersistedOutputDevice(app.ProcessId);
                Log($"Round-trip: route pid={app.ProcessId} -> {target.Name}");
                var setOk = routing.SetOutputDevice(app.ProcessId, target.Id);
                var after = routing.GetPersistedOutputDevice(app.ProcessId);
                Log($"  setOk={setOk}, after={after}");
                routing.SetOutputDevice(app.ProcessId, before);
                var restored = routing.GetPersistedOutputDevice(app.ProcessId);
                Log($"  restored={restored ?? "default"}");
                if (!setOk || !string.Equals(after, target.Id, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Routing round-trip failed");
                }
            }

            Log("SMOKE_OK");
        }
        catch (Exception ex)
        {
            Log("SMOKE_FAIL: " + ex);
            Environment.ExitCode = 1;
        }
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        var open = new MenuItem { Header = "Open AUDSPLIT" };
        open.Click += (_, _) => ToggleFlyout();
        menu.Items.Add(open);

        var refresh = new MenuItem { Header = "Refresh" };
        refresh.Click += (_, _) => _flyout?.Refresh();
        menu.Items.Add(refresh);

        menu.Items.Add(new Separator());

        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (_, _) => Shutdown();
        menu.Items.Add(exit);

        return menu;
    }

    private void ToggleFlyout()
    {
        if (_flyout is null)
        {
            return;
        }

        if (_flyout.IsVisible)
        {
            _flyout.Hide();
        }
        else
        {
            _flyout.ShowNearCursor();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _trayIcon?.Dispose();
        _flyout?.Close();
        _sessions?.Dispose();
        _devices?.Dispose();

        if (_tempIconPath is not null)
        {
            try { File.Delete(_tempIconPath); } catch { }
        }

        base.OnExit(e);
    }
}
