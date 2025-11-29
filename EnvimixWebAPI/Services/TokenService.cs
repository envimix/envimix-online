using EnvimixWebAPI.Options;
using EnvimixWebAPI.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EnvimixWebAPI.Services;

public interface ITokenService
{
    string GenerateEnvimaniaSessionToken(Guid sessionGuid, string mapUid, string serverLogin, out DateTimeOffset startedAt, out DateTimeOffset expiresAt);
    string GenerateManiaPlanetUserAccessToken(string login, bool isAdmin, out Guid tokenId);
}

public sealed class TokenService(IOptionsSnapshot<JwtOptions> jwtOptions) : ITokenService
{
    public string GenerateEnvimaniaSessionToken(Guid sessionGuid, string mapUid, string serverLogin, out DateTimeOffset startedAt, out DateTimeOffset expiresAt)
    {
        var tokenDescriptor = GetDescriptor(Consts.EnvimaniaSession, [
            new Claim(JwtRegisteredClaimNames.UniqueName, serverLogin),
            new Claim(EnvimaniaClaimTypes.SessionGuid, sessionGuid.ToString()),
            new Claim(EnvimaniaClaimTypes.SessionMapUid, mapUid)
        ], validFor: TimeSpan.FromMinutes(30));

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        startedAt = securityToken.ValidFrom;
        expiresAt = securityToken.ValidTo;

        return tokenHandler.WriteToken(securityToken);
    }

    public string GenerateManiaPlanetUserAccessToken(string login, bool isAdmin, out Guid tokenId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.UniqueName, login),
            new(JwtRegisteredClaimNames.Jti, (tokenId = Guid.NewGuid()).ToString()),
            new(ClaimTypes.Role, Roles.User)
        };

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, Roles.Admin));
        }

        if (login == "bigbang1112")
        {
            claims.Add(new Claim(ClaimTypes.Role, Roles.SuperAdmin));
        }

        // This is not a standard practice
        // however, that way I don't need to handle refresh token complexity
        // and instead I can invalidate tokens on closed or new game sessions
        // fucking ManiaPlanet doesn't need a refresh token overkill xdd
        var tokenDescriptor = GetDescriptor(Consts.ManiaPlanetUser, claims, validFor: TimeSpan.FromDays(3));

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }

    private SecurityTokenDescriptor GetDescriptor(string audience, IEnumerable<Claim> claims, TimeSpan validFor)
    {
        return new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Value.Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = GetSigningCredentials(),
            Expires = DateTime.UtcNow.Add(validFor)
        };
    }

    private SigningCredentials GetSigningCredentials()
    {
        return new SigningCredentials(
            new SymmetricSecurityKey(Convert.FromHexString(jwtOptions.Value.Key)),
            SecurityAlgorithms.HmacSha256Signature);
    }
}
