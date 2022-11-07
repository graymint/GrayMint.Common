namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public interface ISimpleRoleProvider
{
    Task<SimpleUser?> GetSimpleUserByEmail(string email);
}