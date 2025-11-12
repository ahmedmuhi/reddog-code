# Build stage - .NET 10 SDK (Ubuntu 24.04 per ADR-0003)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/RedDog.OrderService"
RUN dotnet build "RedDog.OrderService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RedDog.OrderService.csproj" -c Release -o /app/publish

# Runtime stage - .NET 10 ASP.NET Runtime (Ubuntu 24.04 per ADR-0003)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 80

# Infrastructure configuration (ADR-0006)
ENV ASPNETCORE_URLS=http://+:80

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedDog.OrderService.dll"]
