# GalacticaBot mTLS Quick Start

This is a quick reference guide for getting started with mTLS in GalacticaBot.

## Quick Setup (Docker Compose)

### 1. Configure Environment

```bash
# Copy example environment file
cp .env.example .env

# Edit .env and set your values
nano .env
```

Required variables:
- `DATABASE_URL`: PostgreSQL connection string
- `GALACTICA_TOKEN`: Discord bot token
- `CA_PASSWORD`: Strong password for step-ca (change from default!)

### 2. Start Services

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Check status
docker-compose ps
```

### 3. Verify mTLS

```bash
# Check step-ca is healthy
docker-compose exec step-ca step ca health

# Verify API certificate
docker-compose exec galactica-bot.api \
  step certificate inspect /app/certs/galactica-bot.api.crt

# Verify Bot certificate
docker-compose exec galactica-bot \
  step certificate inspect /app/certs/galactica-bot.crt

# Check bot connection to API
docker-compose logs galactica-bot | grep "Connected to BotConfigHub"
```

## Common Operations

### Check Certificate Expiry

```bash
# API certificate
docker-compose exec galactica-bot.api bash -c '
  step certificate inspect /app/certs/galactica-bot.api.crt \
    --format json | jq -r ".validity.end"
'

# Bot certificate
docker-compose exec galactica-bot bash -c '
  step certificate inspect /app/certs/galactica-bot.crt \
    --format json | jq -r ".validity.end"
'
```

### Renew Certificates

```bash
# Renew API certificate
docker-compose exec galactica-bot.api /app/scripts/provision-api-cert.sh
docker-compose restart galactica-bot.api

# Renew Bot certificate
docker-compose exec galactica-bot /app/scripts/provision-bot-cert.sh
docker-compose restart galactica-bot
```

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f step-ca
docker-compose logs -f galactica-bot.api
docker-compose logs -f galactica-bot

# Filter for certificate-related logs
docker-compose logs galactica-bot.api | grep -i certificate
```

## Troubleshooting

### Certificates Not Generated

**Problem**: Services fail to start with certificate errors.

**Solution**:
```bash
# Check step-ca is running
docker-compose ps step-ca

# View step-ca logs
docker-compose logs step-ca

# Restart step-ca
docker-compose restart step-ca

# Wait for CA to be healthy, then restart services
docker-compose restart galactica-bot.api galactica-bot
```

### Connection Refused

**Problem**: Bot can't connect to API.

**Solution**:
```bash
# Check API is listening on port 8443
docker-compose exec galactica-bot nc -zv galactica-bot.api 8443

# Check API logs for startup errors
docker-compose logs galactica-bot.api | tail -50

# Verify API certificate exists
docker-compose exec galactica-bot.api ls -la /app/certs/
```

### Certificate Validation Failed

**Problem**: "certificate validation failed" in logs.

**Solution**:
```bash
# Re-download root CA certificate
docker-compose exec galactica-bot \
  step ca root /app/certs/root_ca.crt \
  --ca-url https://step-ca:9000 --insecure

# Restart bot
docker-compose restart galactica-bot

# Verify certificate chain
docker-compose exec galactica-bot \
  step certificate verify /app/certs/galactica-bot.crt \
  --roots /app/certs/root_ca.crt
```

## Reset Everything

If you need to start fresh:

```bash
# Stop all services
docker-compose down

# Remove volumes (WARNING: This deletes all certificates and CA data!)
docker volume rm galacticabot_step-ca-data
docker volume rm galacticabot_api-certs
docker volume rm galacticabot_bot-certs

# Start fresh
docker-compose up -d
```

## Security Checklist

Before deploying to production:

- [ ] Change `CA_PASSWORD` from default value
- [ ] Use strong passwords for all secrets
- [ ] Store secrets in a secure vault (not in .env file)
- [ ] Backup step-ca volume regularly
- [ ] Set up certificate expiry monitoring
- [ ] Configure firewall to block external access to port 9000 (step-ca)
- [ ] Enable network isolation between services
- [ ] Review and harden Docker/Kubernetes security settings
- [ ] Test certificate renewal process
- [ ] Document incident response procedures

## Additional Resources

- Full documentation: `step-ca/README.md`
- Kubernetes deployment: `k8s/README.md`
- Smallstep docs: https://smallstep.com/docs/step-ca/
- GitHub issues: Report problems to the repository

## Support

For help:
1. Check logs: `docker-compose logs`
2. Review documentation in `step-ca/README.md`
3. Check troubleshooting section above
4. Open an issue with logs and error details
