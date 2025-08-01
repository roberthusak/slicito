using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;

namespace StandaloneGui;

// https://stackoverflow.com/a/65496277/2105235
internal static class Marshal2
{
    internal const string OLEAUT32 = "oleaut32.dll";
    internal const string OLE32 = "ole32.dll";

    [SecurityCritical]  // auto-generated_required
    public static object GetActiveObject(string progID)
    {
        Guid clsid;

        // Call CLSIDFromProgIDEx first then fall back on CLSIDFromProgID if
        // CLSIDFromProgIDEx doesn't exist.
        try
        {
            CLSIDFromProgIDEx(progID, out clsid);
        }
        //            catch
        catch (Exception)
        {
            CLSIDFromProgID(progID, out clsid);
        }

        GetActiveObject(ref clsid, nint.Zero, out var obj);
        return obj;
    }

    [DllImport(OLE32, PreserveSig = false)]
    [ResourceExposure(ResourceScope.None)]
    [SuppressUnmanagedCodeSecurity]
    [SecurityCritical]  // auto-generated
    private static extern void CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] string progId, out Guid clsid);

    [DllImport(OLE32, PreserveSig = false)]
    [ResourceExposure(ResourceScope.None)]
    [SuppressUnmanagedCodeSecurity]
    [SecurityCritical]  // auto-generated
    private static extern void CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string progId, out Guid clsid);

    [DllImport(OLEAUT32, PreserveSig = false)]
    [ResourceExposure(ResourceScope.None)]
    [SuppressUnmanagedCodeSecurity]
    [SecurityCritical]  // auto-generated
    private static extern void GetActiveObject(ref Guid rclsid, nint reserved, [MarshalAs(UnmanagedType.Interface)] out object ppunk);

}
