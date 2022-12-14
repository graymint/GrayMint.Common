namespace GrayMint.Common.AspNetCore;

public class RegisterServicesOptions
{
    public bool AddCors { get; set; } = true;
    public bool AddControllers { get; set; } = true;
    public bool AddSwagger { get; set; } = true;
    public bool AddSwaggerVersioning { get; set; } = true;
    public bool AddMemoryCache { get; set; } = true;
    public bool AddHttpClient { get; set; } = true;
}