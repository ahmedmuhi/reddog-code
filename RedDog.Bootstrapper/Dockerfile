FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/RedDog.Bootstrapper"
RUN dotnet build "RedDog.Bootstrapper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RedDog.Bootstrapper.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedDog.Bootstrapper.dll"]
