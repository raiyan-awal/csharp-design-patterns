using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjectionPattern;

public static class ServiceRegistration
{
    public static IServiceCollection AddCheckoutServices(this IServiceCollection services)
    {
        services.AddSingleton<IInventoryService, InventoryService>(); // one instance for the app's lifetime
        services.AddScoped<IShoppingCart, ShoppingCart>();            // one instance per scope (session)
        services.AddScoped<ICheckoutService, CheckoutService>();      // one instance per scope (session)
        services.AddTransient<IHstCalculator, HstCalculator>();       // new instance every time
        return services;
    }
}
