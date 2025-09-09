# Prague MCP Server Containerized PowerShell Deployment Script
# Usage: .\deploy-container.ps1 [-ServerHost "zdeneknovak.one"] [-DomainName "zdeneknovak.one"]

param(
    [string]$ServerHost = "zdeneknovak.one",
    [string]$DomainName = "zdeneknovak.one",
    [string]$SshUser = "root",
    [string]$ContainerName = "praguemcp",
    [string]$ImageName = "praguemcp-server",
    [string]$ImageTag = "latest"
)

$ErrorActionPreference = "Stop"

# Configuration
$AppUser = "praguemcp"
$AppDir = "/var/www/praguemcp"
$ServiceName = "praguemcp-container"
$SshConnection = "$SshUser@$ServerHost"

Write-Host "🚀 Starting Prague MCP Server containerized deployment..." -ForegroundColor Green
Write-Host "Server: $ServerHost" -ForegroundColor Cyan
Write-Host "Domain: $DomainName" -ForegroundColor Cyan
Write-Host "Container: ${ImageName}:$ImageTag" -ForegroundColor Cyan

# Function to execute SSH commands
function Invoke-SshCommand {
    param([string]$Command)
    Write-Host "Executing: $Command" -ForegroundColor Yellow
    ssh $SshConnection $Command
    if ($LASTEXITCODE -ne 0) {
        throw "SSH command failed: $Command"
    }
}

# Function to copy files via SCP
function Copy-FileToServer {
    param([string]$LocalPath, [string]$RemotePath)
    Write-Host "Copying $LocalPath to $RemotePath" -ForegroundColor Yellow
    scp $LocalPath "${SshConnection}:$RemotePath"
    if ($LASTEXITCODE -ne 0) {
        throw "SCP failed: $LocalPath -> $RemotePath"
    }
}

try {
    Write-Host "🐳 Building Docker container..." -ForegroundColor Green
    
    # Build Docker image
    docker build -t "${ImageName}:${ImageTag}" .
    if ($LASTEXITCODE -ne 0) {
        throw "Docker build failed"
    }

    Write-Host "📦 Exporting container image..." -ForegroundColor Green
    
    # Export Docker image to tar file
    $imageFile = "${ImageName}-${ImageTag}.tar"
    docker save -o $imageFile "${ImageName}:${ImageTag}"
    if ($LASTEXITCODE -ne 0) {
        throw "Docker save failed"
    }

    Write-Host "🔧 Setting up server environment..." -ForegroundColor Green

    # Update system packages
    Invoke-SshCommand "apt update && apt upgrade -y"

    # Install dependencies including Podman
    Invoke-SshCommand "apt install -y nginx ufw fail2ban wget curl podman"

    # Create application user
    Write-Host "Creating application user..." -ForegroundColor Green
    Invoke-SshCommand @"
if ! id '$AppUser' &>/dev/null; then
    useradd -r -s /bin/false $AppUser
fi
"@

    # Create application directory for configuration
    Invoke-SshCommand "mkdir -p $AppDir"

    Write-Host "📤 Uploading container image..." -ForegroundColor Green
    Copy-FileToServer $imageFile "/tmp/"

    Write-Host "🔄 Stopping existing container..." -ForegroundColor Green
    Invoke-SshCommand "systemctl stop $ServiceName || true"
    Invoke-SshCommand "podman stop $ContainerName || true"
    Invoke-SshCommand "podman rm $ContainerName || true"

    Write-Host "📋 Loading container image..." -ForegroundColor Green
    Invoke-SshCommand "podman load -i /tmp/$imageFile"
    Invoke-SshCommand "rm /tmp/$imageFile"

    Write-Host "📝 Creating production environment file..." -ForegroundColor Green
    $envContent = @"
# Prague MCP Server Production Configuration
GOLEMIO_API_TOKEN=your_actual_token_here
ASPNETCORE_URLS=http://+:5093
ASPNETCORE_ENVIRONMENT=Production
"@
    $envContent | Out-File -FilePath "temp.env" -Encoding UTF8
    Copy-FileToServer "temp.env" "$AppDir/.env"
    Remove-Item "temp.env"
    Invoke-SshCommand "chown ${AppUser}:${AppUser} $AppDir/.env"
    Invoke-SshCommand "chmod 600 $AppDir/.env"

    Write-Host "⚙️ Configuring Podman systemd service..." -ForegroundColor Green
    Copy-FileToServer "deployment/praguemcp-container.service" "/etc/systemd/system/$ServiceName.service"
    Invoke-SshCommand "systemctl daemon-reload"
    Invoke-SshCommand "systemctl enable $ServiceName"

    Write-Host "🌐 Configuring Nginx..." -ForegroundColor Green
    Copy-FileToServer "deployment/nginx-praguemcp-container" "/etc/nginx/sites-available/praguemcp"
    Invoke-SshCommand "sed -i 's/your-domain.com/$DomainName/g' /etc/nginx/sites-available/praguemcp"
    Invoke-SshCommand "ln -sf /etc/nginx/sites-available/praguemcp /etc/nginx/sites-enabled/"
    Invoke-SshCommand "rm -f /etc/nginx/sites-enabled/default"
    Invoke-SshCommand "nginx -t"

    Write-Host "🚀 Starting services..." -ForegroundColor Green
    Invoke-SshCommand "systemctl start $ServiceName"
    Invoke-SshCommand "systemctl reload nginx"

    Write-Host "🔒 Setting up SSL certificate..." -ForegroundColor Green
    # Install Certbot
    Invoke-SshCommand "apt install -y certbot python3-certbot-nginx"
    
    # Get SSL certificate
    Write-Host "Obtaining SSL certificate for $DomainName..." -ForegroundColor Green
    Invoke-SshCommand "certbot --nginx -d $DomainName --non-interactive --agree-tos --email admin@$DomainName"

    Write-Host "✅ Containerized deployment completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Next steps:" -ForegroundColor Cyan
    Write-Host "1. Edit the environment file: ssh $SshConnection 'nano $AppDir/.env'" -ForegroundColor White
    Write-Host "2. Add your actual Golemio API token to the GOLEMIO_API_TOKEN variable" -ForegroundColor White
    Write-Host "3. Restart the service: ssh $SshConnection 'systemctl restart $ServiceName'" -ForegroundColor White
    Write-Host "4. Check service status: ssh $SshConnection 'systemctl status $ServiceName'" -ForegroundColor White
    Write-Host "5. View container logs: ssh $SshConnection 'podman logs $ContainerName'" -ForegroundColor White
    Write-Host "6. View service logs: ssh $SshConnection 'journalctl -u $ServiceName -f'" -ForegroundColor White
    Write-Host ""
    Write-Host "🌍 Your MCP server will be accessible at: https://$DomainName/api/mcp" -ForegroundColor Green
    Write-Host "🔧 Container management: ssh $SshConnection" -ForegroundColor Cyan

} catch {
    Write-Host "❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # Cleanup
    if (Test-Path $imageFile) {
        Remove-Item $imageFile
    }
}

Write-Host "🎉 Containerized deployment script completed!" -ForegroundColor Green
