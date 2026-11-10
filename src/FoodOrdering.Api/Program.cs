using FoodOrdering.Api.Endpoints;
using FoodOrdering.Api.Middleware;
using FoodOrdering.Api.Services;
using FoodOrdering.Application;
using FoodOrdering.Application.Common.Interfaces;
using FoodOrdering.Infrastructure;
using FoodOrdering.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapAuthEndpoints();
app.MapStoreEndpoints();
app.MapStoreMemberEndpoints();
app.MapCategoryEndpoints();

await AdminSeeder.SeedAsync(app.Services);

app.Run();
