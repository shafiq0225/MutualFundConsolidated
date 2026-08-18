FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["MutualFund.Mcp.API/MutualFund.Mcp.API.csproj", "MutualFund.Mcp.API/"]
COPY ["MutualFund.ConsolidatedAPI/MutualFund.ConsolidatedAPI.csproj", "MutualFund.ConsolidatedAPI/"]
RUN dotnet restore "MutualFund.Mcp.API/MutualFund.Mcp.API.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/MutualFund.Mcp.API"
RUN dotnet build "MutualFund.Mcp.API.csproj" -c Release -o /app/build
RUN dotnet publish "MutualFund.Mcp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MutualFund.Mcp.API.dll"]
