using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Audsplit.Models;
using NAudio.CoreAudioApi;

namespace Audsplit.Services;

public sealed class SessionService : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly RoutingService _routing;

    public SessionService(RoutingService routing)
    {
        _routing = routing;
    }

    public IReadOnlyList<AppSessionInfo> GetAppSessions()
    {
        var byPid = new Dictionary<uint, AppSessionInfo>();

        var devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        foreach (var device in devices)
        {
            AudioSessionManager sessionManager;
            try
            {
                sessionManager = device.AudioSessionManager;
            }
            catch
            {
                continue;
            }

            if (sessionManager?.Sessions is null)
            {
                continue;
            }

            var sessions = sessionManager.Sessions;
            for (var i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                uint pid;
                try
                {
                    pid = session.GetProcessID;
                }
                catch
                {
                    continue;
                }

                if (pid == 0)
                {
                    continue; // system sounds
                }

                try
                {
                    _ = session.State;
                }
                catch
                {
                    continue;
                }

                if (byPid.ContainsKey(pid))
                {
                    continue;
                }

                var info = ResolveProcess(pid);
                if (info is null)
                {
                    continue;
                }

                info.CurrentDeviceId = device.ID;
                info.PersistedDeviceId = _routing.GetPersistedOutputDevice(pid);
                byPid[pid] = info;
            }
        }

        return byPid.Values
            .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static AppSessionInfo? ResolveProcess(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            var name = process.ProcessName;
            string? path = null;
            try
            {
                path = process.MainModule?.FileName;
            }
            catch
            {
            }

            var display = !string.IsNullOrWhiteSpace(process.MainWindowTitle)
                ? process.MainWindowTitle
                : Path.GetFileNameWithoutExtension(path ?? name);

            if (string.IsNullOrWhiteSpace(display))
            {
                display = name;
            }

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    var version = FileVersionInfo.GetVersionInfo(path);
                    if (!string.IsNullOrWhiteSpace(version.FileDescription))
                    {
                        display = version.FileDescription;
                    }
                }
                catch
                {
                }
            }

            return new AppSessionInfo
            {
                ProcessId = processId,
                ProcessName = name,
                DisplayName = display,
                ExecutablePath = path,
                Icon = ExtractIcon(path),
            };
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? ExtractIcon(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using var icon = Icon.ExtractAssociatedIcon(path);
            if (icon is null)
            {
                return null;
            }

            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32));
            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() => _enumerator.Dispose();
}
