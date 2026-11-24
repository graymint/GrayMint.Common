namespace GrayMint.Common.Acme;

/// <summary>An order that did not end in a certificate; the message says why, in words a user can act on.</summary>
public class AcmeException(string message, Exception? innerException = null) : Exception(message, innerException);
