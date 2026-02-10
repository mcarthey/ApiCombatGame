# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ApiCombatGame.sln .
COPY ApiCombatGame/ApiCombatGame.csproj ApiCombatGame/
COPY ApiCombatGame.Tests/ApiCombatGame.Tests.csproj ApiCombatGame.Tests/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build
RUN dotnet build -c Release --no-restore

# Test
RUN dotnet test -c Release --no-build --verbosity normal

# Publish
FROM build AS publish
RUN dotnet publish ApiCombatGame/ApiCombatGame.csproj -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create directory for SQLite database
RUN mkdir -p /app/data

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/api_combat_game.db"

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ApiCombatGame.dll"]
