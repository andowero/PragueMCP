# Prague MCP Server Dockerfile
# Multi-stage build for optimized production image

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY *.csproj ./
COPY *.sln ./

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish the application
RUN dotnet publish -c Release -o /app/publish --self-contained false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Create non-root user for security
RUN groupadd -r praguemcp && useradd -r -g praguemcp praguemcp

# Set working directory
WORKDIR /app

# Create logs directory with proper permissions
RUN mkdir -p /app/logs && chown -R praguemcp:praguemcp /app/logs

# Copy published application
COPY --from=build /app/publish .

# Copy configuration files
COPY appsettings.json ./
COPY appsettings.Production.json ./

# Set proper ownership
RUN chown -R praguemcp:praguemcp /app

# Switch to non-root user
USER praguemcp

# Expose port
EXPOSE 5093

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5093
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5093/api/mcp || exit 1

# Entry point
ENTRYPOINT ["dotnet", "PragueMCP.dll"]
