# Dockerfile - Place in root directory (same level as .sln file)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files - Updated for your actual structure
COPY ["PlantMonitor.Domain/PlantMonitor.Domain.csproj", "PlantMonitor.Domain/"]
COPY ["PlantMonitor.Application/PlantMonitor.Application.csproj", "PlantMonitor.Application/"]
COPY ["PlantMonitor.Infrastructure/PlantMonitor.Infrastructure.csproj", "PlantMonitor.Infrastructure/"]
COPY ["PlantMonitor.Api/PlantMonitor.Api.csproj", "PlantMonitor.Api/"]

# Restore dependencies
RUN dotnet restore "PlantMonitor.Api/PlantMonitor.Api.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/PlantMonitor.Api"
RUN dotnet build "PlantMonitor.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PlantMonitor.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Configure environment for Railway
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PlantMonitor.Api.dll"]