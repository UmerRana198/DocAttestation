# Officer Login Guide

## How Officers Login

Officers login to the system using the **same login page** as applicants. The system automatically detects their role and redirects them to the appropriate dashboard.

### Login Steps

1. **Navigate to Login Page**
   - Go to: `http://localhost:5000/Account/Login`
   - Or click "Login" in the navigation menu

2. **Enter Credentials**
   - **Login**: Enter your **Email address** (officers use email, not CNIC)
   - **Password**: Enter your password
   - **Remember Me**: (Optional) Check to stay logged in

3. **Click Login Button**

4. **Automatic Redirect**
   - **VerificationOfficer** → Redirected to `/Workflow/Index` (Pending Applications)
   - **Supervisor** → Redirected to `/Workflow/Index` (Pending Applications)
   - **AttestationOfficer** → Redirected to `/Workflow/Index` (Pending Applications)
   - **Admin** → Redirected to `/Admin/Index` (Admin Dashboard)
   - **Applicant** → Redirected to `/Profile/Index` (Profile Dashboard)

## Creating Officer Accounts

Officer accounts must be created by an **Admin** user. Officers cannot self-register.

### Admin Steps to Create Officer Account

1. **Admin Login**
   - Login as Admin user
   - You'll be redirected to Admin Dashboard

2. **Navigate to Create Officer**
   - Click "Create Officer Account" button
   - Or go to: `/Admin/CreateOfficer`

3. **Fill Officer Details**
   - **Full Name**: Officer's full name
   - **Email**: Officer's email address (used for login)
   - **Phone Number**: (Optional) Officer's phone number
   - **Role**: Select one of:
     - `VerificationOfficer` - Level 1 reviewer
     - `Supervisor` - Level 2 reviewer
     - `AttestationOfficer` - Level 3 reviewer
     - `Admin` - System administrator
   - **Password**: Set initial password (minimum 8 characters)
   - **Confirm Password**: Re-enter password

4. **Click "Create Officer Account"**

5. **Share Credentials**
   - Share the email and password with the officer
   - Officer should change password after first login (if password change feature is implemented)

## Password Requirements

Officer passwords must meet the following requirements:
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit (0-9)
- At least one special character

## Login Credentials Format

### For Officers
- **Login Field**: Email address (e.g., `officer@mofa.gov.pk`)
- **Password**: The password set by Admin

### For Applicants
- **Login Field**: Email address OR CNIC number
- **Password**: Password set during registration

## Troubleshooting

### "Invalid login attempt"
- Check email address is correct
- Verify password is correct
- Ensure account is active (Admin can check in Admin Dashboard)
- Check if account is locked (too many failed attempts)

### "Account locked out"
- Account is temporarily locked after 5 failed login attempts
- Wait 15 minutes or contact Admin to unlock

### Cannot Access Workflow
- Verify you have the correct role assigned
- Check with Admin that your account has the proper role
- Ensure you're assigned to review applications

### Forgot Password
- Currently, password reset must be done by Admin
- Contact Admin to reset your password
- (Future: Password reset via email can be implemented)

## Security Notes

1. **Never share your password** with anyone
2. **Use strong passwords** that meet requirements
3. **Logout** when finished, especially on shared computers
4. **Report suspicious activity** to Admin immediately
5. **Change password regularly** (if password change feature is available)

## Role-Based Access

### VerificationOfficer
- Can view applications assigned to them
- Can review and approve/reject at Level 1
- Redirected to Workflow dashboard after login

### Supervisor
- Can view applications assigned to them
- Can review and approve/reject at Level 2
- Redirected to Workflow dashboard after login

### AttestationOfficer
- Can view applications assigned to them
- Can review and approve/reject at Level 3
- Redirected to Workflow dashboard after login

### Admin
- Full system access
- Can create officer accounts
- Can view all users
- Redirected to Admin dashboard after login

## Quick Reference

| Role | Login Email | Redirects To | Can Create Accounts |
|------|-------------|--------------|---------------------|
| VerificationOfficer | Email | /Workflow/Index | No |
| Supervisor | Email | /Workflow/Index | No |
| AttestationOfficer | Email | /Workflow/Index | No |
| Admin | Email | /Admin/Index | Yes |
| Applicant | Email or CNIC | /Profile/Index | No (Self-register) |

## Example Login Flow

```
1. Officer opens: http://localhost:5000/Account/Login
2. Enters: Email = "verification.officer@mofa.gov.pk"
3. Enters: Password = "SecurePass123!"
4. Clicks: "Login"
5. System checks role → "VerificationOfficer"
6. Redirects to: /Workflow/Index
7. Officer sees pending applications assigned to them
```

## Need Help?

- **Login Issues**: Contact your system administrator
- **Account Creation**: Only Admin can create officer accounts
- **Role Assignment**: Admin can assign/change roles
- **Password Reset**: Contact Admin for password reset

