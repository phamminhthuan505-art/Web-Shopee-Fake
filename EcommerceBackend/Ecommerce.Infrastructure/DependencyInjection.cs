using Ecommerce.Application.Common.Interfaces;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=EcommerceDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            }));

        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
