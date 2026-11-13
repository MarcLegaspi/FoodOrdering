# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for optimal Docker layer caching
COPY ["src/FoodOrdering.Api/FoodOrdering.Api.csproj", "src/FoodOrdering.Api/"]
COPY ["src/FoodOrdering.Application/FoodOrdering.Application.csproj", "src/FoodOrdering.Application/"]
COPY ["src/FoodOrdering.Domain/FoodOrdering.Domain.csproj", "src/FoodOrdering.Domain/"]
COPY ["src/FoodOrdering.Infrastructure/FoodOrdering.Infrastructure.csproj", "src/FoodOrdering.Infrastructure/"]

RUN dotnet restore "src/FoodOrdering.Api/FoodOrdering.Api.csproj"

# Copy all source files
COPY src/ src/

# Publish
WORKDIR "/src/src/FoodOrdering.Api"
RUN dotnet publish "FoodOrdering.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Railway injects PORT (default 8080)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FoodOrdering.Api.dll"]
