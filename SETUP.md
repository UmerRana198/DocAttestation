# Setup Guide

## Quick Start

### 1. Prerequisites
- .NET 8.0 SDK installed
- SQL Server (or SQL Server Express) running
- Visual Studio 2022 or VS Code with C# extension

### 2. Clone and Navigate
```bash
cd DocAttestation
```

### 3. Update Configuration

Edit `appsettings.json`:

**IMPORTANT**: Generate secure keys before deployment!

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=Attest;User ID=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "JwtSettings": {
    "SecretKey": "GENERATE_A_RANDOM_32_CHAR_MINIMUM_KEY_HERE",
    "Issuer": "DocAttestation",
    "Audience": "DocAttestationUsers"
  },
  "EncryptionSettings": {
    "AESKey": "GENERATE_EXACTLY_32_CHARACTERS_KEY",
    "AESIV": "GENERATE_EXACTLY_16_CHARACTERS_IV"
  }
}
```

### 4. Generate Secure Keys

**For JWT SecretKey** (minimum 32 characters):
```bash
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | % {[char]$_})

# Or use online generator: https://www.grc.com/passwords.htm
```

**For AES Key** (exactly 32 characters):
```bash
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | % {[char]$_})
```

**For AES IV** (exactly 16 characters):
```bash
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 16 | % {[char]$_})
```

### 5. Install Client Libraries

```bash
# Install LibMan CLI (if not installed)
dotnet tool install -g Microsoft.Web.LibraryManager.Cli

# Restore client libraries
libman restore
```

### 6. Create Database Migration

```bash
# Install EF Core tools (if not installed)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

### 7. Create Upload Directories

```bash
# Windows PowerShell
New-Item -ItemType Directory -Path "wwwroot\uploads\documents" -Force
New-Item -ItemType Directory -Path "wwwroot\uploads\photos" -Force
New-Item -ItemType Directory -Path "logs" -Force
```

### 8. Run Application

```bash
dotnet run
```

Navigate to: `https://localhost:5001`

### 9. Create Initial Admin User

**Option 1: Using Package Manager Console in Visual Studio**

```csharp
// In Package Manager Console
var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

// Create admin user
var admin = new ApplicationUser 
{ 
    UserName = "admin@example.com", 
    Email = "admin@example.com",
    FullName = "System Administrator"
};
await userManager.CreateAsync(admin, "Admin@123456");
await userManager.AddToRoleAsync(admin, "Admin");
```

**Option 2: Using SQL Script**

```sql
-- Note: Password hash needs to be generated using ASP.NET Identity
-- It's better to use Option 1 or create a seed script
```

**Option 3: Create Seed Script**

Create `Data/SeedData.cs`:

```csharp
public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure Admin role exists
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Create admin user if not exists
        var adminEmail = "admin@example.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                IsActive = true
            };
            await userManager.CreateAsync(adminUser, "Admin@123456");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
```

Then call in `Program.cs`:
```csharp
await SeedData.SeedAsync(scope.ServiceProvider);
```

## Production Deployment Checklist

- [ ] Change all default passwords and keys
- [ ] Update connection string with production database
- [ ] Enable HTTPS and configure SSL certificates
- [ ] Set `RequireHttpsMetadata = true` in JWT settings
- [ ] Configure proper logging (Serilog to file/database)
- [ ] Set up backup procedures for database
- [ ] Configure firewall rules
- [ ] Set up monitoring and alerts
- [ ] Perform security audit
- [ ] Test all workflows end-to-end
- [ ] Configure email settings (if using email confirmation)
- [ ] Set up CDN for static files (optional)
- [ ] Configure reverse proxy (IIS/Nginx) if needed
- [ ] Set environment variables for sensitive data
- [ ] Enable CORS if needed for API access
- [ ] Configure rate limiting
- [ ] Set up automated backups

## Troubleshooting

### Migration Errors
- Ensure SQL Server is running
- Check connection string
- Verify database user has CREATE permissions

### JWT Errors
- Verify SecretKey is at least 32 characters
- Check Issuer and Audience match in configuration

### Encryption Errors
- Verify AESKey is exactly 32 characters
- Verify AESIV is exactly 16 characters

### File Upload Errors
- Check upload directory permissions
- Verify file size limits in configuration
- Ensure wwwroot folder exists

## Support

For issues, check:
1. Application logs in `logs/` directory
2. Event Viewer (Windows)
3. Database connection
4. Configuration file syntax

