namespace GrayMint.Common.AspNetCore.SimpleUserControllers;

public class SimpleUserControllerOptions
{
    public bool AllowUserApiKey { get; set; }
    public bool AllowBotAppOwner { get; set; }
    public bool IsTestEnvironment { get; set; }
}