using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AccountService.Application.Interfaces;
using AccountService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AccountService.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured.");
        _issuer = configuration["Jwt:Issuer"] ?? "MyBankAI";
        _audience = configuration["Jwt:Audience"] ?? "MyBankAI.Clients";
        _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60;
    }

    public (string token, DateTime expiresAt) GenerateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
