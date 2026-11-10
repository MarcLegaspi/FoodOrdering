using FoodOrdering.Application.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace FoodOrdering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<StoreService>();

        return services;
    }
}
