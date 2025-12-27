# Mobile App Troubleshooting Guide

## App Builds Successfully But Won't Run

If your mobile app builds successfully but doesn't launch or crashes immediately, check the following:

---

## Android App Issues

### 1. Check AndroidManifest.xml

**Missing Launch Activity:**
```xml
<activity
    android:name=".MainActivity"
    android:exported="true"
    android:label="@string/app_name"
    android:theme="@style/LaunchTheme">
    <intent-filter>
        <action android:name="android.intent.action.MAIN" />
        <category android:name="android.intent.category.LAUNCHER" />
    </intent-filter>
</activity>
```

**Required Permissions:**
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

### 2. Network Security Configuration (Android 9+)

If your app uses HTTP (not HTTPS), create `res/xml/network_security_config.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<network-security-config>
    <domain-config cleartextTrafficPermitted="true">
        <domain includeSubdomains="true">your-server-ip-or-domain</domain>
    </domain-config>
</network-security-config>
```

Then reference it in AndroidManifest.xml:
```xml
<application
    android:networkSecurityConfig="@xml/network_security_config"
    ...>
```

### 3. Check Logcat for Errors

```bash
# Connect device and run:
adb logcat | grep -i "error\|exception\|crash\|fatal"

# Or filter by your app package:
adb logcat | grep "com.mofa.docattestation.scanner"
```

Common errors to look for:
- `ClassNotFoundException`
- `NoSuchMethodError`
- `NetworkSecurityException`
- `SecurityException`

### 4. ProGuard/R8 Issues

If using release build, check `proguard-rules.pro`:

```proguard
# Keep your API models
-keep class com.mofa.docattestation.scanner.models.** { *; }

# Keep Retrofit/OkHttp
-keepattributes Signature
-keepattributes *Annotation*
-keep class retrofit2.** { *; }
-keep class okhttp3.** { *; }
-keep class okio.** { *; }
```

### 5. App Installation Issues

**Check if app is installed:**
```bash
adb shell pm list packages | grep docattestation
```

**Uninstall and reinstall:**
```bash
adb uninstall com.mofa.docattestation.scanner
adb install app-release.apk
```

**Check installation errors:**
```bash
adb install -r app-release.apk
# Look for error messages
```

### 6. App Signing Issues

**Debug build:** Should auto-sign with debug keystore
**Release build:** Must be signed with release keystore

Check signing configuration in `build.gradle`:
```gradle
android {
    signingConfigs {
        release {
            storeFile file('path/to/keystore.jks')
            storePassword 'your-password'
            keyAlias 'your-alias'
            keyPassword 'your-password'
        }
    }
    buildTypes {
        release {
            signingConfig signingConfigs.release
        }
    }
}
```

---

## iOS App Issues

### 1. Check Info.plist

**Required entries:**
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>
</dict>
<key>NSCameraUsageDescription</key>
<string>Camera access is required to scan QR codes</string>
```

### 2. Code Signing

- Check signing certificate in Xcode
- Ensure provisioning profile is valid
- Check bundle identifier matches: `com.mofa.docattestation.scanner`

### 3. Check Console Logs

In Xcode:
- Window → Devices and Simulators
- Select your device
- View Device Logs
- Filter by your app name

---

## Common Startup Issues

### 1. API Base URL Not Configured

**Check your app's configuration:**
- Ensure API base URL is set correctly
- Test connectivity: `GET /api/mobile/health`
- Verify server is running and accessible

**Test from command line:**
```bash
# Test health endpoint
curl https://your-server-url/api/mobile/health

# Should return:
# {"status":"healthy","serverTime":"...","minimumAppVersion":"1.0.0",...}
```

### 2. Missing Dependencies

**Check if all required libraries are included:**
- Network library (Retrofit/OkHttp for Android, URLSession for iOS)
- JSON parsing library
- Camera/QR scanning library
- JWT token handling

### 3. Initialization Errors

**Wrap app initialization in try-catch:**
```kotlin
// Android example
try {
    // Initialize app
    initializeApp()
} catch (e: Exception) {
    Log.e("App", "Initialization failed", e)
    // Show error to user
}
```

### 4. Null Pointer Exceptions

**Check for null values:**
- API base URL
- Configuration values
- Service instances
- Context references

---

## Debugging Steps

### Step 1: Check Build Output
- Look for warnings or errors during build
- Check if APK/IPA was generated successfully
- Verify file size (should not be 0 bytes)

### Step 2: Install on Device
```bash
# Android
adb install -r app-release.apk

# Check installation
adb shell pm list packages | grep docattestation
```

### Step 3: Launch App Manually
```bash
# Android - Launch app
adb shell am start -n com.mofa.docattestation.scanner/.MainActivity

# Check if it starts
adb shell dumpsys activity activities | grep "docattestation"
```

### Step 4: Monitor Logs
```bash
# Android - Real-time logs
adb logcat -c  # Clear logs
adb logcat | grep -i "docattestation\|error\|exception"

# Then launch app and watch for errors
```

### Step 5: Test API Connectivity

**From device browser or Postman:**
```
GET https://your-server-url/api/mobile/health
```

Should return:
```json
{
  "status": "healthy",
  "serverTime": "2024-01-01T00:00:00Z",
  "minimumAppVersion": "1.0.0",
  "allowWebVerification": false
}
```

---

## Quick Fixes

### Fix 1: Clean and Rebuild
```bash
# Android
./gradlew clean
./gradlew assembleRelease

# iOS
# In Xcode: Product → Clean Build Folder (Shift+Cmd+K)
# Then: Product → Build (Cmd+B)
```

### Fix 2: Check Minimum SDK Version
**Android:** Ensure `minSdkVersion` matches device Android version
**iOS:** Ensure deployment target matches device iOS version

### Fix 3: Verify App Identifier
Must match exactly: `com.mofa.docattestation.scanner`

### Fix 4: Check Device Compatibility
- Android: Minimum API level 21 (Android 5.0)
- iOS: Minimum iOS 13.0

---

## Backend API Health Check

Before troubleshooting mobile app, verify backend is working:

```bash
# Test health endpoint
curl https://your-server-url/api/mobile/health

# Test login endpoint (should fail without credentials, but should respond)
curl -X POST https://your-server-url/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test","password":"test"}'
```

Expected responses:
- Health: `200 OK` with JSON
- Login: `400 Bad Request` or `401 Unauthorized` (not `500 Internal Server Error`)

---

## Still Not Working?

1. **Check device logs** - Most errors appear in logs
2. **Test on emulator/simulator first** - Easier to debug
3. **Test on different device** - Rule out device-specific issues
4. **Check server logs** - Backend might be rejecting requests
5. **Verify network connectivity** - Device can reach server
6. **Test with Postman/curl** - Verify API endpoints work independently

---

## Contact Information

If issues persist, provide:
- Device model and OS version
- App build type (debug/release)
- Logcat/Xcode console output
- Server response from health endpoint
- Steps to reproduce

