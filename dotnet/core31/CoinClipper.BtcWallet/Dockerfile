 

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 61431
EXPOSE 44311

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY CoinClipper.BtcWallet.Api.csproj CoinClipper.BtcWallet/
RUN dotnet restore CoinClipper.BtcWallet/CoinClipper.BtcWallet.Api.csproj
COPY . .
RUN dotnet build CoinClipper.BtcWallet.Api.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish CoinClipper.BtcWallet.Api.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CoinClipper.BtcWallet.Api.dll"]
