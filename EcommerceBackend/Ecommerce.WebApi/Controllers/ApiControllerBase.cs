using System.Security.Claims;
using Ecommerce.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
            {
                throw new UnauthorizedException("User is not authenticated or user ID claim is missing.");
            }

            return userId;
        }
    }

    protected string CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    protected bool IsAdmin => CurrentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    protected bool IsStaff => CurrentUserRole.Equals("Staff", StringComparison.OrdinalIgnoreCase);

    protected bool IsAdminOrStaff => IsAdmin || IsStaff;
}
