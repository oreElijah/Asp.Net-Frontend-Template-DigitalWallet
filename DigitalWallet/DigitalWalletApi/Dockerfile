FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src/DigitalWallet

COPY . .

RUN dotnet restore DigitalWalletApi.csproj

RUN dotnet publish DigitalWalletApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 10000

ENTRYPOINT ["dotnet", "DigitalWalletApi.dll"]
