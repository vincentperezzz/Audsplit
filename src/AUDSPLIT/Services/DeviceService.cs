using Audsplit.Models;
using NAudio.CoreAudioApi;

namespace Audsplit.Services;

public sealed class DeviceService : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();

    public IReadOnlyList<AudioDeviceInfo> GetOutputDevices()
    {
        var defaultId = GetDefaultOutputDeviceId();
        var devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        var list = new List<AudioDeviceInfo>(devices.Count);

        foreach (var device in devices)
        {
            float volume = 0f;
            var muted = false;
            try
            {
                volume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
                muted = device.AudioEndpointVolume.Mute;
            }
            catch
            {
            }

            list.Add(new AudioDeviceInfo
            {
                Id = device.ID,
                Name = device.FriendlyName,
                IsDefault = string.Equals(device.ID, defaultId, StringComparison.OrdinalIgnoreCase),
                Volume = volume,
                IsMuted = muted,
            });
        }

        return list
            .OrderByDescending(d => d.IsDefault)
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string? GetDefaultOutputDeviceId()
    {
        try
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return device.ID;
        }
        catch
        {
            return null;
        }
    }

    public bool SetVolume(string deviceId, float level)
    {
        try
        {
            using var device = _enumerator.GetDevice(deviceId);
            device.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(level, 0f, 1f);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool SetMute(string deviceId, bool muted)
    {
        try
        {
            using var device = _enumerator.GetDevice(deviceId);
            device.AudioEndpointVolume.Mute = muted;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose() => _enumerator.Dispose();
}
