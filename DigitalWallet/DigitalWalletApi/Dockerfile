# FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# WORKDIR /src

# COPY DigitalWallet/*.csproj DigitalWallet/
# COPY DigitalWalletApplication/*.csproj DigitalWalletApplication/
# COPY DigitalWalletCore/*.csproj DigitalWalletCore/
# COPY DigitalWalletInfrastructure/*.csproj DigitalWalletInfrastructure/

# RUN dotnet restore DigitalWallet/DigitalWalletApi.csproj

# COPY . .

# RUN dotnet publish DigitalWallet/DigitalWalletApi.csproj -c Release -o /app/publish

# FROM mcr.microsoft.com/dotnet/aspnet:10.0

# WORKDIR /app

# COPY --from=build /app/publish .

# EXPOSE 8080

# ENTRYPOINT ["dotnet","DigitalWallet.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

COPY . .

WORKDIR /src/DigitalWalletApi

RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libx11-6 \
    libxrender1 \
    libxext6

RUN dotnet restore DigitalWalletApi.csproj

EXPOSE 5071

CMD ["dotnet", "watch", "run", "--urls=http://0.0.0.0:5071"]
