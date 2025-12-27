# Build APK with Production Base URL

## Base URL Configuration
**Production API URL:** `http://103.175.122.31:81`

---

## Quick Build Steps

### Step 1: Update Mobile App Configuration

Copy the configuration files from `mobile-app-config/android/` to your Android project:

1. **ApiConfig.kt** → `app/src/main/java/com/mofa/docattestation/scanner/config/ApiConfig.kt`
2. **AndroidManifest.xml** → Update your existing `AndroidManifest.xml`
3. **network_security_config.xml** → `app/src/main/res/xml/network_security_config.xml`
4. **RetrofitService.kt** → `app/src/main/java/com/mofa/docattestation/scanner/network/RetrofitService.kt`

### Step 2: Update build.gradle

Add to your `app/build.gradle`:

```gradle
android {
    defaultConfig {
        buildConfigField "String", "BASE_URL", "\"http://103.175.122.31:81\""
    }
}
```

### Step 3: Build APK

#### Option A: Build Debug APK (Quick Testing)
```bash
cd /path/to/your/android/project
./gradlew assembleDebug
```

**Output:** `app/build/outputs/apk/debug/app-debug.apk`

#### Option B: Build Release APK (Production)
```bash
cd /path/to/your/android/project
./gradlew assembleRelease
```

**Output:** `app/build/outputs/apk/release/app-release.apk`

**Note:** Release APK requires signing. See signing configuration below.

### Step 4: Install APK

```bash
# Install debug APK
adb install app/build/outputs/apk/debug/app-debug.apk

# Or install release APK
adb install app/build/outputs/apk/release/app-release.apk
```

---

## Complete Build Commands

### Windows (PowerShell)
```powershell
# Navigate to Android project
cd C:\path\to\android\project

# Clean previous builds
.\gradlew clean

# Build debug APK
.\gradlew assembleDebug

# Build release APK
.\gradlew assembleRelease
```

### Mac/Linux
```bash
# Navigate to Android project
cd /path/to/android/project

# Clean previous builds
./gradlew clean

# Build debug APK
./gradlew assembleDebug

# Build release APK
./gradlew assembleRelease
```

---

## Verify Base URL in APK

### Method 1: Check BuildConfig
```bash
# Extract and check BuildConfig
aapt dump badging app-debug.apk | grep BASE_URL
```

### Method 2: Test After Installation
1. Install APK on device
2. Open app
3. Check logs for API calls:
   ```bash
   adb logcat | grep -i "http://103.175.122.31"
   ```

### Method 3: Test Health Endpoint
After installing, the app should connect to:
```
http://103.175.122.31:81/api/mobile/health
```

---

## Signing Release APK

### Create Keystore (First Time Only)
```bash
keytool -genkey -v -keystore docattestation-release.keystore \
  -alias docattestation -keyalg RSA -keysize 2048 -validity 10000
```

### Configure Signing in build.gradle
```gradle
android {
    signingConfigs {
        release {
            storeFile file('docattestation-release.keystore')
            storePassword 'your-store-password'
            keyAlias 'docattestation'
            keyPassword 'your-key-password'
        }
    }
    
    buildTypes {
        release {
            signingConfig signingConfigs.release
            minifyEnabled false
            proguardFiles getDefaultProguardFile('proguard-android-optimize.txt'), 'proguard-rules.pro'
        }
    }
}
```

---

## Configuration Checklist

Before building, verify:

- [ ] `ApiConfig.kt` has `BASE_URL = "http://103.175.122.31:81"`
- [ ] `AndroidManifest.xml` has INTERNET permission
- [ ] `network_security_config.xml` allows HTTP for `103.175.122.31`
- [ ] `build.gradle` has BASE_URL buildConfigField
- [ ] App package name is `com.mofa.docattestation.scanner`
- [ ] Minimum SDK is 21 (Android 5.0)

---

## Test API Connectivity

### Before Building
Test that the API is accessible:
```bash
curl http://103.175.122.31:81/api/mobile/health
```

Expected response:
```json
{
  "status": "healthy",
  "baseApiUrl": "http://103.175.122.31:81",
  "endpoints": { ... }
}
```

### After Installing APK
1. Launch app
2. Check logs:
   ```bash
   adb logcat | grep -i "api\|http\|error"
   ```
3. Test login functionality
4. Verify API calls go to `http://103.175.122.31:81`

---

## Troubleshooting

### Build Fails
```bash
# Clean and rebuild
./gradlew clean
./gradlew assembleDebug
```

### APK Won't Install
```bash
# Uninstall existing app
adb uninstall com.mofa.docattestation.scanner

# Install new APK
adb install -r app-debug.apk
```

### API Connection Fails
1. Check network security config
2. Verify INTERNET permission
3. Test API from browser: `http://103.175.122.31:81/api/mobile/health`
4. Check device logs for errors

### App Crashes on Launch
```bash
# View crash logs
adb logcat | grep -i "fatal\|exception\|crash"
```

---

## Output Files

After successful build:

**Debug APK:**
- Location: `app/build/outputs/apk/debug/app-debug.apk`
- Size: ~5-10 MB
- Signed: Auto-signed with debug keystore

**Release APK:**
- Location: `app/build/outputs/apk/release/app-release.apk`
- Size: ~3-8 MB (optimized)
- Signed: Requires release keystore

---

## Next Steps

1. ✅ Build APK with correct base URL
2. ✅ Install on test device
3. ✅ Test API connectivity
4. ✅ Test login functionality
5. ✅ Test QR code scanning
6. ✅ Test device registration
7. ✅ Deploy to production

---

## Quick Reference

**Base URL:** `http://103.175.122.31:81`
**Package Name:** `com.mofa.docattestation.scanner`
**Min SDK:** 21
**Target SDK:** 34

**Health Endpoint:** `http://103.175.122.31:81/api/mobile/health`
**Login Endpoint:** `http://103.175.122.31:81/api/auth/login`

