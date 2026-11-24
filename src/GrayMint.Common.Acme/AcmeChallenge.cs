namespace GrayMint.Common.Acme;

/// <summary>A challenge's token and what its URL must answer, while the authority checks it.</summary>
// the app's PublishChallenge keeps it for whatever answers port 80
// ReSharper disable once NotAccessedPositionalProperty.Global
public record AcmeChallenge(string Domain, string Token, string KeyAuthorization);
