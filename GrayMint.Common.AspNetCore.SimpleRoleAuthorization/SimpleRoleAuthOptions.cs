namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public class SimpleRoleAuthOptions
{
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public void Validate()
    {
    }
}