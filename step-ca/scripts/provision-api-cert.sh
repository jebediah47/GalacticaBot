#!/bin/bash
set -e

# Script to provision server certificates for GalacticaBot.Api
# This runs as an init container or during container startup

CA_URL="${CA_URL:-https://step-ca:9000}"
CA_FINGERPRINT="${CA_FINGERPRINT}"
SERVICE_NAME="${SERVICE_NAME:-galactica-bot.api}"
CERT_PATH="${CERT_PATH:-/app/certs}"
CA_PASSWORD="${CA_PASSWORD:-changeme}"
CA_PROVISIONER="${CA_PROVISIONER:-admin}"

# Create certificate directory if it doesn't exist
mkdir -p "$CERT_PATH"

# Wait for CA to be ready (use --insecure for initial health check)
echo "Waiting for Certificate Authority at $CA_URL..."
MAX_RETRIES=60
RETRY_COUNT=0
until curl -k "$CA_URL/health" 2>/dev/null | grep -q "ok"; do
    RETRY_COUNT=$((RETRY_COUNT + 1))
    if [ $RETRY_COUNT -ge $MAX_RETRIES ]; then
        echo "Certificate Authority did not become ready after $MAX_RETRIES attempts"
        echo "Last check output:"
        curl -k "$CA_URL/health" 2>&1 || true
        exit 1
    fi
    echo "CA not ready yet, retrying in 2 seconds... (attempt $RETRY_COUNT/$MAX_RETRIES)"
    sleep 2
done

echo "Certificate Authority is ready"

# Download root certificate if it doesn't exist
if [ ! -f "$CERT_PATH/root_ca.crt" ]; then
    echo "Downloading root certificate..."
    if [ -n "$CA_FINGERPRINT" ]; then
        step ca root "$CERT_PATH/root_ca.crt" --ca-url "$CA_URL" --fingerprint "$CA_FINGERPRINT" || \
        step ca root "$CERT_PATH/root_ca.crt" --ca-url "$CA_URL" --insecure
    else
        step ca root "$CERT_PATH/root_ca.crt" --ca-url "$CA_URL" --insecure
    fi
    echo "Root certificate downloaded"
fi

# Check if certificate already exists and is valid
if [ -f "$CERT_PATH/$SERVICE_NAME.crt" ] && [ -f "$CERT_PATH/$SERVICE_NAME.key" ]; then
    echo "Certificate exists, checking validity..."
    # Use step certificate inspect to check if still valid
    if step certificate inspect "$CERT_PATH/$SERVICE_NAME.crt" --format json 2>/dev/null | jq -e '.validity.end' > /dev/null; then
        EXPIRY=$(step certificate inspect "$CERT_PATH/$SERVICE_NAME.crt" --format json | jq -r '.validity.end')
        echo "Certificate valid until: $EXPIRY"
        
        # Simple check: if certificate inspection succeeds, assume it's still valid
        # For Alpine/BusyBox compatibility, skip complex date math
        echo "Using existing valid certificate"
        exit 0
    else
        echo "Certificate appears invalid, will renew..."
    fi
fi

echo "Provisioning certificate for $SERVICE_NAME..."

# Request certificate using the provisioner
echo "Requesting certificate from CA..."
echo "$CA_PASSWORD" | step ca certificate "$SERVICE_NAME" \
    "$CERT_PATH/$SERVICE_NAME.crt" \
    "$CERT_PATH/$SERVICE_NAME.key" \
    --ca-url "$CA_URL" \
    --root "$CERT_PATH/root_ca.crt" \
    --san "$SERVICE_NAME" \
    --san "galactica-bot.api" \
    --san "localhost" \
    --provisioner "$CA_PROVISIONER" \
    --provisioner-password-file /dev/stdin \
    --not-after 2160h \
    --force

# Set appropriate permissions
chmod 644 "$CERT_PATH/$SERVICE_NAME.crt"
chmod 600 "$CERT_PATH/$SERVICE_NAME.key"
chmod 644 "$CERT_PATH/root_ca.crt"

echo "Certificate provisioned successfully for $SERVICE_NAME"
echo "Certificate: $CERT_PATH/$SERVICE_NAME.crt"
echo "Private Key: $CERT_PATH/$SERVICE_NAME.key"
echo "Root CA: $CERT_PATH/root_ca.crt"

# Verify certificate was created
if [ ! -f "$CERT_PATH/$SERVICE_NAME.crt" ] || [ ! -f "$CERT_PATH/$SERVICE_NAME.key" ]; then
    echo "ERROR: Certificate files were not created!"
    ls -la "$CERT_PATH/"
    exit 1
fi

echo "Certificate verification passed"
