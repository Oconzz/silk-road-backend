# NMJ Backend API

A .NET 8 backend API built with Entity Framework Core, SQL Server, and JWT authentication.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Environment Setup](#environment-setup)
- [Database Migrations](#database-migrations)
- [Configuration](#configuration)
- [Security Headers](#security-headers)
- [Custom Headers](#custom-headers)
- [Running the Application](#running-the-application)
- [API Documentation](#api-documentation)

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express, Developer, or full version)
- A code editor (Visual Studio 2022, VS Code, or Rider)

## Environment Setup

### 1. Environment Variables

The application uses environment variables to determine which database connection string to use. Set the following environment variable:

**Windows (PowerShell):**
```powershell
$env:ConnectionString = "LocalConnectionString"
```

**Windows (Command Prompt):**
```cmd
set ConnectionString=LocalConnectionString
```

**Linux/Mac:**
```bash
export ConnectionString=LocalConnectionString
```

### 2. Available Environment Values

- `LocalConnectionString` - For local development
- `StagingConnectionString` - For staging environment
- `ProductionConnectionString` - For production environment

### 3. Configuration Files

Edit `APIServer/appsettings.json` to configure your database connections:

```json
{
  "ConnectionStrings": {
    "LocalConnectionStringdbUser": "your_username",
    "LocalConnectionStringdbPass": "your_password",
    "LocalConnectionStringdbName": "your_database_name",
    "LocalConnectionStringdbServer": "your_server_name"
  }
}
```

**Example for Local SQL Server:**
```json
{
  "ConnectionStrings": {
    "LocalConnectionStringdbUser": "sa",
    "LocalConnectionStringdbPass": "YourPassword123!",
    "LocalConnectionStringdbName": "nmj",
    "LocalConnectionStringdbServer": "localhost\\SQLEXPRESS"
  }
}
```

## Database Migrations

### Creating a New Migration

When you make changes to your Entity Framework models, create a new migration:

```bash
# Navigate to the APIServer project directory
cd APIServer

# Create a new migration
dotnet ef migrations add YourMigrationName

# Example:
dotnet ef migrations add AddNewUserFields
```

### Applying Migrations

**Apply migrations to update your database:**

```bash
# Apply all pending migrations
dotnet ef database update

# Apply to a specific migration
dotnet ef database update YourMigrationName

# Rollback to previous migration
dotnet ef database update PreviousMigrationName
```

### Removing the Last Migration

If you need to remove a migration that hasn't been applied yet:

```bash
dotnet ef migrations remove
```

### Viewing Migration History

```bash
# List all migrations
dotnet ef migrations list

# View SQL script for a migration
dotnet ef migrations script

# View SQL for specific migration range
dotnet ef migrations script FromMigration ToMigration
```

### Common Migration Scenarios

**Initial Setup:**
```bash
cd APIServer
dotnet ef database update
```

**After pulling changes with new migrations:**
```bash
cd APIServer
dotnet ef database update
```

**Reset database (WARNING: This will delete all data):**
```bash
dotnet ef database drop
dotnet ef database update
```

## Configuration

### JWT Authentication

Set your JWT secret in `appsettings.json`:

```json
{
  "Secret": "your-very-long-secret-key-at-least-64-characters-long"
}
```

### Third-Party Services

Configure the following services in `appsettings.json`:

**MailGun (Email Service):**
```json
{
  "MailGun": {
    "ApiUrl": "https://api.mailgun.net/v3/yourdomain/messages",
    "ApiKey": "your-mailgun-api-key",
    "DefaultSender": "noreply@yourdomain.com"
  }
}
```

**AWS Settings:**
```json
{
  "AwsSettings": {
    "AWS_ACCESS_KEY": "your-access-key",
    "AWS_SECRET_KEY": "your-secret-key",
    "S3PublicBucket": "your-bucket-name"
  }
}
```

**OneSignal (Push Notifications):**
```json
{
  "OneSignal": {
    "AppId": "your-onesignal-app-id",
    "ApiKey": "your-onesignal-api-key"
  }
}
```

## Security Headers

The application implements comprehensive security headers through the `SecurityHeadersMiddleware`. This middleware automatically adds the following security headers to all responses:

### Implemented Security Headers

| Header | Value | Purpose |
|--------|-------|---------|
| `Content-Security-Policy` | `default-src 'self'` | Prevents XSS attacks by controlling resource loading |
| `X-Content-Type-Options` | `nosniff` | Prevents MIME type sniffing |
| `X-Frame-Options` | `SAMEORIGIN` | Prevents clickjacking attacks |
| `X-XSS-Protection` | `1; mode=block` | Enables browser XSS protection |
| `Cache-Control` | `no-store, no-cache, must-revalidate` | Prevents sensitive data caching |
| `Pragma` | `no-cache` | HTTP 1.0 cache prevention |
| `Expires` | `0` | Ensures content expiration |

### CORS Configuration

The middleware handles CORS for allowed origins:

**Allowed Origins:**
- `http://localhost:5192`
- `http://localhost:5173`
- `http://localhost:3000`

**Allowed Headers:**
- `Content-Type`
- `Authorization`
- `X-Auth-Security` (Custom security header)
- `X-Company-Id` (Custom company context header)

**Allowed Methods:**
- `POST`, `GET`, `PUT`, `PATCH`, `DELETE`, `OPTIONS`

### Modifying Security Headers

To customize security headers, edit `Business/Middleware/SecurityHeadersMiddleware.cs`:

```csharp
// Example: Adding a custom security header
if (!context.Response.Headers.ContainsKey("X-Custom-Header"))
    context.Response.Headers.Append("X-Custom-Header", new StringValues("your-value"));
```

## Custom Headers

The API uses custom headers for authentication and company context:

### X-Auth-Security Header

**Purpose:** Additional security layer for API authentication

**Usage:** Include in all API requests (optional but recommended)

```bash
curl -H "X-Auth-Security: your-security-token" \
     -H "Authorization: Bearer your-jwt-token" \
     https://api.yourdomain.com/api/endpoint
```

### X-Company-Id Header

**Purpose:** Provides company context for multi-tenant operations

**Required:** For endpoints that need company-scoped data

**Format:** GUID/UUID

**Usage:**

```bash
curl -H "Authorization: Bearer your-jwt-token" \
     -H "X-Company-Id: 12345678-1234-1234-1234-123456789abc" \
     https://api.yourdomain.com/api/company-specific-endpoint
```

### Swagger Integration

Both custom headers are automatically included in the Swagger UI:

1. **X-Auth-Security** - Added to all endpoints
2. **X-Company-Id** - Added only to endpoints requiring company context

The `AddCustomHeaderOperationFilter` service automatically detects which endpoints require the `X-Company-Id` header based on the `ServiceProviderFilterFactory` attribute.

### Implementing Custom Headers in Controllers

**For endpoints requiring company context:**

```csharp
[HttpGet]
[TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { "read", "resource", true })]
public IActionResult GetCompanyData()
{
    // Access company ID from HttpContext
    var companyId = HttpContext.Items["CompanyId"]?.ToString();
    // Your logic here
}
```

**For endpoints without company context:**

```csharp
[HttpGet]
[TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { "read", "resource", false })]
public IActionResult GetGlobalData()
{
    // No company context required
}
```

## Running the Application

### Development Mode

```bash
# Navigate to the APIServer directory
cd APIServer

# Run the application
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:5001/swagger`

### Production Mode

```bash
# Build the application
dotnet build --configuration Release

# Run in production mode
dotnet run --configuration Release
```

### Using Docker (if applicable)

```bash
docker build -t nmj-backend .
docker run -p 5000:5000 -e ConnectionString=ProductionConnectionString nmj-backend
```

## API Documentation

### Swagger/OpenAPI

Access the interactive API documentation at `/swagger` when running the application.

**Local Development:** `https://localhost:5001/swagger`

### Authentication

Most endpoints require JWT authentication. To authenticate:

1. Call the login endpoint to receive a JWT token
2. Include the token in subsequent requests:
   ```
   Authorization: Bearer {your-jwt-token}
   ```

### Example API Call

```bash
# Login
curl -X POST https://localhost:5001/api/authorization/login \
  -H "Content-Type: application/json" \
  -d '{"username":"user","password":"pass"}'

# Authenticated request
curl -X GET https://localhost:5001/api/products \
  -H "Authorization: Bearer {token}" \
  -H "X-Company-Id: {company-guid}"
```

## Project Structure

```
backend/
├── APIServer/          # Web API project
│   ├── Controllers/    # API controllers
│   ├── Migrations/     # EF Core migrations
│   ├── Services/       # Application services
│   └── Program.cs      # Application entry point
├── Business/           # Business logic layer
│   ├── Data/           # DbContext and data access
│   ├── Handler/        # Business handlers
│   ├── Middleware/     # Custom middleware
│   ├── Model/          # Entity models
│   ├── Response/       # Response models
│   ├── Services/       # Business services
│   ├── Utilities/      # Utility classes
│   └── ViewModel/      # View models
└── README.md
```

## Troubleshooting

### Migration Issues

**Error: "Unable to create an object of type 'DBContext'"**
```bash
# Ensure you're in the APIServer directory
cd APIServer
dotnet ef database update
```

**Error: "A connection was successfully established with the server, but then an error occurred during the login process"**
- Verify your connection string credentials
- Check SQL Server is running
- Ensure TCP/IP is enabled in SQL Server Configuration Manager

### Connection String Issues

If the application can't connect to the database:

1. Verify the `ConnectionString` environment variable is set
2. Check credentials in `appsettings.json`
3. Ensure SQL Server is running and accessible
4. Test connection using SQL Server Management Studio

### Port Already in Use

If ports 5000/5001 are in use:
```bash
# Change ports in launchSettings.json or
dotnet run --urls "https://localhost:5101;http://localhost:5100"
```

## Contributing

1. Create a feature branch from `main`
2. Make your changes
3. Create migrations if needed: `dotnet ef migrations add YourChanges`
4. Test your changes locally
5. Submit a merge request

## License

[Specify your license here]

## Support

For issues or questions, contact the development team or create an issue in the repository.
