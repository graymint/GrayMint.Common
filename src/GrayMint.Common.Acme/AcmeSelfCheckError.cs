namespace GrayMint.Common.Acme;

public enum AcmeSelfCheckError
{
    /// <summary>The domain has no address the check may connect to.</summary>
    NoAddress,

    /// <summary>Nothing answered on port 80 of the domain's address.</summary>
    NotReached,

    /// <summary>Something answered on port 80, but not with the challenge's key authorization.</summary>
    OtherAnswer
}
