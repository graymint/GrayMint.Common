namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public interface ISimpleUserProvider
{
    Task<SimpleUser?> FindSimpleUserByEmail(string email);
}