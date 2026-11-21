namespace GrayMint.Common.Acme;

/// <summary>An order that did not end in a certificate; the message says why, in words a user can act on.</summary>
public class AcmeException(string message, Exception? innerException = null) : Exception(message, innerException);

/// <summary>
/// The authority did not decide on the challenge in time. It may still decide, and even sign: the order's outcome is not
/// known.
/// </summary>
public class AcmeTimeoutException(string message, Exception? innerException = null)
    : AcmeException(message, innerException);

public enum AcmeSelfCheckError
{
    /// <summary>The domain has no address the check may connect to.</summary>
    NoAddress,

    /// <summary>Nothing answered on port 80 of the domain's address.</summary>
    NotReached,

    /// <summary>Something answered on port 80, but not with the challenge's key authorization.</summary>
    OtherAnswer
}

/// <summary>Our own check of the challenge's URL failed, so the order was not sent to the authority to check.</summary>
public class AcmeSelfCheckException(AcmeSelfCheckError error, string message, Exception? innerException = null)
    : AcmeException(message, innerException)
{
    public AcmeSelfCheckError Error { get; } = error;
}
