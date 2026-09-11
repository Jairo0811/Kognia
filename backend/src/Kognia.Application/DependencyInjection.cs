using Microsoft.Extensions.DependencyInjection;

namespace Kognia.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
