# ADR-0003: Ubuntu 24.04 base image standardization
# ADR-0006: Infrastructure configuration via environment variables (ASPNETCORE_URLS)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/RedDog.ReceiptGenerationService"
RUN dotnet build "RedDog.ReceiptGenerationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RedDog.ReceiptGenerationService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedDog.ReceiptGenerationService.dll"]
