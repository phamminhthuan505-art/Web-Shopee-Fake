using System.Text;
using Ecommerce.Application;
using Ecommerce.Application.Common.Models;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Data;
using Ecommerce.WebApi.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Configuration (Console + Daily Rolling File in logs/ with CorrelationId enrichment)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/ecommerce-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Register Application & Infrastructure Services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 3. Configure JWT Bearer Authentication & RBAC Authorization
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "Antigravity_Advanced_Secret_Key_For_JWT_Token_Generation_Min_256_Bits!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "EcommerceApi";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "EcommerceClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Permissive for local IIS HTTP bindings
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Admin", "Staff"));
    options.AddPolicy("CustomerAccess", policy => policy.RequireRole("Customer", "Admin", "Staff"));
});

// 4. Configure Controllers & Consistent Validation Error Responses
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err => err.ErrorMessage))
                .ToList();

            var response = ApiResponse<object>.Fail("Dữ liệu đầu vào không hợp lệ.", errors);
            return new BadRequestObjectResult(response);
        };
    });

// 5. Configure Swagger OpenAPI with JWT Bearer "Authorize" UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "E-Commerce Backend API",
        Version = "v1",
        Description = "Hệ thống E-Commerce API chuẩn Clean Architecture: EF Core 8.0 Code-First, Stored Procedure Transaction (ACID), JWT Authentication + RBAC (Admin/Staff/Customer), Serilog Structured Logging, Global Exception Middleware và hỗ trợ Host Local IIS."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Dán trực tiếp chuỗi Access Token (JWT eyJ...):",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 6. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 7. Auto Migration & Database Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        
        // Ensure Database is created & updated
        logger.LogInformation("Đang kiểm tra schema và khởi tạo cơ sở dữ liệu...");
        await db.Database.EnsureCreatedAsync();

        // Seed initial roles, admin user, products and stored procedure
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Cơ sở dữ liệu đã sẵn sàng và được nạp dữ liệu mẫu thành công.");
    }
    catch (Exception ex)
    {
        logger.LogWarning("Khởi tạo database bỏ qua hoặc có cảnh báo: {Message}. (Sẽ tự động kết nối lại khi SQL Server online).", ex.Message);
    }
}

// 8. HTTP Pipeline Configuration (Thứ tự Middlewares chuẩn)
// 8.1 Gắn mã tương quan CorrelationId cho mỗi request
app.UseMiddleware<CorrelationIdMiddleware>();

// 8.2 Ghi log HTTP Request với Serilog (tự động đính kèm CorrelationId, URL, Status code, Time)
app.UseSerilogRequestLogging();

// 8.3 Xử lý ngoại lệ toàn cục tập trung
app.UseMiddleware<GlobalExceptionMiddleware>();

// 8.4 Swagger UI tại root URL (tiện test trên trình duyệt hoặc IIS)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
    c.RoutePrefix = string.Empty; // Truy cập Swagger tại http://localhost:[port]/
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Khởi động ứng dụng E-Commerce Web API thành công...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Ứng dụng bị dừng đột ngột do lỗi nghiêm trọng.");
}
finally
{
    Log.CloseAndFlush();
}
