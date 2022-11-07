namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public interface ISimpleRoleProvider
{
    Task<SimpleUser?> FindSimpleUserByEmail(string email);
}