namespace GrayMint.Common.Exceptions;

public class AssertException : Exception
{
    public AssertException(string? message = null)
        : base(message)
    {
    }

    public AssertException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}