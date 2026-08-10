using MultiSpeaker.Models;
using NAudio.CoreAudioApi;

namespace MultiSpeaker.Services;

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
            list.Add(new AudioDeviceInfo
            {
                Id = device.ID,
                Name = device.FriendlyName,
                IsDefault = string.Equals(device.ID, defaultId, StringComparison.OrdinalIgnoreCase),
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

    public void Dispose() => _enumerator.Dispose();
}
