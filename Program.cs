using DocAttestation.Configuration;
using DocAttestation.Data;
using DocAttestation.Middleware;
using DocAttestation.Models;
using DocAttestation.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllersWithViews();

// CORS Configuration for Mobile App
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileAppPolicy", policy =>
    {
        policy.AllowAnyOrigin()  // In production, replace with specific origins
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-App-Version", "X-Platform");
    });
});

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Set to true in production with email confirmation

    // Security stamp
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
{
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");
}

var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    // Default to Identity cookies for MVC views
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApplicantOnly", policy => policy.RequireRole("Applicant"));
    options.AddPolicy("OfficerOnly", policy => policy.RequireRole("VerificationOfficer", "Supervisor", "AttestationOfficer"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("VerificationOfficerOrAbove", policy => policy.RequireRole("VerificationOfficer", "Supervisor", "AttestationOfficer", "Admin"));
    options.AddPolicy("SupervisorOrAbove", policy => policy.RequireRole("Supervisor", "AttestationOfficer", "Admin"));
    options.AddPolicy("AttestationOfficerOrAbove", policy => policy.RequireRole("AttestationOfficer", "Admin"));
    
    // Configure access denied page
    options.FallbackPolicy = null;
});

// Configure access denied redirect
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

// Application Services
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IQRCodeService, QRCodeService>();
builder.Services.AddScoped<IPdfStampingService, PdfStampingService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddScoped<IMobileAppService, MobileAppService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOTPService, OTPService>();
builder.Services.AddMemoryCache();

// Configuration Options
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<EncryptionSettings>(builder.Configuration.GetSection("EncryptionSettings"));
builder.Services.Configure<QRCodeSettings>(builder.Configuration.GetSection("QRCodeSettings"));
builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection("FileUploadSettings"));
builder.Services.Configure<MobileAppSettings>(builder.Configuration.GetSection("MobileAppSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));

// HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Global Exception Handling Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// CORS must be before UseRouting
app.UseCors("MobileAppPolicy");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map API controllers
app.MapControllers();

// Map MVC controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize Database and Roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created
        context.Database.Migrate();

        // Seed roles
        await SeedRolesAsync(roleManager);

        // Seed Admin User (only if no admin exists)
        // TODO: Remove this after creating admin user for security
        await SeedAdminUserAsync(userManager);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding the database");
    }
}

app.Run();

// Seed Roles
static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
{
    string[] roles = { "Applicant", "VerificationOfficer", "Supervisor", "AttestationOfficer", "Admin" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Seed Admin User
static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
{
    var adminEmail = "admin@mofa.gov.pk";
    var adminPassword = "Admin@12345"; // ⚠️ CHANGE THIS PASSWORD AFTER FIRST LOGIN!

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
            Log.Warning("⚠️ Default admin password is: {Password} - Please change it after first login!", adminPassword);
        }
        else
        {
            Log.Error("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}

