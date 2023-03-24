namespace GrayMint.Common.AspNetCore.SimpleUserManagement;

public class SimpleUserOptions
{
    public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromMinutes(10);
}