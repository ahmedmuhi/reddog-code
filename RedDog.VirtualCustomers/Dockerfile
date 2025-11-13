FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/RedDog.VirtualCustomers"
# Restore dependencies once (keeps publish layer reproducible)
RUN dotnet restore "RedDog.VirtualCustomers.csproj"
RUN dotnet publish "RedDog.VirtualCustomers.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RedDog.VirtualCustomers.dll"]
