using System.Text;
using FoodOrdering.Application.Auth;
using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Infrastructure.Database;
using FoodOrdering.Infrastructure.Identity;
using FoodOrdering.Infrastructure.MultiTenancy;
using FoodOrdering.Infrastructure.Payments;
using FoodOrdering.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FoodOrdering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider =>
            provider.GetRequiredService<TenantContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<IUserDirectory, IdentityUserDirectory>();
        services.AddScoped<IAuthService, IdentityAuthService>();

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.AddScoped<LocalFileImageStorageService>();
        services.AddHttpClient<SupabaseImageStorageService>();

        services.AddScoped<IImageStorageService>(sp =>
        {
            var storageOpts = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            var isSupabase = string.Equals(storageOpts.Provider, "Supabase", StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(storageOpts.SupabaseUrl) && !string.IsNullOrWhiteSpace(storageOpts.SupabaseKey));

            if (isSupabase)
            {
                return sp.GetRequiredService<SupabaseImageStorageService>();
            }

            return sp.GetRequiredService<LocalFileImageStorageService>();
        });

        services.Configure<XenditOptions>(options =>
        {
            configuration.GetSection(XenditOptions.SectionName).Bind(options);

            options.SecretKey ??= configuration["Xendit:SecretKey"]
                               ?? configuration["Xendit__SecretKey"]
                               ?? configuration["XENDIT_SECRET_KEY"]
                               ?? configuration["XENDIT_API_KEY"]
                               ?? Environment.GetEnvironmentVariable("XENDIT_SECRET_KEY")
                               ?? Environment.GetEnvironmentVariable("Xendit__SecretKey")
                               ?? Environment.GetEnvironmentVariable("XENDIT_API_KEY");

            options.WebhookToken ??= configuration["Xendit:WebhookToken"]
                                  ?? configuration["Xendit__WebhookToken"]
                                  ?? configuration["XENDIT_WEBHOOK_TOKEN"]
                                  ?? Environment.GetEnvironmentVariable("XENDIT_WEBHOOK_TOKEN");

            options.WebBaseUrl ??= configuration["Xendit:WebBaseUrl"]
                                ?? configuration["Xendit__WebBaseUrl"]
                                ?? configuration["XENDIT_WEB_BASE_URL"]
                                ?? Environment.GetEnvironmentVariable("XENDIT_WEB_BASE_URL")
                                ?? "https://food-ordering-web.pages.dev";
        });
        services.AddHttpClient<IPaymentGatewayService, XenditPaymentGatewayService>();

        return services;
    }
}
