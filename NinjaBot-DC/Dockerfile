FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NinjaBot-DC/NinjaBot-DC.csproj", "NinjaBot-DC/"]
COPY ["CommonPluginHelpers/CommonPluginHelpers.csproj", "CommonPluginHelpers/"]
COPY ["PluginBase/PluginBase.csproj", "PluginBase/"]
RUN dotnet restore "NinjaBot-DC/NinjaBot-DC.csproj"
COPY . .
WORKDIR "/src/NinjaBot-DC"
RUN dotnet build "./NinjaBot-DC.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NinjaBot-DC.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NinjaBot-DC.dll"]