using System.Net;
using System.Text.Json;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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
            var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault()
                                ?? context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                                ?? "N/A";

            _logger.LogError(
                ex,
                "[{CorrelationId}] Ngoại lệ chưa được xử lý tại [{Method}] {Path}: {Message}",
                correlationId,
                context.Request.Method,
                context.Request.Path,
                ex.Message
            );

            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new ApiResponse<object>
        {
            Success = false
        };

        int statusCode;

        switch (exception)
        {
            case NotFoundException notFoundEx:
                statusCode = StatusCodes.Status404NotFound;
                response.Message = notFoundEx.Message;
                break;

            case UnauthorizedException unauthEx:
                statusCode = StatusCodes.Status401Unauthorized;
                response.Message = unauthEx.Message;
                break;

            case ForbiddenException forbiddenEx:
                statusCode = StatusCodes.Status403Forbidden;
                response.Message = forbiddenEx.Message;
                break;

            case BadRequestException badReqEx:
                statusCode = StatusCodes.Status400BadRequest;
                response.Message = badReqEx.Message;
                break;

            case AppException appEx:
                statusCode = appEx.StatusCode;
                response.Message = appEx.Message;
                break;

            case KeyNotFoundException keyEx:
                statusCode = StatusCodes.Status404NotFound;
                response.Message = string.IsNullOrWhiteSpace(keyEx.Message) ? "Không tìm thấy tài nguyên yêu cầu." : keyEx.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Yêu cầu không được xác thực hoặc token không hợp lệ.";
                break;

            case ArgumentException or InvalidOperationException:
                statusCode = StatusCodes.Status400BadRequest;
                response.Message = exception.Message;
                break;

            case DbUpdateException dbEx:
                statusCode = StatusCodes.Status409Conflict;
                response.Message = "Xảy ra xung đột dữ liệu cơ sở dữ liệu (ràng buộc khóa chính/ngoại hoặc dữ liệu trùng lặp).";
                response.Errors = new List<string> { dbEx.InnerException?.Message ?? dbEx.Message };
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                response.Message = "Lỗi máy chủ nội bộ. Vui lòng liên hệ quản trị viên hoặc kiểm tra mã tương quan (Correlation ID).";
                response.Errors = new List<string> { exception.Message };
                break;
        }

        context.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
