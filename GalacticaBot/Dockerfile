FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS base

# Install step CLI for certificate management
USER root
RUN apk add --no-cache bash jq curl wget ca-certificates && \
    wget -O /etc/apk/keys/smallstep.rsa.pub https://raw.githubusercontent.com/smallstep/cli/master/packaging/alpine/smallstep.rsa.pub && \
    echo "https://raw.githubusercontent.com/smallstep/cli/master/packaging/alpine/" >> /etc/apk/repositories && \
    apk update && \
    apk add --no-cache step-cli

USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
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
