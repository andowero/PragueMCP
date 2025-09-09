# Prague MCP Server Containerized Deployment Guide

This guide covers deploying the Prague MCP Server using Docker containers to a Hetzner Cloud server running Ubuntu/Debian Linux with Podman.

## Overview

The containerized deployment provides several advantages:
- **Isolation**: Application runs in a secure, isolated container environment
- **Consistency**: Same environment across development and production
- **Security**: Enhanced security through container isolation and read-only filesystem
- **Portability**: Easy to move between different servers or environments
- **Rollback**: Simple rollback to previous versions

## Prerequisites

### Local Machine (Windows)
- Docker Desktop installed and running
- PowerShell with SSH/SCP capabilities (Cygwin recommended)
- SSH certificate-based authentication configured for `root@zdeneknovak.one`

### Target Server
- Hetzner Cloud server (Ubuntu 22.04 LTS recommended)
- Domain name pointing to your server (optional but recommended)
- SSH access configured
- Golemio API token from https://api.golemio.cz/

## Deployment Process

### 1. Automated Deployment

Use the provided PowerShell script for automated deployment:

```powershell
# Navigate to your project directory
cd C:\path\to\PragueMCP

# Run the containerized deployment script
.\deployment\deploy-container.ps1 -ServerHost "zdeneknovak.one" -DomainName "zdeneknovak.one"
```

The script will:
1. Build the Docker container locally
2. Export the container image to a tar file
3. Transfer the image to the server via SCP
4. Install Podman and dependencies on the server
5. Load the container image using Podman
6. Configure systemd service for container management
7. Set up Nginx reverse proxy
8. Configure SSL certificate with Let's Encrypt

### 2. Manual Deployment Steps

If you prefer manual deployment or need to troubleshoot:

#### Step 1: Build Container Locally

```powershell
# Build the Docker image
docker build -t praguemcp-server:latest .

# Export the image
docker save -o praguemcp-server-latest.tar praguemcp-server:latest
```

#### Step 2: Transfer to Server

```powershell
# Copy image to server
scp praguemcp-server-latest.tar root@zdeneknovak.one:/tmp/

# Copy configuration files
scp deployment/praguemcp-container.service root@zdeneknovak.one:/etc/systemd/system/
scp deployment/nginx-praguemcp-container root@zdeneknovak.one:/etc/nginx/sites-available/praguemcp
```

#### Step 3: Server Setup

```bash
# Install Podman and dependencies
apt update && apt upgrade -y
apt install -y nginx ufw fail2ban wget curl podman

# Create application user and directories
useradd -r -s /bin/false praguemcp
mkdir -p /var/www/praguemcp/logs
chown -R praguemcp:praguemcp /var/www/praguemcp

# Load container image
podman load -i /tmp/praguemcp-server-latest.tar
rm /tmp/praguemcp-server-latest.tar
```

#### Step 4: Configure Environment

```bash
# Create environment file
cat > /var/www/praguemcp/.env << EOF
GOLEMIO_API_TOKEN=your_actual_golemio_api_token_here
ASPNETCORE_URLS=http://+:5093
ASPNETCORE_ENVIRONMENT=Production
EOF

# Set proper permissions
chown praguemcp:praguemcp /var/www/praguemcp/.env
chmod 600 /var/www/praguemcp/.env
```

#### Step 5: Start Services

```bash
# Enable and start container service
systemctl daemon-reload
systemctl enable praguemcp-container
systemctl start praguemcp-container

# Configure Nginx
sed -i 's/your-domain.com/zdeneknovak.one/g' /etc/nginx/sites-available/praguemcp
ln -sf /etc/nginx/sites-available/praguemcp /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl reload nginx

# Setup SSL
apt install -y certbot python3-certbot-nginx
certbot --nginx -d zdeneknovak.one --non-interactive --agree-tos --email admin@zdeneknovak.one
```

## Container Architecture

### Security Features
- **Non-root user**: Container runs as `praguemcp` user (UID 1000)
- **Read-only filesystem**: Container filesystem is read-only except for logs
- **Dropped capabilities**: All Linux capabilities dropped except essential ones
- **No new privileges**: Prevents privilege escalation
- **Isolated networking**: Container only exposes port 5093

### Volume Mounts
- `/var/www/praguemcp/logs:/app/logs:Z` - Log files (read-write)
- Environment variables loaded from `/var/www/praguemcp/.env`

### Health Checks
- Built-in health check every 30 seconds
- Checks `/api/mcp` endpoint availability
- Automatic container restart on health check failure

## Management Commands

### Container Management

```bash
# Check container status
systemctl status praguemcp-container

# View container logs
podman logs praguemcp

# View service logs
journalctl -u praguemcp-container -f

# Restart container
systemctl restart praguemcp-container

# Stop container
systemctl stop praguemcp-container

# Manual container operations
podman ps                    # List running containers
podman images               # List available images
podman exec -it praguemcp bash  # Access container shell (if needed)
```

### Application Updates

```bash
# Stop the service
systemctl stop praguemcp-container

# Load new image (after transferring from local machine)
podman load -i /tmp/new-praguemcp-server-latest.tar

# Start the service (will use new image)
systemctl start praguemcp-container
```

### Log Management

```bash
# View application logs
tail -f /var/www/praguemcp/logs/application-*.log

# View container logs
podman logs praguemcp --tail 100

# View systemd service logs
journalctl -u praguemcp-container --tail 100

## Troubleshooting

### Common Issues

#### Container Won't Start
```bash
# Check service status
systemctl status praguemcp-container

# Check container logs
podman logs praguemcp

# Check if image exists
podman images | grep praguemcp-server

# Check if port is available
netstat -tlnp | grep 5093
```

#### Permission Issues
```bash
# Fix log directory permissions
chown -R praguemcp:praguemcp /var/www/praguemcp/logs
chmod 755 /var/www/praguemcp/logs

# Fix environment file permissions
chown praguemcp:praguemcp /var/www/praguemcp/.env
chmod 600 /var/www/praguemcp/.env
```

#### Network Issues
```bash
# Check if container is listening
podman exec praguemcp netstat -tlnp

# Test container connectivity
curl http://localhost:5093/api/mcp

# Check Nginx configuration
nginx -t
systemctl status nginx
```

#### Environment Variables Not Loading
```bash
# Verify environment file exists and has correct content
cat /var/www/praguemcp/.env

# Check if container can read the file
podman exec praguemcp env | grep GOLEMIO
```

### Performance Monitoring

```bash
# Container resource usage
podman stats praguemcp

# System resource usage
htop
df -h
free -h

# Application performance
curl -w "@curl-format.txt" -o /dev/null -s http://localhost:5093/api/mcp
```

### Backup and Recovery

#### Backup
```bash
# Backup environment configuration
cp /var/www/praguemcp/.env /var/www/praguemcp/.env.backup

# Backup logs
tar -czf praguemcp-logs-$(date +%Y%m%d).tar.gz /var/www/praguemcp/logs/

# Export current container image
podman save -o praguemcp-backup-$(date +%Y%m%d).tar praguemcp-server:latest
```

#### Recovery
```bash
# Restore environment configuration
cp /var/www/praguemcp/.env.backup /var/www/praguemcp/.env

# Load backup image
podman load -i praguemcp-backup-YYYYMMDD.tar

# Restart service
systemctl restart praguemcp-container
```

## Security Considerations

### Container Security
- Container runs with minimal privileges
- Read-only filesystem prevents tampering
- No shell access by default
- Isolated network namespace

### Host Security
```bash
# Configure firewall
ufw allow 22/tcp    # SSH
ufw allow 80/tcp    # HTTP
ufw allow 443/tcp   # HTTPS
ufw deny 5093/tcp   # Block direct access to application port
ufw enable

# Configure fail2ban
systemctl enable fail2ban
systemctl start fail2ban
```

### Environment Variables Security
- Store sensitive data in `/var/www/praguemcp/.env`
- Use restrictive file permissions (600)
- Never commit `.env` files to version control
- Consider using secrets management for production

## Comparison with Traditional Deployment

| Aspect | Traditional | Containerized |
|--------|-------------|---------------|
| **Isolation** | Process-level | Container-level |
| **Dependencies** | Host-managed | Container-managed |
| **Security** | Host permissions | Container + host permissions |
| **Updates** | File replacement | Image replacement |
| **Rollback** | Manual backup/restore | Image versioning |
| **Portability** | Host-dependent | Host-independent |
| **Resource Usage** | Lower overhead | Slight overhead |
| **Debugging** | Direct access | Container access |

## Migration from Traditional Deployment

If you're migrating from the traditional deployment:

1. **Backup current deployment**:
   ```bash
   systemctl stop praguemcp
   cp -r /var/www/praguemcp /var/www/praguemcp.backup
   ```

2. **Run containerized deployment**:
   ```powershell
   .\deployment\deploy-container.ps1
   ```

3. **Copy environment configuration**:
   ```bash
   cp /var/www/praguemcp.backup/.env /var/www/praguemcp/.env
   systemctl restart praguemcp-container
   ```

4. **Verify functionality**:
   ```bash
   curl https://zdeneknovak.one/api/mcp
   ```

5. **Remove old deployment** (after verification):
   ```bash
   rm -rf /var/www/praguemcp.backup
   systemctl disable praguemcp
   rm /etc/systemd/system/praguemcp.service
   ```

This completes the containerized deployment setup for your Prague MCP Server on Hetzner Cloud using Podman.
```
