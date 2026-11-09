using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var normalizedUsername = request.Username.Trim().ToLower();
        var normalizedEmail = request.Email.Trim().ToLower();

        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername))
        {
            throw new BadRequestException($"Username '{request.Username}' is already taken.");
        }

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
        {
            throw new BadRequestException($"Email '{request.Email}' is already registered.");
        }

        // Determine Role (Default to Customer unless requested and valid)
        var roleName = string.IsNullOrWhiteSpace(request.Role) ? "Customer" : request.Role.Trim();
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
        if (role == null)
        {
            role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (role == null)
            {
                role = new Role { Name = "Customer", Description = "Default customer role" };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }
        }

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            RoleId = role.Id,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var (token, expiration) = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = role.Name
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var identifier = request.UsernameOrEmail.Trim().ToLower();

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == identifier || u.Email.ToLower() == identifier);

        if (user == null)
        {
            throw new UnauthorizedException("Invalid username/email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Your account has been deactivated. Please contact administrator.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid username/email or password.");
        }

        var (token, expiration) = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.Name
        };
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new NotFoundException($"User with ID {userId} was not found.");
        }

        return new CurrentUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.Name,
            CreatedAt = user.CreatedAt
        };
    }
}
