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

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Configuration (Console + Daily Rolling File in logs/)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/ecommerce-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
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

            var response = ApiResponse<object>.Fail("Validation failed for input data.", errors);
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
        Description = "Enterprise E-Commerce API with Clean Architecture, EF Core Code-First, Stored Procedure Transaction, JWT RBAC, Serilog, and Local IIS support."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
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
        logger.LogInformation("Applying migrations / verifying database schema...");
        await db.Database.EnsureCreatedAsync();

        // Seed initial roles, admin user, products and stored procedure
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Database successfully initialized and seeded.");
    }
    catch (Exception ex)
    {
        logger.LogWarning("Database initialization skipped or encountered warning: {Message}. (Will connect when SQL Server is running).", ex.Message);
    }
}

// 8. HTTP Pipeline Configuration
app.UseMiddleware<GlobalExceptionMiddleware>();

// Enable Swagger in all environments (supports Local IIS debugging and demo)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
    c.RoutePrefix = string.Empty; // Serves Swagger UI at application root URL: http://localhost:[port]/
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting E-Commerce Web API application...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
