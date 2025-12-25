# How to Create the First Admin User

Since the Admin panel requires Admin role access, you need to create the first Admin user manually. Here are the methods:

## Method 1: Using Entity Framework Core (Recommended)

Create a migration or seed script to add the first admin user.

### Option A: Add to Program.cs (Temporary)

Add this code in `Program.cs` after role seeding (around line 165):

```csharp
// Seed Admin User (only if no admin exists)
var adminEmail = "admin@mofa.gov.pk";
var adminPassword = "Admin@12345"; // Change this!

var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
if (existingAdmin == null)
{
    var adminUser = new ApplicationUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        FullName = "System Administrator",
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };
    
    var result = await userManager.CreateAsync(adminUser, adminPassword);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
        Log.Information("Admin user created: {Email}", adminEmail);
    }
}
```

**⚠️ IMPORTANT**: Remove this code after creating the admin user for security!

### Option B: Create a Seed Script

Create a file `Scripts/SeedAdmin.cs`:

```csharp
using DocAttestation.Data;
using DocAttestation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class SeedAdmin
{
    public static async Task CreateAdminUser(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var adminEmail = "admin@mofa.gov.pk";
        var adminPassword = "Admin@12345"; // Change this!
        
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine($"Admin user created: {adminEmail}");
            }
        }
    }
}
```

## Method 2: Using SQL Server Directly

Run this SQL script in your database:

```sql
-- First, get the Admin role ID
DECLARE @AdminRoleId NVARCHAR(450);
SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';

-- Create admin user (password hash is for 'Admin@12345')
-- You should generate a proper password hash using ASP.NET Identity PasswordHasher
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
    EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, 
    PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, 
    FullName, CreatedAt, IsActive)
VALUES (
    NEWID(),
    'admin@mofa.gov.pk',
    'ADMIN@MOFA.GOV.PK',
    'admin@mofa.gov.pk',
    'ADMIN@MOFA.GOV.PK',
    1,
    'AQAAAAIAAYagAAAAEJ...', -- Replace with actual password hash
    NEWID().ToString(),
    NEWID().ToString(),
    0, 0, 1, 0,
    'System Administrator',
    GETUTCDATE(),
    1
);

-- Assign Admin role
DECLARE @AdminUserId NVARCHAR(450);
SELECT @AdminUserId = Id FROM AspNetUsers WHERE UserName = 'admin@mofa.gov.pk';

INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES (@AdminUserId, @AdminRoleId);
```

**Note**: This method requires generating a proper password hash, which is complex. Use Method 1 instead.

## Method 3: Using Package Manager Console

In Visual Studio, open Package Manager Console and run:

```powershell
# Create a migration with seed data
Add-Migration SeedAdminUser
```

Then edit the migration file to add admin user creation code.

## Quick Start (Recommended)

1. **Temporarily add seed code to Program.cs** (Method 1, Option A)
2. **Run the application** - Admin user will be created automatically
3. **Login with**:
   - Email: `admin@mofa.gov.pk`
   - Password: `Admin@12345` (or whatever you set)
4. **Remove the seed code** from Program.cs for security
5. **Change the admin password** through the admin panel (if password change feature exists)

## After Creating Admin

1. Login as Admin
2. Go to Admin Dashboard (`/Admin/Index`)
3. Click "Create Officer Account" to create officer accounts
4. Share credentials with officers

## Security Notes

- ⚠️ **Change the default password immediately** after first login
- ⚠️ **Remove seed code** from Program.cs after creating admin
- ⚠️ **Use strong passwords** (minimum 8 characters with complexity)
- ⚠️ **Limit admin accounts** - only create what's necessary

## Troubleshooting

### "Access Denied" when accessing Admin panel
- Verify you're logged in as Admin
- Check your user has the "Admin" role assigned
- Try logging out and logging back in
- Clear browser cookies and try again

### Cannot create admin user
- Ensure database migrations are applied
- Check database connection string is correct
- Verify roles are seeded (should happen automatically)

### Admin user exists but can't login
- Verify email is correct
- Check password meets requirements
- Ensure account is active (`IsActive = true`)
- Check if account is locked (too many failed attempts)

