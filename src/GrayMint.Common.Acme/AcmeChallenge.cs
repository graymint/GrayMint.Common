namespace GrayMint.Common.Acme;

/// <summary>A challenge's token and what its URL must answer, while the authority checks it.</summary>
public record AcmeChallenge(string Domain, string Token, string KeyAuthorization);
