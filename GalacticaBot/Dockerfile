FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base

# Install step CLI for certificate management
USER root
RUN apt-get update && \
    apt-get install -y wget ca-certificates jq && \
    wget -O step.tar.gz https://dl.smallstep.com/gh-release/cli/gh-release-header/v0.27.2/step_linux_0.27.2_amd64.tar.gz && \
    tar -xzf step.tar.gz && \
    mv step_0.27.2/bin/step /usr/local/bin/ && \
    rm -rf step.tar.gz step_0.27.2 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GalacticaBot/GalacticaBot.csproj", "GalacticaBot/"]
RUN dotnet restore "GalacticaBot/GalacticaBot.csproj"
COPY . .
WORKDIR "/src/GalacticaBot"
RUN dotnet build "./GalacticaBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GalacticaBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create certificates directory
USER root
RUN mkdir -p /app/certs && chown $APP_UID:$APP_UID /app/certs
USER $APP_UID

# Copy entrypoint script
COPY --chown=$APP_UID:$APP_UID step-ca/scripts/provision-bot-cert.sh /app/scripts/provision-bot-cert.sh
RUN chmod +x /app/scripts/provision-bot-cert.sh

ENV DOTNET_ENVIRONMENT=Production

# Provision certificates and start the application
ENTRYPOINT ["/bin/bash", "-c", "/app/scripts/provision-bot-cert.sh && dotnet GalacticaBot.dll"]
