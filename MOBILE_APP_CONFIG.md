# Mobile App Configuration Reference

## Production API Base URL

**Base URL:** `http://103.175.122.31:81`

**Portal:** [http://103.175.122.31:81/](http://103.175.122.31:81/)

---

## API Endpoints

### Base Configuration
```
Base URL: http://103.175.122.31:81
```

### Available Endpoints

#### 1. Health Check
```
GET http://103.175.122.31:81/api/mobile/health
```
**Response:**
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

#### 2. Authentication

**Login:**
```
POST http://103.175.122.31:81/api/auth/login
Content-Type: application/json

{
  "email": "officer@example.com",
  "password": "password"
}
```

**Refresh Token:**
```
POST http://103.175.122.31:81/api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

#### 3. Device Registration
```
POST http://103.175.122.31:81/api/mobile/device/register
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "deviceId": "unique-device-id",
  "deviceName": "Device Name",
  "platform": "Android",
  "appVersion": "1.0.0",
  "appSignature": "app-signature-hash"
}
```

#### 4. QR Verification
```
POST http://103.175.122.31:81/api/mobile/verify
Content-Type: application/json
X-App-Version: 1.0.0
X-Platform: Android

{
  "qrToken": "encrypted-qr-token",
  "deviceToken": "device-token",
  "signature": "hmac-signature"
}
```

---

## Mobile App Configuration

### Android (Kotlin)

**Create `Config.kt`:**
```kotlin
object ApiConfig {
    const val BASE_URL = "http://103.175.122.31:81"
    
    // API Endpoints
    const val HEALTH = "$BASE_URL/api/mobile/health"
    const val LOGIN = "$BASE_URL/api/auth/login"
    const val REFRESH = "$BASE_URL/api/auth/refresh"
    const val REGISTER_DEVICE = "$BASE_URL/api/mobile/device/register"
    const val VERIFY = "$BASE_URL/api/mobile/verify"
    
    // App Settings
    const val APP_IDENTIFIER = "com.mofa.docattestation.scanner"
    const val MINIMUM_APP_VERSION = "1.0.0"
}
```

**Retrofit Setup:**
```kotlin
val retrofit = Retrofit.Builder()
    .baseUrl(ApiConfig.BASE_URL)
    .addConverterFactory(GsonConverterFactory.create())
    .build()
```

### iOS (Swift)

**Create `Config.swift`:**
```swift
struct ApiConfig {
    static let baseURL = "http://103.175.122.31:81"
    
    // API Endpoints
    static let health = "\(baseURL)/api/mobile/health"
    static let login = "\(baseURL)/api/auth/login"
    static let refresh = "\(baseURL)/api/auth/refresh"
    static let registerDevice = "\(baseURL)/api/mobile/device/register"
    static let verify = "\(baseURL)/api/mobile/verify"
    
    // App Settings
    static let appIdentifier = "com.mofa.docattestation.scanner"
    static let minimumAppVersion = "1.0.0"
}
```

**URLSession Setup:**
```swift
let url = URL(string: ApiConfig.health)!
var request = URLRequest(url: url)
request.httpMethod = "GET"
```

---

## Network Security Configuration

### Android (AndroidManifest.xml)

**Required Permissions:**
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

**Network Security Config (res/xml/network_security_config.xml):**
```xml
<?xml version="1.0" encoding="utf-8"?>
<network-security-config>
    <domain-config cleartextTrafficPermitted="true">
        <domain includeSubdomains="true">103.175.122.31</domain>
    </domain-config>
</network-security-config>
```

**Reference in AndroidManifest.xml:**
```xml
<application
    android:networkSecurityConfig="@xml/network_security_config"
    ...>
```

### iOS (Info.plist)

**App Transport Security:**
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>
    <!-- Or specific domain exception -->
    <key>NSExceptionDomains</key>
    <dict>
        <key>103.175.122.31</key>
        <dict>
            <key>NSExceptionAllowsInsecureHTTPLoads</key>
            <true/>
            <key>NSIncludesSubdomains</key>
            <true/>
        </dict>
    </dict>
</dict>
```

**Camera Permission:**
```xml
<key>NSCameraUsageDescription</key>
<string>Camera access is required to scan QR codes for document verification</string>
```

---

## Testing API Connectivity

### Test Health Endpoint

**From Browser:**
```
http://103.175.122.31:81/api/mobile/health
```

**From Command Line:**
```bash
curl http://103.175.122.31:81/api/mobile/health
```

**Expected Response:**
```json
{
  "status": "healthy",
  "serverTime": "2025-01-01T00:00:00Z",
  "minimumAppVersion": "1.0.0",
  "allowWebVerification": false,
  "baseApiUrl": "http://103.175.122.31:81",
  "endpoints": { ... }
}
```

### Test Login Endpoint

```bash
curl -X POST http://103.175.122.31:81/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"test"}'
```

---

## App Settings

### App Identifier
```
com.mofa.docattestation.scanner
```

### Minimum App Version
```
1.0.0
```

### App Secret (for HMAC)
```
DocAttestMobileApp2024SecretKey64CharsForHMACSHA256Security!!
```

**⚠️ Important:** This secret must match between server and mobile app for device registration and QR verification to work.

---

## Error Handling

### Common HTTP Status Codes

- **200 OK** - Request successful
- **400 Bad Request** - Invalid request data
- **401 Unauthorized** - Authentication required or invalid credentials
- **403 Forbidden** - Insufficient permissions
- **404 Not Found** - Endpoint not found
- **500 Internal Server Error** - Server error

### Error Response Format

```json
{
  "success": false,
  "message": "Error description"
}
```

---

## Security Notes

⚠️ **Important:**
- The API uses HTTP (not HTTPS) - ensure network security config allows cleartext traffic
- JWT tokens expire after 15 minutes - implement token refresh
- Device registration requires officer authentication
- QR verification requires device token and HMAC signature
- Never commit app secrets or API keys to version control

---

## Quick Start Checklist

- [ ] Set `BASE_URL = "http://103.175.122.31:81"` in app config
- [ ] Add INTERNET permission (Android)
- [ ] Configure network security for HTTP (Android 9+)
- [ ] Add App Transport Security exception (iOS)
- [ ] Test health endpoint connectivity
- [ ] Implement login with JWT token handling
- [ ] Implement token refresh mechanism
- [ ] Test device registration
- [ ] Test QR code verification

---

## Support

For API issues or questions:
- Check server logs
- Test endpoints with curl/Postman
- Verify network connectivity
- Check CORS configuration
- Verify authentication tokens

