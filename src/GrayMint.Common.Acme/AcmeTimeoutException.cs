namespace GrayMint.Common.Acme;

/// <summary>
/// The authority did not decide on the challenge in time. It may still decide, and even sign: the order's outcome is not
/// known.
/// </summary>
public class AcmeTimeoutException(string message, Exception? innerException = null)
    : AcmeException(message, innerException);
