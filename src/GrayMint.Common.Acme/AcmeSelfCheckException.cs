namespace GrayMint.Common.Acme;

/// <summary>Our own check of the challenge's URL failed, so the order was not sent to the authority to check.</summary>
public class AcmeSelfCheckException(AcmeSelfCheckError error, string message, Exception? innerException = null)
    : AcmeException(message, innerException)
{
    public AcmeSelfCheckError Error { get; } = error;
}
