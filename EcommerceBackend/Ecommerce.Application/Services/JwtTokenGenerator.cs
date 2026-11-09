using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Application.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime Expiration) GenerateToken(User user)
    {
        var secret = _configuration["JwtSettings:Secret"] ?? "SuperSecretKeyForJwtAuthenticationWithMinimum32Characters!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "EcommerceApi";
        var audience = _configuration["JwtSettings:Audience"] ?? "EcommerceClient";
        var expiryInHours = double.TryParse(_configuration["JwtSettings:ExpiryInHours"], out var hours) ? hours : 24;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.Name)
        };

        var expiration = DateTime.UtcNow.AddHours(expiryInHours);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), expiration);
    }
}
