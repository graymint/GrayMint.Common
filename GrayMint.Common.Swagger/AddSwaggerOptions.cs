namespace GrayMint.Common.Swagger;

public class AddSwaggerOptions
{
    public string? Title { get; init; }
    public bool AddNSwag { get; init; } = true;
    public bool AddOpenApi { get; init; } = true;
    public bool AddScalar { get; init; } = true;
}