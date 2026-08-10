namespace Audsplit.Interop;

/// <summary>
/// Abstraction over the undocumented AudioPolicyConfig WinRT factory.
/// </summary>
public interface IAudioPolicyConfigFactory
{
    HRESULT SetPersistedDefaultAudioEndpoint(uint processId, EDataFlow flow, ERole role, IntPtr deviceId);
    HRESULT GetPersistedDefaultAudioEndpoint(uint processId, EDataFlow flow, ERole role, out string? deviceId);
    HRESULT ClearAllPersistedApplicationDefaultEndpoints();
}

internal static class AudioPolicyConfigFactory
{
    public static IAudioPolicyConfigFactory Create() => new AudioPolicyConfig();
}
