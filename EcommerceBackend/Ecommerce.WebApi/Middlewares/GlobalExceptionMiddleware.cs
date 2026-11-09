using System.Net;
using System.Text.Json;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Models;

namespace Ecommerce.WebApi.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred during request execution: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>();

        switch (exception)
        {
            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Success = false;
                response.Message = notFoundEx.Message;
                break;

            case UnauthorizedException unauthEx:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Success = false;
                response.Message = unauthEx.Message;
                break;

            case ForbiddenException forbiddenEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Success = false;
                response.Message = forbiddenEx.Message;
                break;

            case BadRequestException badReqEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Success = false;
                response.Message = badReqEx.Message;
                break;

            case AppException appEx:
                context.Response.StatusCode = appEx.StatusCode;
                response.Success = false;
                response.Message = appEx.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Success = false;
                response.Message = "An internal server error occurred. Please contact system administrator.";
                response.Errors = new List<string> { exception.Message };
                break;
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, jsonOptions);

        await context.Response.WriteAsync(json);
    }
}
