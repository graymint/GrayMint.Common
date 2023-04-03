namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRoleResource
{
    public string Resource { get; }

    public SimpleRoleResource(string? resource)
    {
        Resource = resource ?? "*";
    }
}