using Kognia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kognia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KogniaConnection")
            ?? throw new InvalidOperationException("Connection string 'KogniaConnection' was not found.");

        services.AddDbContext<KogniaDbContext>(options => options.UseSqlServer(connectionString));
        return services;
    }
}
