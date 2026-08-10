using Audsplit.Interop;
using Audsplit.Models;

namespace Audsplit.Services;

public sealed class RoutingService
{
    private const string MmDevApiToken = @"\\?\SWD#MMDEVAPI#";
    private const string RenderInterface = "#{e6327cad-dcec-4949-ae8a-991e976a79d2}";
    private const string CaptureInterface = "#{2eef81be-33fa-4800-9670-1cd474972c3f}";

    private readonly IAudioPolicyConfigFactory _policy;
    private static readonly ERole[] AllRoles = [ERole.eConsole, ERole.eMultimedia, ERole.eCommunications];

    public RoutingService()
    {
        _policy = AudioPolicyConfigFactory.Create();
    }

    public bool IsAvailable => _policy is not null;

    public bool SetOutputDevice(uint processId, string? deviceId)
    {
        try
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                foreach (var role in AllRoles)
                {
                    var hr = _policy.SetPersistedDefaultAudioEndpoint(processId, EDataFlow.eRender, role, IntPtr.Zero);
                    if (hr != HRESULT.S_OK && hr != HRESULT.PROCESS_NO_AUDIO)
                    {
                        return false;
                    }
                }

                return true;
            }

            var wrapped = GenerateDeviceId(deviceId, EDataFlow.eRender);
            var hstring = Combase.CreateHString(wrapped);
            try
            {
                foreach (var role in AllRoles)
                {
                    var hr = _policy.SetPersistedDefaultAudioEndpoint(processId, EDataFlow.eRender, role, hstring);
                    if (hr != HRESULT.S_OK && hr != HRESULT.PROCESS_NO_AUDIO)
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                Combase.WindowsDeleteString(hstring);
            }
        }
        catch
        {
            return false;
        }
    }

    public string? GetPersistedOutputDevice(uint processId)
    {
        try
        {
            var hr = _policy.GetPersistedDefaultAudioEndpoint(
                processId, EDataFlow.eRender, ERole.eMultimedia, out var deviceId);

            if (hr != HRESULT.S_OK || string.IsNullOrEmpty(deviceId))
            {
                return null;
            }

            return UnpackDeviceId(deviceId);
        }
        catch
        {
            return null;
        }
    }

    public bool ClearAllRoutes()
    {
        try
        {
            return _policy.ClearAllPersistedApplicationDefaultEndpoints() == HRESULT.S_OK;
        }
        catch
        {
            return false;
        }
    }

    private static string GenerateDeviceId(string deviceId, EDataFlow flow)
    {
        if (deviceId.StartsWith(MmDevApiToken, StringComparison.OrdinalIgnoreCase))
        {
            return deviceId;
        }

        var suffix = flow == EDataFlow.eRender ? RenderInterface : CaptureInterface;
        return $"{MmDevApiToken}{deviceId}{suffix}";
    }

    private static string? UnpackDeviceId(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            return null;
        }

        var id = deviceId;
        if (id.StartsWith(MmDevApiToken, StringComparison.OrdinalIgnoreCase))
        {
            id = id[MmDevApiToken.Length..];
        }

        if (id.EndsWith(RenderInterface, StringComparison.OrdinalIgnoreCase))
        {
            id = id[..^RenderInterface.Length];
        }
        else if (id.EndsWith(CaptureInterface, StringComparison.OrdinalIgnoreCase))
        {
            id = id[..^CaptureInterface.Length];
        }

        return id;
    }
}
