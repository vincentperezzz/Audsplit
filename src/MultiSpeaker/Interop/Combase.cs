using System.Runtime.InteropServices;

namespace MultiSpeaker.Interop;

internal static class Combase
{
    [DllImport("combase.dll")]
    public static extern int WindowsCreateString(
        [MarshalAs(UnmanagedType.LPWStr)] string src,
        uint length,
        out IntPtr hstring);

    [DllImport("combase.dll")]
    public static extern int WindowsDeleteString(IntPtr hstring);

    [DllImport("combase.dll", EntryPoint = "WindowsGetStringRawBuffer")]
    public static extern IntPtr WindowsGetStringRawBuffer(IntPtr hstring, out uint length);

    [DllImport("AudioSes.dll", CallingConvention = CallingConvention.StdCall, PreserveSig = false)]
    public static extern void DllGetActivationFactory(IntPtr activatableClassId, out IntPtr factory);

    public static IntPtr CreateHString(string value)
    {
        var hr = WindowsCreateString(value, (uint)value.Length, out var hstring);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return hstring;
    }

    public static string? HStringToString(IntPtr hstring)
    {
        if (hstring == IntPtr.Zero)
        {
            return null;
        }

        var ptr = WindowsGetStringRawBuffer(hstring, out var len);
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringUni(ptr, (int)len);
    }
}
