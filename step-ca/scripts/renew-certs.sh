#!/bin/bash
set -e

# Certificate renewal and monitoring script
# This script checks certificate expiry and renews if needed

CERT_PATH="${CERT_PATH:-/app/certs}"
SERVICE_NAME="${SERVICE_NAME:-galactica-bot.api}"
RENEW_THRESHOLD_DAYS="${RENEW_THRESHOLD_DAYS:-7}"
CHECK_INTERVAL_SECONDS="${CHECK_INTERVAL_SECONDS:-86400}"  # Default: 24 hours

echo "Starting certificate renewal monitor for $SERVICE_NAME"
echo "Certificate path: $CERT_PATH"
echo "Renewal threshold: $RENEW_THRESHOLD_DAYS days"
echo "Check interval: $CHECK_INTERVAL_SECONDS seconds"

while true; do
    if [ -f "$CERT_PATH/$SERVICE_NAME.crt" ]; then
        echo "Checking certificate validity..."
        
        # Get certificate expiration date
        EXPIRY=$(step certificate inspect "$CERT_PATH/$SERVICE_NAME.crt" --format json 2>/dev/null | jq -r '.validity.end')
        
        if [ -n "$EXPIRY" ] && [ "$EXPIRY" != "null" ]; then
            EXPIRY_SECONDS=$(date -d "$EXPIRY" +%s)
            NOW_SECONDS=$(date +%s)
            DAYS_LEFT=$(( ($EXPIRY_SECONDS - $NOW_SECONDS) / 86400 ))
            
            echo "Certificate expires in $DAYS_LEFT days (on $EXPIRY)"
            
            if [ $DAYS_LEFT -le $RENEW_THRESHOLD_DAYS ]; then
                echo "Certificate expiring soon (${DAYS_LEFT} days left), triggering renewal..."
                
                # Trigger renewal based on service type
                if [ "$SERVICE_NAME" = "galactica-bot.api" ]; then
                    /app/scripts/provision-api-cert.sh
                else
                    /app/scripts/provision-bot-cert.sh
                fi
                
                echo "Certificate renewed successfully"
            else
                echo "Certificate is valid, no renewal needed"
            fi
        else
            echo "Warning: Could not determine certificate expiration date"
        fi
    else
        echo "Warning: Certificate not found at $CERT_PATH/$SERVICE_NAME.crt"
        echo "Attempting initial provisioning..."
        
        # Trigger initial provisioning
        if [ "$SERVICE_NAME" = "galactica-bot.api" ]; then
            /app/scripts/provision-api-cert.sh
        else
            /app/scripts/provision-bot-cert.sh
        fi
    fi
    
    echo "Next check in $CHECK_INTERVAL_SECONDS seconds..."
    sleep $CHECK_INTERVAL_SECONDS
done
