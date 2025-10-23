#!/bin/bash
set -e

# step-ca initialization script
# This script initializes the certificate authority on first run

CA_NAME="${CA_NAME:-GalacticaBot CA}"
CA_DNS="${CA_DNS:-step-ca}"
CA_ADDRESS="${CA_ADDRESS:-:9000}"
CA_PROVISIONER="${CA_PROVISIONER:-admin}"
CA_PASSWORD="${CA_PASSWORD:-changeme}"

# Check if already initialized
if [ -f "/home/step/config/ca.json" ]; then
    echo "Certificate Authority already initialized"
    exec step-ca /home/step/config/ca.json --password-file /home/step/secrets/password
fi

echo "Initializing Certificate Authority..."

# Initialize the CA
echo "$CA_PASSWORD" > /home/step/secrets/password

step ca init \
    --name="$CA_NAME" \
    --dns="$CA_DNS" \
    --address="$CA_ADDRESS" \
    --provisioner="$CA_PROVISIONER" \
    --password-file=/home/step/secrets/password \
    --provisioner-password-file=/home/step/secrets/password \
    --deployment-type=standalone

echo "Certificate Authority initialized successfully"

# Start the CA
exec step-ca /home/step/config/ca.json --password-file /home/step/secrets/password
