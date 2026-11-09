using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface IJwtTokenGenerator
{
    (string Token, DateTime Expiration) GenerateToken(User user);
}
