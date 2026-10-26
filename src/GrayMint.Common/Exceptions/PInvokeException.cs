using System.Runtime.InteropServices;

namespace GrayMint.Common.Exceptions;

public class PInvokeException : ExternalException
{
    public PInvokeException(string message, int errorCode)
        : base(message, errorCode)
    {
    }

    public PInvokeException(string message)
        : base(message, Marshal.GetLastWin32Error())
    {
    }
}