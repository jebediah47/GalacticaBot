#!/bin/bash
set -e

# Script to provision client certificates for GalacticaBot
# This runs during container startup

CA_URL="${CA_URL:-https://step-ca:9000}"
CA_FINGERPRINT="${CA_FINGERPRINT}"
SERVICE_NAME="${SERVICE_NAME:-galactica-bot}"
CERT_PATH="${CERT_PATH:-/app/certs}"
CA_PASSWORD="${CA_PASSWORD:-changeme}"

# Wait for CA to be ready
echo "Waiting for Certificate Authority at $CA_URL..."
until step ca health --ca-url "$CA_URL" --root /app/certs/root_ca.crt 2>/dev/null; do
    echo "CA not ready yet, retrying in 2 seconds..."
    sleep 2
done

echo "Certificate Authority is ready"

# Check if certificate already exists and is valid
if [ -f "$CERT_PATH/$SERVICE_NAME.crt" ] && [ -f "$CERT_PATH/$SERVICE_NAME.key" ]; then
    echo "Certificate exists, checking validity..."
    if step certificate inspect "$CERT_PATH/$SERVICE_NAME.crt" --short 2>/dev/null | grep -q "valid"; then
        EXPIRY=$(step certificate inspect "$CERT_PATH/$SERVICE_NAME.crt" --format json | jq -r '.validity.end')
        echo "Certificate valid until: $EXPIRY"
        
        # Check if certificate expires in less than 7 days
        EXPIRY_SECONDS=$(date -d "$EXPIRY" +%s)
        NOW_SECONDS=$(date +%s)
        DAYS_LEFT=$(( ($EXPIRY_SECONDS - $NOW_SECONDS) / 86400 ))
        
        if [ $DAYS_LEFT -gt 7 ]; then
            echo "Certificate is valid for $DAYS_LEFT more days, skipping renewal"
            exit 0
        fi
        
        echo "Certificate expires in $DAYS_LEFT days, renewing..."
    fi
fi

echo "Provisioning client certificate for $SERVICE_NAME..."

# Create certificate directory if it doesn't exist
mkdir -p "$CERT_PATH"

# Download root certificate
if [ ! -f "$CERT_PATH/root_ca.crt" ]; then
    echo "Downloading root certificate..."
    step ca root "$CERT_PATH/root_ca.crt" --ca-url "$CA_URL" --fingerprint "$CA_FINGERPRINT" || \
    step ca root "$CERT_PATH/root_ca.crt" --ca-url "$CA_URL" --insecure
fi

# Request client certificate using the provisioner
echo "Requesting client certificate..."
echo "$CA_PASSWORD" | step ca certificate "$SERVICE_NAME" \
    "$CERT_PATH/$SERVICE_NAME.crt" \
    "$CERT_PATH/$SERVICE_NAME.key" \
    --ca-url "$CA_URL" \
    --root "$CERT_PATH/root_ca.crt" \
    --san "$SERVICE_NAME" \
    --provisioner "$CA_PROVISIONER" \
    --provisioner-password-file /dev/stdin \
    --not-after 2160h \
    --force

# Set appropriate permissions
chmod 644 "$CERT_PATH/$SERVICE_NAME.crt"
chmod 600 "$CERT_PATH/$SERVICE_NAME.key"
chmod 644 "$CERT_PATH/root_ca.crt"

echo "Client certificate provisioned successfully for $SERVICE_NAME"
echo "Certificate: $CERT_PATH/$SERVICE_NAME.crt"
echo "Private Key: $CERT_PATH/$SERVICE_NAME.key"
echo "Root CA: $CERT_PATH/root_ca.crt"
