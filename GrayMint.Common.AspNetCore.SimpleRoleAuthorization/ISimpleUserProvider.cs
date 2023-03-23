using System.Security.Claims;

namespace GrayMint.Common.AspNetCore.SimpleRoleAuthorization;

public interface ISimpleUserProvider
{
    Task<SimpleUser?> FindSimpleUser(ClaimsPrincipal claimsPrincipal);
}
