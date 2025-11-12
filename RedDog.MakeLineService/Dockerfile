# Build stage - .NET 10 SDK on Ubuntu 24.04 (ADR-0003)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/RedDog.MakeLineService"
RUN dotnet build "RedDog.MakeLineService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RedDog.MakeLineService.csproj" -c Release -o /app/publish

# Runtime stage - .NET 10 ASP.NET Core on Ubuntu 24.04 (ADR-0003)
# Infrastructure configuration via environment variables (ADR-0006)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedDog.MakeLineService.dll"]
