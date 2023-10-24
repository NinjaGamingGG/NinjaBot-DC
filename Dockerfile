FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
VOLUME /app/ninjabot
WORKDIR /app

ENV ninja-bot:token="You Token Here"
ENV ninja-bot:prefix="!"
ENV ninja-bot:sqlite-source="ninjabot/database.sqlite"
ENV ninja-bot:plugin-folder="ninjabot/plugins"

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["NinjaBot-DC/NinjaBot-DC.csproj", "NinjaBot-DC/"]
COPY ["PluginBase/PluginBase.csproj","PluginBase/"]

RUN dotnet restore "NinjaBot-DC/NinjaBot-DC.csproj"
COPY . .
WORKDIR "/src/NinjaBot-DC"
RUN dotnet build "NinjaBot-DC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NinjaBot-DC.csproj" -c Release -o /app/publish

FROM debian:11-slim
RUN apt-get update && apt-get upgrade -y
RUN apt-get install fontconfig -y

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NinjaBot-DC.dll"]