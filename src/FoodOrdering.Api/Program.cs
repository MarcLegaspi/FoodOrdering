using FoodOrdering.Api.Endpoints;
using FoodOrdering.Api.Hubs;
using FoodOrdering.Api.Middleware;
using FoodOrdering.Api.Services;
using FoodOrdering.Application;
using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Infrastructure;
using FoodOrdering.Infrastructure.Identity;
using FoodOrdering.Infrastructure.Storage;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

var storageOptions = app.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
    ?? new StorageOptions();
var uploadsRoot = StorageOptions.ResolveUploadsRoot(storageOptions, app.Environment.ContentRootPath);
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapAuthEndpoints();
app.MapStoreEndpoints();
app.MapStoreMemberEndpoints();
app.MapCategoryEndpoints();
app.MapMenuItemEndpoints();
app.MapPromoEndpoints();
app.MapOrderEndpoints();
app.MapHub<OrderHub>("/hubs/orders");

await AdminSeeder.SeedAsync(app.Services);

app.Run();
