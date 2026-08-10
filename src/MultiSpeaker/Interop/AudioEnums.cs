namespace MultiSpeaker.Interop;

/// <summary>
/// Matches Windows Core Audio EDataFlow.
/// </summary>
public enum EDataFlow
{
    eRender = 0,
    eCapture = 1,
    eAll = 2,
}

/// <summary>
/// Matches Windows Core Audio ERole.
/// </summary>
public enum ERole
{
    eConsole = 0,
    eMultimedia = 1,
    eCommunications = 2,
}

/// <summary>
/// HRESULT values commonly returned by AudioPolicyConfig.
/// </summary>
public enum HRESULT : uint
{
    S_OK = 0,
    PROCESS_NO_AUDIO = 0x80240401,
}
