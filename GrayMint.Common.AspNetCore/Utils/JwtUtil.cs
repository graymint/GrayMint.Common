using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace GrayMint.Common.AspNetCore.Utils;

public class JwtUtil
{

    public static string CreateAccessToken(byte[] key, string authIssuer, string authCode, string id, string email, string[]? roles)
    {
        return JwtUtil.CreateSymmetricJwt(key, authIssuer,
            authIssuer,
            Guid.NewGuid().ToString(),
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id),
                new Claim(ClaimTypes.Email, email),
                new Claim("AuthCode", authCode),
            }, 
            roles
        );
    }

    public static string CreateSymmetricJwt(byte[] secret, string issuer, string audience, string subject,
        Claim[]? claims = null, string[]? roles = null)
    {
        var claimsList = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        if (claims != null) claimsList.AddRange(claims);
        if (roles != null) claimsList.AddRange(roles.Select(x=>new Claim(ClaimTypes.Role, x)));

        // create token
        var secKey = new SymmetricSecurityKey(secret);
        var signingCredentials = new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer,
            claims: claimsList.ToArray(),
            audience: audience,
            expires: DateTime.Now.AddYears(10),
            signingCredentials: signingCredentials);

        var handler = new JwtSecurityTokenHandler();
        var ret = handler.WriteToken(token);
        return ret;
    }
}