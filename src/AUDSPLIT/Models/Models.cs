using System.Windows.Media;

namespace Audsplit.Models;

public sealed class AudioDeviceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool IsDefault { get; init; }
    public float Volume { get; init; }
    public bool IsMuted { get; init; }

    public override string ToString() => Name;
}

public sealed class AppSessionInfo
{
    public required uint ProcessId { get; init; }
    public required string DisplayName { get; init; }
    public required string ProcessName { get; init; }
    public string? ExecutablePath { get; init; }
    public ImageSource? Icon { get; set; }
    public string? CurrentDeviceId { get; set; }
    public string? PersistedDeviceId { get; set; }
}
