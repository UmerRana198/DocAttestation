# Document Attestation Online Portal

A production-ready ASP.NET Core MVC (.NET 8) application for secure document attestation with government-grade security, workflow management, and audit compliance.

## Features

### 🔐 Security
- **ASP.NET Core Identity** with role-based access control (RBAC)
- **JWT Bearer Authentication** with access and refresh tokens
- **AES-256 Encryption** for sensitive data (CNIC)
- **Token rotation and revocation**
- **Policy-based authorization**
- **Secure logging** (no sensitive data in logs)
- **Global exception handling**

### 👤 User Management
- **CNIC-based Registration** with encryption
- **Unique CNIC validation** with hash-based lookup
- **Masked CNIC display** in UI
- **Role-based access**: Applicant, VerificationOfficer, Supervisor, AttestationOfficer, Admin

### 📝 Multi-Step Application Wizard
- **Step 1**: Personal Information (Name, Father Name, DOB, Gender, Photo)
- **Step 2**: Contact Information (Mobile, Email, Address)
- **Step 3**: Education/Document Information (Type, Authority, Year, Registration/Roll No)
- **Step 4**: Document Upload (PDF only, with validation)
- **Step 5**: Review & Submit
- **State persistence** - resume from any step
- **Progress tracking**

### 🔄 Workflow Engine
- **3-Level Approval Process**:
  - Level 1: VerificationOfficer
  - Level 2: Supervisor
  - Level 3: AttestationOfficer
- **Sequential enforcement** - must complete each level
- **Actions**: Approve, Reject, Send Back (with mandatory remarks)
- **Full audit trail** with IP logging
- **Status transitions** tracked

### 📄 PDF Stamping
- **QR Code embedding** on bottom-right corner
- **Watermark text**: "Digitally Attested"
- **SHA256 hash** calculation and verification
- **Tamper detection** via hash mismatch

### 🔍 QR Code Verification
- **Encrypted tokens only** - no plain data
- **AES-256 encrypted** QR tokens
- **Token validation**: expiry, nonce, document hash
- **Revocation support**
- **Scan logging** (IP, timestamp, user agent)
- **Public verification endpoint**: `/verify/qr?t=ENCRYPTED_TOKEN`

## Technology Stack

- **.NET 8.0**
- **ASP.NET Core MVC**
- **Entity Framework Core** (SQL Server)
- **ASP.NET Core Identity**
- **JWT Bearer Authentication**
- **iText7** (PDF manipulation)
- **QRCoder** (QR code generation)
- **Serilog** (logging)
- **Bootstrap 5** (UI)
- **SweetAlert2** (alerts)

## Database Schema

### Core Tables
- `AspNetUsers` - User accounts
- `AspNetRoles` - Roles
- `ApplicantProfiles` - Applicant profile data (encrypted CNIC)
- `Applications` - Attestation applications
- `Documents` - Document metadata
- `WorkflowSteps` - Current workflow assignments
- `WorkflowHistory` - Complete audit trail
- `QRVerificationTokens` - QR token management
- `QRScanLogs` - Verification scan logs
- `RefreshTokens` - JWT refresh tokens

## Configuration

### appsettings.json

Update the following settings before deployment:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string"
  },
  "JwtSettings": {
    "SecretKey": "YOUR_32_CHAR_MINIMUM_SECRET_KEY",
    "Issuer": "DocAttestation",
    "Audience": "DocAttestationUsers",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "EncryptionSettings": {
    "AESKey": "YOUR_EXACTLY_32_CHAR_AES_KEY",
    "AESIV": "YOUR_EXACTLY_16_CHAR_IV"
  },
  "QRCodeSettings": {
    "VerificationBaseUrl": "https://verify.myattestation.gov/qr",
    "TokenExpirationDays": 365,
    "QRSize": 200
  }
}
```

### Security Notes

⚠️ **IMPORTANT**: 
- Generate strong, random keys for JWT and AES encryption
- Use environment variables or Azure Key Vault for production secrets
- Enable HTTPS in production
- Set `RequireHttpsMetadata = true` in JWT configuration
- Enable email confirmation in production

## Setup Instructions

### 1. Prerequisites
- .NET 8.0 SDK
- SQL Server (or SQL Server Express)
- Visual Studio 2022 or VS Code

### 2. Database Setup

```bash
# Create migration
dotnet ef migrations add InitialCreate

# Apply migration
dotnet ef database update
```

### 3. Configure Secrets

Update `appsettings.json` with:
- Database connection string
- JWT secret key (minimum 32 characters)
- AES encryption key (exactly 32 characters)
- AES IV (exactly 16 characters)

### 4. Run Application

```bash
dotnet run
```

Navigate to `https://localhost:5001` (or configured port)

### 5. Initial Admin User

Create an admin user manually or via seed data:

```csharp
// In Program.cs or a seed script
var adminUser = new ApplicationUser { UserName = "admin@example.com", Email = "admin@example.com" };
await userManager.CreateAsync(adminUser, "SecurePassword123!");
await userManager.AddToRoleAsync(adminUser, "Admin");
```

## User Roles

1. **Applicant**: Register, complete profile, submit applications, track status
2. **VerificationOfficer**: Review and approve/reject applications at Level 1
3. **Supervisor**: Review and approve/reject applications at Level 2
4. **AttestationOfficer**: Final approval at Level 3
5. **Admin**: Full system access, user management

## API Endpoints

### Authentication
- `POST /Account/Register` - Register new applicant
- `POST /Account/Login` - Login
- `POST /Account/Logout` - Logout

### Profile
- `GET /Profile/Index` - View profile
- `GET /Profile/Step1` - Personal information
- `GET /Profile/Step2` - Contact information
- `GET /Profile/Step3` - Document information
- `GET /Profile/Step4` - Document upload
- `GET /Profile/Step5` - Review & submit

### Applications
- `GET /Application/Index` - List user's applications
- `GET /Application/Details/{id}` - View application details
- `GET /Application/Download/{id}` - Download attested PDF

### Workflow
- `GET /Workflow/Index` - Pending applications
- `GET /Workflow/Review/{id}` - Review application
- `POST /Workflow/Approve` - Approve application
- `POST /Workflow/Reject` - Reject application
- `POST /Workflow/SendBack` - Send back application

### Verification
- `GET /Verification/QR` - QR verification page
- `GET /verify/qr?t={token}` - Verify QR token (API)

## Security Best Practices Implemented

✅ **CNIC Encryption**: AES-256 encryption, hash-based lookup
✅ **JWT Security**: Short-lived access tokens, refresh token rotation
✅ **Password Security**: ASP.NET Identity hashing
✅ **SQL Injection Protection**: EF Core parameterized queries
✅ **XSS Protection**: Input validation, output encoding
✅ **CSRF Protection**: Anti-forgery tokens
✅ **Secure Headers**: HTTPS enforcement, secure cookies
✅ **Audit Logging**: All workflow actions logged with IP
✅ **Token Revocation**: Refresh token revocation support
✅ **Document Integrity**: SHA256 hashing, tamper detection

## Future Enhancements

- [ ] MAUI app restriction for QR scanning
- [ ] Email notifications
- [ ] SMS notifications
- [ ] Document preview
- [ ] Bulk operations
- [ ] Advanced reporting
- [ ] Multi-language support
- [ ] Certificate pinning for MAUI app

## License

This project is proprietary software. All rights reserved.

## Support

For issues or questions, contact the development team.

---

**⚠️ SECURITY WARNING**: This is a production-ready template. Before deployment:
1. Change all default passwords and keys
2. Enable HTTPS
3. Configure proper logging
4. Set up backup procedures
5. Perform security audit
6. Configure firewall rules
7. Set up monitoring and alerts

