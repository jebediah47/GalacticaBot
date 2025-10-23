# Local Development and Testing Guide

This guide explains how to set up and test the GalacticaBot mTLS implementation on your local machine without deploying to production.

## Prerequisites

Before you begin, ensure you have the following installed:

- **Docker Desktop** (or Docker Engine + Docker Compose)
  - Docker version 20.10+ recommended
  - Docker Compose version 2.0+ recommended
- **Git** for cloning the repository
- **.NET SDK 9.0** (optional, for building/debugging outside containers)
- **PostgreSQL client tools** (optional, for database debugging)

### Verify Prerequisites

```bash
# Check Docker
docker --version
docker-compose --version

# Check .NET (optional)
dotnet --version

# Verify Docker is running
docker ps
```

## Quick Start: Local Testing

### 1. Clone and Set Up

```bash
# Clone the repository
git clone https://github.com/jebediah47/GalacticaBot.git
cd GalacticaBot

# Checkout the mTLS branch (if not on main)
git checkout copilot/implement-mtls-with-step-ca
```

### 2. Configure Environment Variables

```bash
# Copy the example environment file
cp .env.example .env

# Edit the .env file with your local settings
nano .env  # or use your preferred editor
```

**Required variables for local testing:**

```env
# Local PostgreSQL (you can use docker-compose to run it)
DATABASE_URL=postgresql://galacticabot:galacticabot@postgres:5432/galacticabot

# Discord Bot Token - Get from https://discord.com/developers/applications
GALACTICA_TOKEN=your_discord_bot_token_here

# Certificate Authority Password (use any password for local testing)
CA_PASSWORD=local-test-password-123
```

### 3. Start Services with Docker Compose

```bash
# Start all services (step-ca, postgres, api, bot) in detached mode
docker-compose up -d

# View logs from all services
docker-compose logs -f

# Or view logs from specific services
docker-compose logs -f step-ca
docker-compose logs -f galactica-bot.api
docker-compose logs -f galactica-bot
```

### 4. Verify mTLS Setup

Once services are running, verify the mTLS configuration:

```bash
# Check that step-ca is healthy
docker-compose exec step-ca step ca health

# Expected output: "ok"
```

```bash
# Verify API server certificate
docker-compose exec galactica-bot.api step certificate inspect /app/certs/galactica-bot.api.crt

# Check certificate expiry date
docker-compose exec galactica-bot.api step certificate inspect /app/certs/galactica-bot.api.crt --format json | jq -r '.validity.end'
```

```bash
# Verify Bot client certificate
docker-compose exec galactica-bot step certificate inspect /app/certs/galactica-bot.crt

# Check certificate expiry date
docker-compose exec galactica-bot step certificate inspect /app/certs/galactica-bot.crt --format json | jq -r '.validity.end'
```

```bash
# Check that bot connected to API successfully
docker-compose logs galactica-bot | grep -i "connected to botconfighub"

# Expected output: "Connected to BotConfigHub at https://galactica-bot.api:8443/hubs/botconfig"
```

### 5. Test the Health Endpoint

```bash
# Test the API health endpoint (will fail without valid client cert)
docker-compose exec galactica-bot curl -k https://galactica-bot.api:8443/health

# Expected: Connection should succeed and return: {"status":"healthy","timestamp":"..."}
```

### 6. Inspect Certificate Details

```bash
# View all files in the API certs directory
docker-compose exec galactica-bot.api ls -la /app/certs/

# View all files in the Bot certs directory
docker-compose exec galactica-bot ls -la /app/certs/

# Expected files:
# - root_ca.crt (CA root certificate)
# - galactica-bot.api.crt (API server certificate)
# - galactica-bot.api.key (API private key)
# - galactica-bot.crt (Bot client certificate)
# - galactica-bot.key (Bot private key)
```

## Testing Without a PostgreSQL Database

If you don't have a Discord bot token or database yet, you can still test the mTLS components:

### Option 1: Add PostgreSQL to Docker Compose

Add this service to your `compose.yaml`:

```yaml
  postgres:
    image: postgres:16-alpine
    environment:
      - POSTGRES_USER=galacticabot
      - POSTGRES_PASSWORD=galacticabot
      - POSTGRES_DB=galacticabot
    ports:
      - "5432:5432"
    networks:
      - galactica-bot-network
    volumes:
      - postgres-data:/var/lib/postgresql/data
```

And add the volume:

```yaml
volumes:
  step-ca-data:
  bot-certs:
  api-certs:
  postgres-data:  # Add this
```

Then update your `.env`:

```env
DATABASE_URL=postgresql://galacticabot:galacticabot@postgres:5432/galacticabot
```

### Option 2: Test Only mTLS Components

You can test just the certificate infrastructure without the full application:

```bash
# Start only step-ca
docker-compose up -d step-ca

# Check it's running
docker-compose logs step-ca
docker-compose exec step-ca step ca health

# Manually provision a test certificate
docker-compose exec step-ca sh -c '
  echo "changeme" | step ca certificate test-service \
    /tmp/test.crt /tmp/test.key \
    --provisioner admin \
    --provisioner-password-file /dev/stdin
'

# Inspect the test certificate
docker-compose exec step-ca step certificate inspect /tmp/test.crt
```

## Debugging Common Issues

### Issue: Services fail to start

**Check logs:**
```bash
docker-compose logs step-ca
docker-compose logs galactica-bot.api
docker-compose logs galactica-bot
```

**Common causes:**
- step-ca not initialized yet (wait 10-20 seconds)
- Missing environment variables
- Port conflicts (8443, 9000 already in use)

**Solution:**
```bash
# Stop all services
docker-compose down

# Clean up volumes if needed
docker-compose down -v

# Start again
docker-compose up -d

# Wait for step-ca to be ready
sleep 15

# Check status
docker-compose ps
```

### Issue: Certificates not provisioned

**Check certificate files:**
```bash
docker-compose exec galactica-bot.api ls -la /app/certs/
docker-compose exec galactica-bot ls -la /app/certs/
```

**If certificates are missing:**
```bash
# Restart the affected service
docker-compose restart galactica-bot.api
docker-compose restart galactica-bot

# Watch logs for certificate provisioning
docker-compose logs -f galactica-bot.api | grep -i certificate
```

### Issue: Bot can't connect to API

**Verify network connectivity:**
```bash
# From bot container, test connection to API
docker-compose exec galactica-bot nc -zv galactica-bot.api 8443

# Expected: "galactica-bot.api (172.x.x.x:8443) open"
```

**Check certificate validation:**
```bash
# Verify bot certificate is valid
docker-compose exec galactica-bot step certificate verify \
  /app/certs/galactica-bot.crt \
  --roots /app/certs/root_ca.crt
```

### Issue: "certificate validation failed"

**Re-download root CA certificate:**
```bash
docker-compose exec galactica-bot step ca root \
  /app/certs/root_ca.crt \
  --ca-url https://step-ca:9000 \
  --insecure

docker-compose restart galactica-bot
```

## Local Development Workflow

### Making Code Changes

If you need to modify the application code:

1. **Make your changes** in the source files
2. **Rebuild the Docker images:**
   ```bash
   docker-compose build
   ```
3. **Restart the services:**
   ```bash
   docker-compose up -d
   ```
4. **View logs to verify:**
   ```bash
   docker-compose logs -f
   ```

### Testing Certificate Renewal

To test the certificate renewal process:

```bash
# Delete existing certificates to force renewal
docker-compose exec galactica-bot.api rm /app/certs/galactica-bot.api.crt /app/certs/galactica-bot.api.key

# Restart to trigger re-provisioning
docker-compose restart galactica-bot.api

# Watch logs for certificate provisioning
docker-compose logs -f galactica-bot.api | grep "Certificate provisioned"
```

### Testing Certificate Revocation

```bash
# Get certificate serial number
SERIAL=$(docker-compose exec galactica-bot.api step certificate inspect \
  /app/certs/galactica-bot.api.crt --format json | jq -r '.serial_number')

# Revoke the certificate
docker-compose exec step-ca step ca revoke --cert-serial "$SERIAL"

# Re-provision
docker-compose exec galactica-bot.api /app/scripts/provision-api-cert.sh
docker-compose restart galactica-bot.api
```

## Building Without Docker (Optional)

If you want to build and test locally without Docker:

### 1. Install .NET SDK 9.0

```bash
# Verify installation
dotnet --version  # Should be 9.0.x
```

### 2. Set up PostgreSQL locally

```bash
# Install PostgreSQL (example for macOS)
brew install postgresql@16
brew services start postgresql@16

# Create database
createdb galacticabot
```

### 3. Set environment variables

```bash
export DATABASE_URL="postgresql://localhost:5432/galacticabot"
export GALACTICA_TOKEN="your_discord_bot_token"
export BOT_API_URL="https://localhost:8443"
export CA_URL="https://localhost:9000"
export CA_PASSWORD="local-test-password"
export MTLS_ENABLED="true"
```

### 4. Build and run

```bash
# Build the solution
dotnet build

# Run migrations
cd GalacticaBot.Api
dotnet ef database update

# Run the API
cd GalacticaBot.Api
dotnet run

# In another terminal, run the Bot
cd GalacticaBot
dotnet run
```

**Note:** Running locally without Docker requires manual setup of step-ca and certificate provisioning, which is more complex. Docker Compose is the recommended approach for local testing.

## Cleanup

When you're done testing:

```bash
# Stop all services
docker-compose down

# Stop and remove volumes (deletes all data including certificates)
docker-compose down -v

# Remove built images (optional)
docker-compose down --rmi local
```

## Integration Testing Checklist

Use this checklist to verify the mTLS setup is working correctly:

- [ ] step-ca starts and is healthy (`docker-compose exec step-ca step ca health`)
- [ ] API service provisions its server certificate successfully
- [ ] Bot service provisions its client certificate successfully
- [ ] Bot connects to API via mTLS (check logs for "Connected to BotConfigHub")
- [ ] Health endpoint is accessible (`/health` returns `{"status":"healthy"}`)
- [ ] Certificates have correct validity period (~90 days)
- [ ] Root CA certificate is present in both services
- [ ] Certificate files have correct permissions (600 for keys, 644 for certs)

## Troubleshooting Resources

- **Docker Compose logs:** `docker-compose logs -f`
- **Service-specific logs:** `docker-compose logs -f <service-name>`
- **Container shell access:** `docker-compose exec <service-name> /bin/bash`
- **Network inspection:** `docker network inspect galacticabot_galactica-bot-network`
- **Volume inspection:** `docker volume inspect galacticabot_step-ca-data`

## Additional Resources

- Full documentation: [`step-ca/README.md`](step-ca/README.md)
- Quick reference: [`MTLS_QUICKSTART.md`](MTLS_QUICKSTART.md)
- Kubernetes deployment: [`k8s/README.md`](k8s/README.md)
- Smallstep documentation: https://smallstep.com/docs/step-ca/

## Getting Help

If you encounter issues:

1. Check the logs: `docker-compose logs -f`
2. Review the troubleshooting section above
3. Check existing documentation in `step-ca/README.md`
4. Open an issue with:
   - Steps to reproduce
   - Full logs from `docker-compose logs`
   - Your environment details (OS, Docker version)
   - Contents of your `.env` file (redact secrets!)

## Next Steps

Once local testing is successful:

1. Review the production deployment guide in `k8s/README.md`
2. Set up proper secrets management for production
3. Configure monitoring and alerting for certificate expiry
4. Implement automated backups of step-ca data
5. Review security best practices in `step-ca/README.md`
