FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GalacticaBot.Dashboard.Api/GalacticaBot.Dashboard.Api.csproj", "GalacticaBot.Dashboard.Api/"]
RUN dotnet restore "GalacticaBot.Dashboard.Api/GalacticaBot.Dashboard.Api.csproj"
COPY . .
WORKDIR "/src/GalacticaBot.Dashboard.Api"
RUN dotnet build "./GalacticaBot.Dashboard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GalacticaBot.Dashboard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GalacticaBot.Dashboard.Api.dll"]
