# SAMA - Service Availability Monitoring and Alerting

A modern, containerizable .NET 10 uptime monitoring and alerting system built with simplicity and reliability in mind.

## Overview

SAMA is a comprehensive service availability monitoring solution that helps you:
- Monitor HTTP/HTTPS endpoints, TCP ports, DNS, TLS certificates, and more
- Receive alerts via Email, Slack, Teams, Discord, and custom scripts
- Track uptime metrics and history
- Visualize response times and service health in real-time

## Features

### Core Monitoring
- **Multiple Check Types**: HTTP/HTTPS, TCP, ICMP Ping, DNS, TLS certificates, Custom Scripts
- **Flexible Scheduling**: Per-check intervals from seconds to hours
- **Traffic Light Status**: Up (healthy), Warn (warning), Down (failed)
- **Configurable Thresholds**: Require N consecutive failures before alerting

### Alerting
- **Reusable Channels**: Define notification channels once, use across multiple checks
- **Multiple Channels**: Email, Slack, Microsoft Teams, Discord, custom scripts, Azure Event Grid
- **Flexible Alerts**: Trigger on Warn/Down status with consecutive failure thresholds
- **Recovery Notifications**: Optional automatic notifications when services recover
- **Lifecycle Events**: Subscribe to check creation, updates, deletion, and status changes
- **External Integrations**: Send events to Azure Event Grid or custom scripts for workflow automation

### User Interface
- **Real-time Dashboard**: Live status grid with HTMX updates
- **Response Time Charts**: Visualize performance trends with Chart.js
- **Check Management**: Intuitive interface for checks, alerts, channels, and workspaces
- **User Management**: Role-based access control

### Future Enhancements (Phase 2+)
- **Geo-Distributed Agents**: Run checks from multiple regions  
- **Advanced Check Types**: Playwright-based browser automation, Database connectivity checks
- **Enterprise SSO**: OIDC and SAML authentication

### Security & Compliance
- **Encrypted Configuration**: AES-256 encryption for sensitive data
- **Role-Based Access**: Global Admin role, workspace-scoped Editor/Viewer roles, and Guest access
- **Audit Logging**: Track all configuration changes (Phase 2+)
- **Multiple Auth Methods**: Local accounts with passkeys, enterprise SSO (Phase 2+)

## Technology Stack

- **.NET 10**: Latest cross-platform ASP.NET Core framework
- **Razor Pages + HTMX**: Simple, server-side rendering with dynamic updates
- **Entity Framework Core**: PostgreSQL database
- **Quartz.NET**: Robust background job scheduling
- **Chart.js**: Beautiful response time visualizations
- **Serilog**: Structured logging

## Quick Start

### Prerequisites

- Docker
- Docker Compose (optional, for easier containerized deployment)

### Docker Deployment

1. **Set the encryption key**
   
   Create a `.env` file in the project root with a secure encryption key:
   ```powershell
   # Generate a random key (PowerShell)
   $key = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object {[char]$_})
   "SAMA_ENCRYPTION_KEY=$key" | Out-File -FilePath .env -Encoding utf8
   ```
   
   ```bash
   # Generate a random key (Bash)
   echo "SAMA_ENCRYPTION_KEY=$(openssl rand -base64 32)" > .env
   ```
   
   Or set it manually in the `.env` file:
   ```
   SAMA_ENCRYPTION_KEY=your-secure-key-here
   ```

2. **Build and run with Docker Compose**
   ```powershell
   docker-compose up -d
   ```

3. **Complete initial setup**
   
   Navigate to `http://localhost:8080` and follow the setup wizard to create your administrator account.

## Configuration

### Environment Variables

**For Docker Compose**: Set `SAMA_ENCRYPTION_KEY` in a `.env` file in the project root.

**For Development**: Set `Encryption__Key` in `appsettings.Development.json` or as an environment variable.

| Variable | Description | Default |
|----------|-------------|---------|
| `Encryption__Key` (or `SAMA_ENCRYPTION_KEY` for Docker) | AES-256 key for encrypting sensitive data | *Required* |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Database=sama;Username=sama;Password=sama_dev_password` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |

### Application Settings

Key configuration sections in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sama;Username=sama;Password=yourpassword"
  },
  "Encryption": {
    "Key": "your-secure-encryption-key-goes-here-or-in-environment-variable"
  },
}
```

Additional configuration is managed through the Admin Settings UI.

## Architecture

SAMA follows a simple layered architecture:

```
┌─────────────────────────────────────┐
│    Web Layer (Razor Pages + HTMX)   │
├─────────────────────────────────────┤
│    Service Layer (Business Logic)   │
├─────────────────────────────────────┤
│   Data Layer (EF Core + DbContext)  │
├─────────────────────────────────────┤
│       Database (PostgreSQL)         │
└─────────────────────────────────────┘
```

**Database Migrations**: Automatically applied on application startup. If migrations fail, the application will not start.

**Initial Setup**: On first run, create an administrator account via the setup wizard.

**Note**: Distributed agents will be added in Phase 2.

For detailed architecture documentation, see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Project Structure

```
SAMA/
├── SAMA.Web/                      # Main ASP.NET Core application
│   ├── Pages/                     # Razor Pages
│   ├── Services/                  # Business logic services
│   ├── Models/                    # Domain models
│   ├── BackgroundServices/        # Quartz.NET jobs
│   └── Dockerfile
├── SAMA.Data/                     # Data access layer
├── SAMA.Shared/                   # Shared library (checks, alerts)
├── SAMA.Tests.Unit/               # Unit tests
├── SAMA.Tests.Integration/        # Integration tests
├── SAMA.Tests.System/             # System tests
├── docker-compose.yml
└── docs/
    ├── ARCHITECTURE.md            # Architecture documentation
    ├── DATABASE_SCHEMA.md         # Database schema
    └── ROADMAP.md                 # Development roadmap
```

**Note**: Agent-related code (`SAMA.Agent` project, API Controllers) will be added in Phase 2.

## Development

### Setup

1. **Clone the repository**
   ```powershell
   git clone https://github.com/sep/sama.git
   cd sama
   ```

2. **Start PostgreSQL**
   ```powershell
   docker run -d `
     --name sama-postgres `
     -e POSTGRES_PASSWORD=sama_dev_password `
     -e POSTGRES_USER=sama `
     -e POSTGRES_DB=sama `
     -p 5432:5432 `
     postgres:18
   ```

3. **Update connection string** (optional)
   
   Edit `SAMA.Web/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=sama;Username=sama;Password=sama_dev_password"
     }
   }
   ```

4. **Run the application**
   ```powershell
   cd SAMA.Web
   dotnet run
   ```
   
   **Note**: Database migrations are applied automatically on startup. No manual migration steps required!

5. **Complete initial setup**
   
   Navigate to `http://localhost:5226` and you'll be redirected to the setup page. Create your administrator account:
   - Enter your email address
   - Choose a strong password or passphrase (minimum 14 characters)
   - Confirm your password
   
   **Password Policy**: SAMA encourages passphrases over complex passwords. Use at least 14 characters - consider something memorable like "correct horse battery staple" rather than complex symbols.

   After setup, you can log in and start configuring SAMA.

### Building from Source

```powershell
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run application (migrations applied automatically)
cd SAMA.Web
dotnet run
```

### Running Tests

```powershell
# Unit tests only
dotnet test SAMA.Tests.Unit/

# Integration tests
dotnet test SAMA.Tests.Integration/

# All tests
dotnet test
```

**Testing Framework**:
- **MSTest**: Test runner and assertion library
- **NSubstitute**: Mocking framework for creating test doubles with virtual methods
- Tests follow the Arrange-Act-Assert (AAA) pattern
- Unit tests use mocks to avoid network I/O and external dependencies

### Database Migrations

Migrations are applied automatically on startup, but you can also manage them manually:

```powershell
# Add a new migration
cd SAMA.Web
dotnet ef migrations add MigrationName --project ../SAMA.Data

# View pending migrations
dotnet ef migrations list

# Revert to a specific migration (use with caution)
dotnet ef database update PreviousMigrationName
```

**Production Note**: In production deployments, migrations are applied automatically when the container starts. Ensure your database connection has appropriate permissions to apply schema changes.

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md) for the complete roadmap.

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Coding Standards

- Follow standard C#/.NET conventions
- Write self-documenting code (minimal comments)
- Include appropriate tests for new features
- Update documentation as needed

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

---

[![Powered by SEP logo](https://raw.githubusercontent.com/sep/assets/master/images/powered-by-sep.svg?sanitize=true)](https://www.sep.com)

Built with ❤️ in sunny Indiana
