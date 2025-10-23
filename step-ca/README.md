# mTLS Setup with step-ca

This document provides comprehensive guidance on managing mutual TLS (mTLS) authentication using step-ca in the GalacticaBot infrastructure.

## Overview

GalacticaBot uses [Smallstep step-ca](https://smallstep.com/docs/step-ca/) as the internal Certificate Authority to manage TLS certificates and enable mTLS authentication between services. This provides:

- **Automated certificate issuance and renewal**
- **Strong authentication** between GalacticaBot and GalacticaBot.Api
- **Reduced manual overhead** in certificate management
- **Scalable security** for production deployments

## Architecture

The mTLS setup consists of three main components:

1. **step-ca**: The Certificate Authority service that issues and manages certificates
2. **GalacticaBot.Api**: The API service with server certificates, requiring client certificates
3. **GalacticaBot**: The bot service with client certificates for authenticating to the API

### Certificate Flow

```
step-ca (CA)
    ├─> Issues server certificate to GalacticaBot.Api
    └─> Issues client certificate to GalacticaBot

GalacticaBot → (client cert) → GalacticaBot.Api
                               (validates with CA root)
```

## Prerequisites

- Docker and Docker Compose
- PostgreSQL database
- Environment variables configured (see below)

## Configuration

### Environment Variables

Create a `.env` file in the project root with the following variables:

```env
# Database
DATABASE_URL=postgresql://user:password@postgres:5432/galacticabot

# Discord Bot Token
GALACTICA_TOKEN=your_discord_bot_token

# Certificate Authority
CA_PASSWORD=changeme  # Change this to a strong password in production!
```

### Certificate Authority Configuration

The CA is automatically initialized on first startup with:

- **CA Name**: GalacticaBot CA
- **CA DNS**: step-ca
- **CA Address**: :9000
- **Default Provisioner**: admin
- **Certificate Validity**: 90 days (2160 hours)
- **Auto-renewal**: Certificates are renewed when they have 7 days or less remaining

## Deployment

### Starting the Services

```bash
# Start all services including step-ca
docker-compose up -d

# View logs
docker-compose logs -f

# Check step-ca health
docker-compose exec step-ca step ca health
```

### Service Startup Order

1. **step-ca** starts first and initializes the CA
2. **GalacticaBot.Api** starts after CA is healthy, provisions its server certificate
3. **GalacticaBot** starts after API is ready, provisions its client certificate

### Initial Bootstrap

On first startup:

1. step-ca initializes and creates root and intermediate CA certificates
2. Each service provisions its certificates automatically
3. Services start with mTLS enabled

The bootstrap process is fully automated - no manual intervention required.

## Certificate Management

### Certificate Locations

Certificates are stored in Docker volumes:

- **step-ca data**: `step-ca-data` volume
- **API certificates**: `api-certs` volume (`/app/certs` in container)
- **Bot certificates**: `bot-certs` volume (`/app/certs` in container)

### Certificate Structure

Each service has:

```
/app/certs/
├── root_ca.crt          # CA root certificate (public)
├── <service-name>.crt   # Service certificate (public)
└── <service-name>.key   # Service private key (private)
```

### Viewing Certificates

```bash
# Inspect API server certificate
docker-compose exec galactica-bot.api step certificate inspect /app/certs/galactica-bot.api.crt

# Inspect Bot client certificate
docker-compose exec galactica-bot step certificate inspect /app/certs/galactica-bot.crt

# Check certificate expiration
docker-compose exec galactica-bot.api step certificate inspect /app/certs/galactica-bot.api.crt --format json | jq -r '.validity.end'
```

### Certificate Renewal

Certificates are automatically renewed when they have **7 days or less** remaining until expiration.

#### Automatic Renewal

The certificate provisioning scripts check certificate validity on each container restart and renew if necessary.

#### Manual Renewal

To manually renew certificates:

```bash
# Renew API certificate
docker-compose exec galactica-bot.api /app/scripts/provision-api-cert.sh

# Renew Bot certificate
docker-compose exec galactica-bot /app/scripts/provision-bot-cert.sh

# Restart services to use new certificates
docker-compose restart galactica-bot.api galactica-bot
```

#### Forced Renewal

To force certificate renewal (even if not expired):

```bash
# Remove existing certificates
docker-compose exec galactica-bot.api rm /app/certs/galactica-bot.api.crt /app/certs/galactica-bot.api.key

# Restart to trigger re-provisioning
docker-compose restart galactica-bot.api
```

### Certificate Revocation

To revoke a certificate:

```bash
# Get the serial number
SERIAL=$(docker-compose exec galactica-bot.api step certificate inspect /app/certs/galactica-bot.api.crt --format json | jq -r '.serial_number')

# Revoke the certificate
docker-compose exec step-ca step ca revoke --cert-serial "$SERIAL" --reason "superseded"

# Re-provision a new certificate
docker-compose exec galactica-bot.api /app/scripts/provision-api-cert.sh
docker-compose restart galactica-bot.api
```

## Monitoring

### Certificate Expiry Monitoring

Set up monitoring for certificate expiration:

```bash
# Check days until expiration
docker-compose exec galactica-bot.api bash -c '
  EXPIRY=$(step certificate inspect /app/certs/galactica-bot.api.crt --format json | jq -r ".validity.end")
  EXPIRY_SECONDS=$(date -d "$EXPIRY" +%s)
  NOW_SECONDS=$(date +%s)
  DAYS_LEFT=$(( ($EXPIRY_SECONDS - $NOW_SECONDS) / 86400 ))
  echo "Certificate expires in $DAYS_LEFT days"
'
```

### Recommended Alerts

Set up alerts for:

1. **Certificate expiration < 14 days** (Warning)
2. **Certificate expiration < 7 days** (Critical)
3. **Certificate expired** (Critical)
4. **CA service down** (Critical)
5. **Certificate provisioning failures** (Critical)

### Health Checks

```bash
# Check CA health
docker-compose exec step-ca step ca health

# Check if services can communicate
docker-compose exec galactica-bot wget --spider https://galactica-bot.api:8443

# Check certificate chain validity
docker-compose exec galactica-bot.api openssl verify -CAfile /app/certs/root_ca.crt /app/certs/galactica-bot.api.crt
```

## Troubleshooting

### Common Issues

#### Services Can't Connect

**Problem**: Bot can't connect to API with mTLS errors.

**Solutions**:
1. Check that certificates exist and are valid:
   ```bash
   docker-compose exec galactica-bot ls -la /app/certs/
   docker-compose exec galactica-bot.api ls -la /app/certs/
   ```

2. Verify certificate trust chain:
   ```bash
   docker-compose exec galactica-bot step certificate verify /app/certs/galactica-bot.crt --roots /app/certs/root_ca.crt
   ```

3. Check CA connectivity:
   ```bash
   docker-compose exec galactica-bot step ca health --ca-url https://step-ca:9000 --root /app/certs/root_ca.crt
   ```

#### Certificate Expired

**Problem**: Certificate has expired.

**Solution**:
```bash
# Remove expired certificate
docker-compose exec galactica-bot.api rm /app/certs/galactica-bot.api.crt /app/certs/galactica-bot.api.key

# Restart to provision new certificate
docker-compose restart galactica-bot.api
```

#### CA Not Initializing

**Problem**: step-ca container keeps restarting.

**Solutions**:
1. Check CA logs:
   ```bash
   docker-compose logs step-ca
   ```

2. Reset CA (WARNING: This will revoke all existing certificates):
   ```bash
   docker-compose down
   docker volume rm galacticabot_step-ca-data
   docker-compose up -d
   ```

#### Certificate Validation Fails

**Problem**: "certificate validation failed" errors in logs.

**Solutions**:
1. Ensure root CA certificate is present and correct:
   ```bash
   docker-compose exec galactica-bot cat /app/certs/root_ca.crt
   docker-compose exec galactica-bot.api cat /app/certs/root_ca.crt
   ```

2. Re-download root CA:
   ```bash
   docker-compose exec galactica-bot step ca root /app/certs/root_ca.crt --ca-url https://step-ca:9000 --insecure
   docker-compose restart galactica-bot
   ```

### Debug Mode

Enable verbose logging:

```bash
# Set in docker-compose.yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development  # For API
  - DOTNET_ENVIRONMENT=Development      # For Bot
```

View detailed mTLS logs:
```bash
docker-compose logs -f galactica-bot.api | grep -i "certificate"
docker-compose logs -f galactica-bot | grep -i "certificate"
```

## Security Best Practices

### Production Deployment

1. **Change default passwords**:
   - Update `CA_PASSWORD` in `.env` to a strong, random password
   - Store securely (e.g., in a secrets manager)

2. **Secure CA access**:
   - Limit network access to step-ca (use firewall rules)
   - Don't expose port 9000 publicly
   - Use Kubernetes secrets or Docker secrets for sensitive data

3. **Certificate lifetime**:
   - Default: 90 days (balance between security and operational overhead)
   - Can be adjusted in provisioning scripts with `--not-after` flag
   - Shorter lifetimes = more secure but more frequent renewals

4. **Backup CA data**:
   ```bash
   # Backup CA volume
   docker run --rm -v galacticabot_step-ca-data:/data -v $(pwd):/backup alpine tar czf /backup/step-ca-backup.tar.gz /data
   
   # Restore CA volume
   docker run --rm -v galacticabot_step-ca-data:/data -v $(pwd):/backup alpine tar xzf /backup/step-ca-backup.tar.gz -C /
   ```

5. **Monitor and audit**:
   - Log all certificate operations
   - Set up alerts for expiration
   - Regularly review issued certificates

### Network Security

When deployed, consider:

- **Network isolation**: Place step-ca in a separate network
- **mTLS for CA**: Configure step-ca itself with mTLS
- **API Gateway**: Use a reverse proxy with additional security layers
- **Rate limiting**: Protect certificate issuance endpoints

## Migration from HTTP to HTTPS

If you have an existing deployment without mTLS:

1. **Deploy step-ca**:
   ```bash
   docker-compose up -d step-ca
   ```

2. **Update services one at a time**:
   ```bash
   # Update and restart API first
   docker-compose up -d galactica-bot.api
   
   # Then update Bot
   docker-compose up -d galactica-bot
   ```

3. **Verify connectivity**:
   ```bash
   docker-compose logs galactica-bot | grep "Connected to BotConfigHub"
   ```

## Advanced Configuration

### Custom Certificate SANs

Edit `step-ca/scripts/provision-api-cert.sh` to add custom Subject Alternative Names:

```bash
step ca certificate "$SERVICE_NAME" \
    "$CERT_PATH/$SERVICE_NAME.crt" \
    "$CERT_PATH/$SERVICE_NAME.key" \
    --san "$SERVICE_NAME" \
    --san "galactica-bot.api" \
    --san "api.example.com" \      # Add custom SAN
    --san "192.168.1.100" \        # Add IP SAN
    ...
```

### Certificate Templates

For more advanced certificate templates, configure step-ca's `ca.json` directly:

```json
{
  "authority": {
    "claims": {
      "minTLSCertDuration": "5m",
      "maxTLSCertDuration": "8760h",
      "defaultTLSCertDuration": "2160h"
    }
  }
}
```

### External CA Integration

To use an external CA instead of step-ca:

1. Obtain certificates from your CA
2. Place them in `/app/certs` in each container
3. Set `MTLS_ENABLED=false` to skip step-ca provisioning
4. Mount certificates as read-only volumes

## References

- [Smallstep CA Documentation](https://smallstep.com/docs/step-ca/)
- [step-ca on GitHub](https://github.com/smallstep/certificates)
- [step CLI Documentation](https://smallstep.com/docs/step-cli/)
- [mTLS Best Practices](https://smallstep.com/blog/everything-you-should-know-about-mtls/)

## Support

For issues or questions:

1. Check the troubleshooting section above
2. Review logs: `docker-compose logs`
3. Consult step-ca documentation
4. Open an issue in the repository with detailed logs and reproduction steps
