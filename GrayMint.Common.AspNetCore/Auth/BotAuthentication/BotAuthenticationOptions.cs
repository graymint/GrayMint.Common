namespace GrayMint.Common.AspNetCore.Auth.BotAuthentication;

public class BotAuthenticationOptions
{
    public string BotIssuer { get; set; } = default!;
    public byte[] BotKey { get; set; } = default!;
}