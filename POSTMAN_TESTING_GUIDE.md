# Postman Testing Guide for DocAttestation Mobile API

## Base URL
```
http://103.175.122.31:81
```

## Import Collection
1. Open Postman
2. Click **Import** button
3. Select the `POSTMAN_COLLECTION.json` file
4. The collection will be imported with all endpoints

## Testing Steps

### Step 1: Health Check
**Endpoint:** `GET /api/mobile/health`

- No authentication required
- Tests if the server is running
- Returns server status and available endpoints

**Expected Response:**
```json
{
  "status": "healthy",
  "serverTime": "2025-01-01T00:00:00Z",
  "minimumAppVersion": "1.0.0",
  "allowWebVerification": false,
  "baseApiUrl": "http://103.175.122.31:81",
  "endpoints": {
    "health": "http://103.175.122.31:81/api/mobile/health",
    "login": "http://103.175.122.31:81/api/auth/login",
    "refresh": "http://103.175.122.31:81/api/auth/refresh",
    "registerDevice": "http://103.175.122.31:81/api/mobile/device/register",
    "verify": "http://103.175.122.31:81/api/mobile/verify"
  }
}
```

---

### Step 2: Login (Get Auth Token)
**Endpoint:** `POST /api/auth/login`

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "officer@example.com",
  "password": "YourPassword123"
}
```

**Important:** 
- Use an officer account (VerificationOfficer, Supervisor, AttestationOfficer, or Admin)
- Regular applicant accounts cannot use the mobile app

**Expected Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh-token-string",
  "expiry": "2025-01-01T00:15:00Z",
  "userName": "Officer Name",
  "roles": ["VerificationOfficer"]
}
```

**Action:** Copy the `token` value and set it as the `accessToken` variable in Postman (or use it in the Authorization header).

---

### Step 3: Register Device
**Endpoint:** `POST /api/mobile/device/register`

**Headers:**
```
Content-Type: application/json
Authorization: Bearer {your-access-token}
```

**Request Body:**
```json
{
  "deviceId": "test-device-12345",
  "deviceName": "Test Android Device",
  "platform": "Android",
  "osVersion": "Android 13",
  "appVersion": "1.0.0",
  "appSignature": "test-signature-hash"
}
```

**Field Descriptions:**
- `deviceId`: Unique device identifier (e.g., Android ID or iOS UDID)
- `deviceName`: Human-readable device name (e.g., "Samsung Galaxy S21")
- `platform`: "Android" or "iOS"
- `osVersion`: OS version string (e.g., "Android 13" or "iOS 17.0")
- `appVersion`: App version (must meet minimum version requirement from health check)
- `appSignature`: App integrity signature hash (for security validation)

**Expected Success Response:**
```json
{
  "success": true,
  "message": "Device registered successfully",
  "deviceToken": "device-token-string-here",
  "tokenExpiry": "2025-01-15T00:00:00Z",
  "serverTime": "2025-01-01T00:00:00Z"
}
```

**Expected Error Responses:**

**401 Unauthorized:**
```json
{
  "success": false,
  "message": "User not authenticated"
}
```

**400 Bad Request (Invalid Request):**
```json
{
  "success": false,
  "message": "Invalid request data"
}
```

**400 Bad Request (Device Limit Reached):**
```json
{
  "success": false,
  "message": "Maximum device limit (3) reached. Please deactivate an existing device."
}
```

**400 Bad Request (Invalid App Signature):**
```json
{
  "success": false,
  "message": "App signature validation failed. Please use the official app."
}
```

**Action:** Copy the `deviceToken` value for use in QR verification requests.

---

### Step 4: Get Device Status (Optional)
**Endpoint:** `GET /api/mobile/device/status`

**Headers:**
```
Authorization: Bearer {your-access-token}
```

**Expected Response:**
```json
{
  "success": true,
  "devices": [
    {
      "id": 1,
      "deviceName": "Test Android Device",
      "platform": "Android",
      "appVersion": "1.0.0",
      "isActive": true,
      "isRevoked": false,
      "registeredAt": "2025-01-01T00:00:00Z",
      "lastUsedAt": "2025-01-01T00:00:00Z",
      "scanCount": 5,
      "tokenExpiry": "2025-01-15T00:00:00Z"
    }
  ]
}
```

---

### Step 5: Verify QR Code
**Endpoint:** `POST /api/mobile/verify`

**Headers:**
```
Content-Type: application/json
X-App-Version: 1.0.0
X-Platform: Android
```

**Request Body:**
```json
{
  "qrToken": "encrypted-qr-token-from-app",
  "deviceToken": "device-token-from-registration",
  "signature": "hmac-signature",
  "timestamp": "2025-01-01T00:00:00Z",
  "nonce": "random-nonce",
  "latitude": null,
  "longitude": null
}
```

**Note:** This endpoint requires proper HMAC signature generation. The mobile app handles this automatically.

---

## Postman Environment Variables

Set up these variables in Postman for easier testing:

1. **baseUrl**: `http://103.175.122.31:81`
2. **accessToken**: (set after login)
3. **refreshToken**: (set after login)
4. **deviceToken**: (set after device registration)

### How to Set Variables:

1. Click on **Environments** in Postman
2. Create a new environment (e.g., "DocAttestation Production")
3. Add the variables listed above
4. Select the environment before making requests

### Using Variables in Requests:

- In URL: `{{baseUrl}}/api/mobile/health`
- In Headers: `Bearer {{accessToken}}`
- In Body: `"deviceToken": "{{deviceToken}}"`

---

## Common Issues and Solutions

### Issue 1: Getting HTML Login Page Instead of JSON Response
**Symptom:** When calling the register device endpoint, you get an HTML login page instead of a JSON error.

**Cause:** The API endpoint is trying to use cookie authentication instead of JWT.

**Solution:** 
1. Make sure you're using the **API login endpoint** (`/api/auth/login`), NOT the web login page (`/Account/Login`)
2. The web login page uses cookies, but the API needs JWT tokens
3. Copy the token from the API login response (not from browser cookies)
4. Set the Authorization header in Postman as: `Bearer {your-jwt-token}`
5. Make sure the header is set correctly:
   - Header name: `Authorization`
   - Header value: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` (your full token)

**Important:** If you're still getting HTML, check:
- You're using `POST /api/auth/login` (not `/Account/Login`)
- The token is from the API response, not from browser cookies
- The Authorization header format is exactly: `Bearer {token}` (with a space after "Bearer")

### Issue 2: 401 Unauthorized
**Solution:** 
- Make sure you've logged in first using `/api/auth/login`
- Copy the token from the login response
- Set it in the Authorization header as: `Bearer {token}`
- Ensure the token hasn't expired (tokens expire after 15 minutes)
- Make sure the user has one of the required roles: VerificationOfficer, Supervisor, AttestationOfficer, or Admin

### Issue 2: 400 Bad Request - Invalid Request Data
**Solution:**
- Check that all required fields are present
- Ensure JSON format is correct
- Verify field names match exactly (case-sensitive)

### Issue 3: 400 Bad Request - App Signature Validation Failed
**Solution:**
- The app signature is validated for security
- For testing, you may need to use a valid signature
- In production, the mobile app generates this automatically

### Issue 4: 400 Bad Request - Maximum Device Limit Reached
**Solution:**
- Each user can register up to 3 devices (configurable)
- Deactivate an existing device first
- Or use a different user account

### Issue 5: Connection Timeout / Network Error
**Solution:**
- Verify the base URL is correct: `http://103.175.122.31:81`
- Check if the server is running
- Ensure your network can reach the server
- Check firewall settings

---

## Testing Checklist

- [ ] Health check returns 200 OK
- [ ] Login with officer account returns token
- [ ] Register device with valid token returns deviceToken
- [ ] Get device status shows registered devices
- [ ] Error handling works (invalid token, missing fields, etc.)

---

## Notes

1. **Authentication:** All device-related endpoints require a valid JWT token from login
2. **Roles:** Only officers (VerificationOfficer, Supervisor, AttestationOfficer, Admin) can use the mobile app
3. **Device Limit:** Maximum 3 devices per user (configurable in `appsettings.json`)
4. **Token Expiry:** 
   - Access tokens expire after 15 minutes
   - Device tokens expire after 30 days (configurable)
   - Use refresh token endpoint to get new access tokens
5. **App Signature:** The app signature is validated to ensure only official app versions can register devices

---

## Quick Test Script

Here's a quick test sequence:

1. **Health Check** → Should return 200 OK
2. **Login** → Should return token
3. **Register Device** (use token from step 2) → Should return deviceToken
4. **Get Device Status** (use token from step 2) → Should list registered devices

If all steps pass, the API is working correctly!

