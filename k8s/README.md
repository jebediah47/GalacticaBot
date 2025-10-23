# Kubernetes Deployment Guide

This directory contains Kubernetes manifests for deploying GalacticaBot with mTLS using step-ca.

## Prerequisites

- Kubernetes cluster (1.24+)
- `kubectl` configured to access your cluster
- PostgreSQL database (can be deployed separately or use external service)
- Docker images built and pushed to a container registry

## Quick Start

1. **Update the secrets** in `deployment.yaml`:
   ```bash
   # Edit the secrets section and replace with your actual values
   kubectl create secret generic galacticabot-secrets \
     --from-literal=database-url='postgresql://user:password@postgres:5432/galacticabot' \
     --from-literal=discord-token='your_discord_bot_token' \
     --from-literal=ca-password='your_strong_ca_password' \
     -n galacticabot
   ```

2. **Deploy the application**:
   ```bash
   kubectl apply -f deployment.yaml
   ```

3. **Verify deployment**:
   ```bash
   # Check all pods are running
   kubectl get pods -n galacticabot
   
   # Check logs
   kubectl logs -n galacticabot -l app=step-ca
   kubectl logs -n galacticabot -l app=galactica-bot-api
   kubectl logs -n galacticabot -l app=galactica-bot
   ```

## Architecture

The deployment consists of:

1. **step-ca**: Certificate Authority (1 replica)
   - Persistent volume for CA data
   - Service exposed internally at `step-ca.galacticabot.svc.cluster.local:9000`

2. **galactica-bot-api**: API service (1 replica)
   - Obtains server certificate from step-ca
   - Requires client certificates for mTLS
   - Service exposed at `galactica-bot-api.galacticabot.svc.cluster.local:8443`

3. **galactica-bot**: Discord bot (1 replica)
   - Obtains client certificate from step-ca
   - Connects to API using mTLS

## Configuration

### Secrets

The deployment uses a Kubernetes Secret named `galacticabot-secrets` containing:

- `database-url`: PostgreSQL connection string
- `discord-token`: Discord bot token
- `ca-password`: Step-CA provisioner password

**Important**: Always use strong, randomly generated passwords in production!

### Environment Variables

Key environment variables:

**step-ca**:
- `CA_NAME`: Name of the Certificate Authority
- `CA_DNS`: DNS name for the CA service
- `CA_PROVISIONER`: Provisioner name (default: admin)
- `CA_PASSWORD`: Provisioner password

**galactica-bot-api**:
- `MTLS_ENABLED`: Enable mTLS validation (default: true)
- `CA_URL`: URL of step-ca service
- `SERVICE_NAME`: Name for certificate issuance
- `CERT_PATH`: Path where certificates are stored

**galactica-bot**:
- `BOT_API_URL`: HTTPS URL of the API service
- `CA_URL`: URL of step-ca service
- `SERVICE_NAME`: Name for certificate issuance
- `CERT_PATH`: Path where certificates are stored

## Scaling

### Horizontal Scaling

To scale the application:

```bash
# Scale API replicas
kubectl scale deployment galactica-bot-api -n galacticabot --replicas=3

# Note: Bot should remain at 1 replica (Discord limitation)
```

**Note**: The step-ca deployment should remain at 1 replica unless you configure step-ca for high availability.

### Resource Limits

Add resource limits to prevent resource exhaustion:

```yaml
resources:
  requests:
    memory: "128Mi"
    cpu: "100m"
  limits:
    memory: "512Mi"
    cpu: "500m"
```

## Monitoring

### Certificate Expiry Monitoring

Set up monitoring for certificate expiration:

```bash
# Check certificate expiry in API pod
kubectl exec -n galacticabot deployment/galactica-bot-api -- \
  step certificate inspect /app/certs/galactica-bot.api.crt --format json | \
  jq -r '.validity.end'

# Check certificate expiry in Bot pod
kubectl exec -n galacticabot deployment/galactica-bot -- \
  step certificate inspect /app/certs/galactica-bot.crt --format json | \
  jq -r '.validity.end'
```

### Prometheus Integration

Example ServiceMonitor for Prometheus Operator:

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: galactica-bot-api
  namespace: galacticabot
spec:
  selector:
    matchLabels:
      app: galactica-bot-api
  endpoints:
  - port: https
    scheme: https
    tlsConfig:
      insecureSkipVerify: true  # Configure properly in production
```

## Security Best Practices

### 1. Use Kubernetes Secrets Properly

Store secrets in a secure secret management system:

```bash
# Using Sealed Secrets
kubeseal --format=yaml < secrets.yaml > sealed-secrets.yaml

# Using External Secrets Operator
# Configure to sync from Vault, AWS Secrets Manager, etc.
```

### 2. Network Policies

Implement network policies to restrict traffic:

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: galactica-bot-api-policy
  namespace: galacticabot
spec:
  podSelector:
    matchLabels:
      app: galactica-bot-api
  policyTypes:
  - Ingress
  ingress:
  - from:
    - podSelector:
        matchLabels:
          app: galactica-bot
    ports:
    - protocol: TCP
      port: 8443
```

### 3. Pod Security Standards

Apply Pod Security Standards:

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: galacticabot
  labels:
    pod-security.kubernetes.io/enforce: restricted
    pod-security.kubernetes.io/audit: restricted
    pod-security.kubernetes.io/warn: restricted
```

### 4. RBAC

Limit service account permissions:

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: galactica-bot
  namespace: galacticabot
---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: galactica-bot-role
  namespace: galacticabot
rules:
- apiGroups: [""]
  resources: ["secrets"]
  verbs: ["get"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: galactica-bot-binding
  namespace: galacticabot
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: Role
  name: galactica-bot-role
subjects:
- kind: ServiceAccount
  name: galactica-bot
  namespace: galacticabot
```

## Troubleshooting

### Pods Not Starting

```bash
# Check pod status
kubectl get pods -n galacticabot

# View pod events
kubectl describe pod -n galacticabot <pod-name>

# Check logs
kubectl logs -n galacticabot <pod-name> --previous
```

### Certificate Issues

```bash
# Check if step-ca is healthy
kubectl exec -n galacticabot deployment/step-ca -- step ca health

# View certificate provisioning logs
kubectl logs -n galacticabot deployment/galactica-bot-api -c api

# Manual certificate check
kubectl exec -n galacticabot deployment/galactica-bot-api -- \
  ls -la /app/certs/
```

### Connection Issues

```bash
# Test network connectivity from bot to API
kubectl exec -n galacticabot deployment/galactica-bot -- \
  nc -zv galactica-bot-api 8443

# Test CA connectivity
kubectl exec -n galacticabot deployment/galactica-bot -- \
  nc -zv step-ca 9000
```

## Backup and Restore

### Backup step-ca Data

```bash
# Create a backup of the step-ca persistent volume
kubectl exec -n galacticabot deployment/step-ca -- \
  tar czf /tmp/step-ca-backup.tar.gz /home/step

kubectl cp galacticabot/step-ca-<pod-id>:/tmp/step-ca-backup.tar.gz \
  ./step-ca-backup.tar.gz
```

### Restore step-ca Data

```bash
# Copy backup to pod
kubectl cp ./step-ca-backup.tar.gz \
  galacticabot/step-ca-<pod-id>:/tmp/step-ca-backup.tar.gz

# Restore from backup
kubectl exec -n galacticabot deployment/step-ca -- \
  tar xzf /tmp/step-ca-backup.tar.gz -C /
```

## High Availability

For production deployments with high availability requirements:

1. **Database**: Use managed PostgreSQL with replication
2. **step-ca**: Configure step-ca in HA mode with multiple replicas
3. **API**: Deploy multiple replicas with a load balancer
4. **Bot**: Keep at 1 replica (Discord bot limitation)

## Upgrading

To upgrade the application:

```bash
# Update image tags in deployment.yaml
# Apply changes
kubectl apply -f deployment.yaml

# Monitor rollout
kubectl rollout status deployment/galactica-bot-api -n galacticabot
kubectl rollout status deployment/galactica-bot -n galacticabot

# Rollback if needed
kubectl rollout undo deployment/galactica-bot-api -n galacticabot
```

## Clean Up

To remove the deployment:

```bash
# Delete all resources
kubectl delete namespace galacticabot

# Or delete specific components
kubectl delete -f deployment.yaml
```

## Additional Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Smallstep Kubernetes Guide](https://smallstep.com/docs/tutorials/kubernetes-acme-ca)
- [Cert-Manager Integration](https://cert-manager.io/docs/)
