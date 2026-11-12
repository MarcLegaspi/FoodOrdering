using FoodOrdering.Application.Catalog;
using FoodOrdering.Application.Orders;
using FoodOrdering.Application.Promos;
using FoodOrdering.Application.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace FoodOrdering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<StoreService>();
        services.AddScoped<StoreAccessService>();
        services.AddScoped<StoreMemberService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<MenuItemService>();
        services.AddScoped<MenuItemImageService>();
        services.AddScoped<PromoService>();
        services.AddScoped<OrderService>();

        return services;
    }
}
