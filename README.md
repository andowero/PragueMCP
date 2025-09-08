# Prague MCP Server

A Model Context Protocol (MCP) server providing access to Prague city data through the Golemio API.

## Features

- **Air Quality Stations**: Access real-time air quality data from monitoring stations across Prague
- **Air Quality History**: Retrieve historical air quality measurements
- **Bicycle Counters**: Get information about bicycle counting stations
- **Bicycle Counter Detections**: Access bicycle traffic detection data
- **City Districts**: Retrieve Prague city district polygon data

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Golemio API token (get one at https://api.golemio.cz/)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd PragueMCP
   ```

2. **Configure environment variables**
   ```bash
   cp .env.example .env
   # Edit .env and add your Golemio API token
   ```

3. **Build and run**
   ```bash
   dotnet build
   dotnet run
   ```

The server will start on `http://localhost:5093` with the MCP endpoint at `/api/mcp`.

### Environment Configuration

The application supports configuration through:

1. **System environment variables** (highest priority)
2. **`.env` file** (recommended for development)
3. **appsettings.json** (fallback)

#### Required Environment Variables

- `GOLEMIO_API_TOKEN`: Your Golemio API token

#### Optional Environment Variables

- `ASPNETCORE_URLS`: Server URLs (default: `http://localhost:5093`)
- `ASPNETCORE_ENVIRONMENT`: Environment name (Development/Production)

## Production Deployment

### Hetzner Cloud Deployment

For detailed deployment instructions to Hetzner Cloud, see [DEPLOYMENT.md](DEPLOYMENT.md).

#### Quick Deployment

1. **Prepare your server**
   ```bash
   # Run the deployment script
   ./deployment/deploy.sh your-server-ip your-domain.com
   ```

2. **Configure environment variables on server**
   ```bash
   # SSH to your server and edit the environment file
   sudo nano /var/www/praguemcp/.env
   ```

3. **Set up SSL (optional but recommended)**
   ```bash
   sudo certbot --nginx -d your-domain.com
   ```

### Manual Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for comprehensive manual deployment instructions.

## API Documentation

### Available Tools

#### Air Quality Stations
- **get_air_quality_stations**: Retrieve air quality monitoring stations
- **get_air_quality_stations_history**: Get historical air quality data

#### Bicycle Counters
- **get_bicycle_counters**: Get bicycle counting station locations
- **get_bicycle_counter_detections**: Retrieve bicycle traffic detection data

#### City Districts
- **get_city_districts**: Access Prague city district polygon data

### Example Usage

The server implements the Model Context Protocol (MCP) specification. Connect using any MCP-compatible client.

## Development

### Project Structure

```
PragueMCP/
├── Models/              # Data models for API responses
├── Services/            # Business logic and API integration
├── Tools/               # MCP tool implementations
├── deployment/          # Production deployment files
├── .env.example         # Environment variables template
├── appsettings.json     # Application configuration
└── Program.cs           # Application entry point
```

### Building

```bash
# Development build
dotnet build

# Production build
dotnet publish -c Release -o ./publish
```

### Testing

```bash
# Run tests
dotnet test

# Test the MCP endpoint
curl http://localhost:5093/api/mcp
```

## Configuration

### Development Configuration

Create a `.env` file in the project root:

```bash
GOLEMIO_API_TOKEN=your_token_here
ASPNETCORE_URLS=http://localhost:5093
ASPNETCORE_ENVIRONMENT=Development
```

### Production Configuration

For production deployment, set environment variables directly on the server or use the provided systemd service configuration.

## Security

- **Never commit `.env` files** to version control
- **Use environment variables** for sensitive configuration in production
- **Enable HTTPS** in production environments
- **Configure firewall rules** to restrict access to necessary ports only

## Monitoring

### Logs

- **Development**: Console output and file logs in `logs/` directory
- **Production**: Systemd journal (`journalctl -u praguemcp -f`)

### Health Checks

The application includes basic health monitoring through systemd service status.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

[Add your license information here]

## Support

For issues and questions:
- Check the [DEPLOYMENT.md](DEPLOYMENT.md) for deployment-related issues
- Review application logs for troubleshooting
- Ensure your Golemio API token is valid and properly configured
