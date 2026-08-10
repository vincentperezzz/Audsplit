using System.Runtime.InteropServices;

namespace MultiSpeaker.Interop;

/// <summary>
/// Version-resilient wrapper around the undocumented AudioPolicyConfig COM object.
/// Discovers the interface IID at runtime (IIDs change across Windows builds) and
/// invokes Set/Get/Clear via vtable slots — same approach as SoundSwitch / EarTrumpet.
/// </summary>
internal sealed class AudioPolicyConfig : IAudioPolicyConfigFactory, IDisposable
{
    private const string ActivatableClassId = "Windows.Media.Internal.AudioPolicyConfig";
    private const int GuidSize = 16;

    // Known IIDs observed across Windows 10/11 builds (used for validation, not required).
    private static readonly HashSet<string> KnownGuids = new(StringComparer.OrdinalIgnoreCase)
    {
        "2a59116d-6c4f-45e0-a74f-707e3fef9258", // downlevel
        "ab3d4648-e242-459f-b02f-541c70306324", // 21H2 / Win11
        "32aa8e18-6496-4e24-9f94-b800e7eccc45", // older Win10
    };

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int QueryInterfaceDelegate(IntPtr thisPtr, IntPtr guid, ref IntPtr result);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetIidsDelegate(IntPtr thisPtr, out int iidCount, out IntPtr iids);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetPersistedDefaultAudioEndpointDelegate(
        IntPtr thisPtr, uint processId, EDataFlow flow, ERole role, IntPtr deviceId);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetPersistedDefaultAudioEndpointDelegate(
        IntPtr thisPtr, uint processId, EDataFlow flow, ERole role, out IntPtr deviceId);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int ClearAllPersistedApplicationDefaultEndpointsDelegate(IntPtr thisPtr);

    private readonly IntPtr _instance;
    private readonly IntPtr _vtable;
    private readonly int _ptrSize;
    private bool _disposed;

    public AudioPolicyConfig()
    {
        _ptrSize = IntPtr.Size;

        var name = Combase.CreateHString(ActivatableClassId);
        try
        {
            Combase.DllGetActivationFactory(name, out _instance);
        }
        finally
        {
            Combase.WindowsDeleteString(name);
        }

        if (_instance == IntPtr.Zero)
        {
            throw new InvalidComObjectException("DllGetActivationFactory returned null.");
        }

        // IInspectable::GetIids — vtable slot 3
        var inspectableVtable = Marshal.ReadIntPtr(_instance);
        var getIidsPtr = Marshal.ReadIntPtr(inspectableVtable, _ptrSize * 3);
        var getIids = Marshal.GetDelegateForFunctionPointer<GetIidsDelegate>(getIidsPtr);

        var hr = getIids(_instance, out var iidCount, out var iids);
        if (hr < 0 || iidCount <= 0 || iids == IntPtr.Zero)
        {
            throw new InvalidComObjectException($"GetIids failed: 0x{hr:X8}, count={iidCount}");
        }

        try
        {
            // The AudioPolicyConfig custom interface is typically the last IID.
            var audioPolicyGuidPtr = IntPtr.Add(iids, GuidSize * (iidCount - 1));
            var guidText = GuidPtrToString(audioPolicyGuidPtr);

            // Prefer a known IID if present; otherwise use the last one.
            IntPtr? preferred = null;
            for (var i = 0; i < iidCount; i++)
            {
                var p = IntPtr.Add(iids, GuidSize * i);
                var g = GuidPtrToString(p);
                if (KnownGuids.Contains(g))
                {
                    preferred = p;
                    guidText = g;
                    break;
                }
            }

            preferred ??= audioPolicyGuidPtr;

            var qiPtr = Marshal.ReadIntPtr(inspectableVtable, 0);
            var qi = Marshal.GetDelegateForFunctionPointer<QueryInterfaceDelegate>(qiPtr);
            var activationFactory = _instance;
            var obj = IntPtr.Zero;
            hr = qi(activationFactory, preferred.Value, ref obj);
            if (hr < 0 || obj == IntPtr.Zero)
            {
                throw new InvalidComObjectException(
                    $"QueryInterface({guidText}) failed: 0x{hr:X8}");
            }

            // Release the activation factory; keep the QI'd interface.
            Marshal.Release(activationFactory);
            _instance = obj;
            _vtable = Marshal.ReadIntPtr(_instance);
        }
        finally
        {
            Marshal.FreeCoTaskMem(iids);
        }
    }

    public HRESULT SetPersistedDefaultAudioEndpoint(uint processId, EDataFlow flow, ERole role, IntPtr deviceId)
    {
        // IInspectable (3) + 19 stubs + Set = slot 25 (0-based from start of object vtable including IUnknown/IInspectable)
        // SoundSwitch uses _ptrSize * 25 relative to the QI'd interface vtable.
        var fnPtr = Marshal.ReadIntPtr(_vtable, _ptrSize * 25);
        var fn = Marshal.GetDelegateForFunctionPointer<SetPersistedDefaultAudioEndpointDelegate>(fnPtr);
        return (HRESULT)(uint)fn(_instance, processId, flow, role, deviceId);
    }

    public HRESULT GetPersistedDefaultAudioEndpoint(uint processId, EDataFlow flow, ERole role, out string? deviceId)
    {
        var fnPtr = Marshal.ReadIntPtr(_vtable, _ptrSize * 26);
        var fn = Marshal.GetDelegateForFunctionPointer<GetPersistedDefaultAudioEndpointDelegate>(fnPtr);
        var hr = fn(_instance, processId, flow, role, out var hstr);
        try
        {
            if ((HRESULT)(uint)hr == HRESULT.S_OK)
            {
                deviceId = Combase.HStringToString(hstr);
            }
            else
            {
                deviceId = null;
            }

            return (HRESULT)(uint)hr;
        }
        finally
        {
            if (hstr != IntPtr.Zero)
            {
                Combase.WindowsDeleteString(hstr);
            }
        }
    }

    public HRESULT ClearAllPersistedApplicationDefaultEndpoints()
    {
        var fnPtr = Marshal.ReadIntPtr(_vtable, _ptrSize * 27);
        var fn = Marshal.GetDelegateForFunctionPointer<ClearAllPersistedApplicationDefaultEndpointsDelegate>(fnPtr);
        return (HRESULT)(uint)fn(_instance);
    }

    private static string GuidPtrToString(IntPtr guidPtr)
    {
        var bytes = new byte[GuidSize];
        Marshal.Copy(guidPtr, bytes, 0, GuidSize);
        return new Guid(bytes).ToString();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_instance != IntPtr.Zero)
        {
            Marshal.Release(_instance);
        }
    }
}
