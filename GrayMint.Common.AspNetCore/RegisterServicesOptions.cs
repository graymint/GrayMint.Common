namespace GrayMint.Common.AspNetCore;

public class RegisterServicesOptions
{
    public bool AddCors { get; set; } = true;
    public bool AddControllers { get; set; } = true;
    public bool AddSwagger { get; set; } = true;
    public bool AddBotAuthentication { get; set; } = true;
    public bool AddCognitoAuthentication { get; set; } = false;
    public bool AddMemoryCache { get; set; } = true;
    public bool AddHttpClient { get; set; } = true;
}