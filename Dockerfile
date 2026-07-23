# ==============================================================================
# Build Stage
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution props and project file for caching restore layer
COPY Directory.Build.props ./
COPY MutualFund.ConsolidatedAPI/MutualFund.ConsolidatedAPI.csproj MutualFund.ConsolidatedAPI/

# Restore dependencies
RUN dotnet restore MutualFund.ConsolidatedAPI/MutualFund.ConsolidatedAPI.csproj

# Copy full source tree
COPY . .

# Build & publish in Release mode
WORKDIR /src/MutualFund.ConsolidatedAPI
RUN dotnet publish MutualFund.ConsolidatedAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# Runtime Stage
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Set production environment and default port (Render uses $PORT or 8080)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
# Disable file watchers to avoid inotify limit crash on Render free tier
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
EXPOSE 8080

# Copy published binaries from build stage
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MutualFund.ConsolidatedAPI.dll"]
