# Deploy Mobile App to Physical Device

## Android - Deploy to Physical Device

### Step 1: Enable Developer Options on Android Device

1. **Open Settings** on your Android device
2. **Go to About Phone** (or About Device)
3. **Find Build Number** (usually at the bottom)
4. **Tap Build Number 7 times** until you see "You are now a developer!"
5. **Go back** to Settings
6. **Open Developer Options** (now visible in Settings)

### Step 2: Enable USB Debugging

1. **Open Developer Options** in Settings
2. **Enable "USB Debugging"**
3. **Enable "Install via USB"** (if available)
4. **Enable "Stay Awake"** (optional, keeps screen on while charging)

### Step 3: Connect Device to Computer

1. **Connect Android device** to computer via USB cable
2. **On device:** When prompted, tap "Allow USB Debugging" and check "Always allow from this computer"
3. **Verify connection:**
   ```bash
   adb devices
   ```
   
   Should show:
   ```
   List of devices attached
   ABC123XYZ    device
   ```
   
   If it shows "unauthorized", tap "Allow" on the device popup.

### Step 4: Build and Install APK

**Option A: Install Debug APK**
```bash
# Navigate to your Android project directory
cd path/to/your/android/project

# Build debug APK
./gradlew assembleDebug

# Install on connected device
adb install app/build/outputs/apk/debug/app-debug.apk

# Or install and run directly
adb install -r app/build/outputs/apk/debug/app-debug.apk
adb shell am start -n com.mofa.docattestation.scanner/.MainActivity
```

**Option B: Build and Install Release APK**
```bash
# Build release APK
./gradlew assembleRelease

# Install release APK
adb install app/build/outputs/apk/release/app-release.apk
```

**Option C: Direct Install from Android Studio**
1. **Open project** in Android Studio
2. **Connect device** via USB
3. **Select device** from device dropdown (top toolbar)
4. **Click Run** (green play button) or press `Shift+F10`
5. App will build and install automatically

### Step 5: Verify Installation

```bash
# Check if app is installed
adb shell pm list packages | grep docattestation

# Launch app
adb shell am start -n com.mofa.docattestation.scanner/.MainActivity

# View app logs
adb logcat | grep -i "docattestation\|error\|exception"
```

---

## iOS - Deploy to Physical Device

### Step 1: Connect iPhone/iPad to Mac

1. **Connect device** to Mac via USB cable
2. **Unlock device** and tap "Trust This Computer" if prompted
3. **Open Xcode**

### Step 2: Register Device in Xcode

1. **Open Xcode**
2. **Window → Devices and Simulators** (or press `Shift+Cmd+2`)
3. **Select your device** from left sidebar
4. **Click "Use for Development"** if prompted
5. Device should show as "Connected" with green dot

### Step 3: Configure Signing & Capabilities

1. **Open your iOS project** in Xcode
2. **Select project** in Navigator (top left)
3. **Select your app target**
4. **Go to "Signing & Capabilities" tab**
5. **Select your Team** (Apple Developer account)
6. **Xcode will automatically create provisioning profile**

**Note:** For free Apple ID:
- Bundle Identifier must be unique
- App expires after 7 days
- Limited to 3 apps per device

### Step 4: Select Device and Build

1. **Select your physical device** from device dropdown (top toolbar, next to Run button)
   - Should show your device name (e.g., "John's iPhone")
2. **Click Run** (play button) or press `Cmd+R`
3. **First time only:** On device, go to:
   - Settings → General → VPN & Device Management
   - Tap your developer account
   - Tap "Trust [Your Name]"
   - Tap "Trust" in popup

### Step 5: Verify Installation

- App should launch automatically on device
- Check device home screen for app icon
- If app doesn't launch, check Xcode console for errors

---

## Troubleshooting

### Android Issues

**Device not detected:**
```bash
# Check USB connection
adb devices

# If empty, try:
# 1. Different USB cable
# 2. Different USB port
# 3. Restart ADB server:
adb kill-server
adb start-server
adb devices
```

**"Device unauthorized":**
- On device, tap "Allow USB Debugging"
- Check "Always allow from this computer"
- Try revoking USB debugging authorizations in Developer Options, then reconnect

**Installation fails:**
```bash
# Uninstall existing app first
adb uninstall com.mofa.docattestation.scanner

# Then install again
adb install -r app-debug.apk
```

**App crashes on launch:**
```bash
# View real-time logs
adb logcat -c  # Clear logs
adb logcat | grep -i "docattestation\|error\|exception\|fatal"

# Launch app and watch for errors
adb shell am start -n com.mofa.docattestation.scanner/.MainActivity
```

### iOS Issues

**"No devices available":**
- Ensure device is unlocked
- Trust computer on device
- Check USB cable connection
- Restart Xcode

**Code signing errors:**
- Select correct Team in Signing & Capabilities
- Ensure Bundle Identifier is unique
- For free account, limit is 3 apps per device

**App installs but won't launch:**
- Go to Settings → General → VPN & Device Management
- Trust your developer certificate
- Delete app and reinstall

**"Untrusted Developer":**
1. Settings → General → VPN & Device Management
2. Tap your developer account
3. Tap "Trust [Your Name]"
4. Tap "Trust" in confirmation

---

## Wireless Debugging (Android 11+)

### Enable Wireless Debugging

1. **Enable Developer Options** (tap Build Number 7 times)
2. **Open Developer Options**
3. **Enable "Wireless debugging"**
4. **Tap "Wireless debugging"** to open settings
5. **Tap "Pair device with pairing code"**
6. **Note the IP address and port** (e.g., 192.168.1.100:12345)
7. **Note the pairing code** (6-digit number)

### Connect from Computer

```bash
# Pair device
adb pair <IP_ADDRESS>:<PORT>
# Enter pairing code when prompted

# Connect wirelessly
adb connect <IP_ADDRESS>:<PORT>

# Verify connection
adb devices
```

**Note:** Device and computer must be on same Wi-Fi network.

---

## Quick Reference Commands

### Android

```bash
# List connected devices
adb devices

# Install APK
adb install app-debug.apk

# Install and replace existing
adb install -r app-debug.apk

# Uninstall app
adb uninstall com.mofa.docattestation.scanner

# Launch app
adb shell am start -n com.mofa.docattestation.scanner/.MainActivity

# View logs
adb logcat | grep docattestation

# Clear logs
adb logcat -c

# Restart ADB
adb kill-server
adb start-server

# Take screenshot
adb shell screencap -p /sdcard/screenshot.png
adb pull /sdcard/screenshot.png
```

### iOS

```bash
# List connected devices (from Xcode)
# Window → Devices and Simulators

# View device logs (from Xcode)
# Window → Devices and Simulators → Select device → View Device Logs

# Install via command line (requires ios-deploy)
ios-deploy --bundle YourApp.app --debug
```

---

## Testing on Physical Device

### Before Testing

1. **Verify server is accessible** from device network
   - If testing on local network, use computer's IP address
   - Example: `http://192.168.1.100:5000/api/mobile/health`
   - Not `http://localhost:5000` (localhost = device itself)

2. **Update API base URL** in app configuration:
   ```kotlin
   // Android example
   const val BASE_URL = "http://192.168.1.100:5000"  // Your computer's IP
   // NOT "http://localhost:5000"
   ```

3. **Check firewall** - Allow connections on server port

### Test API Connectivity

**From device browser:**
```
http://YOUR_COMPUTER_IP:5000/api/mobile/health
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

## Network Configuration

### Find Your Computer's IP Address

**Windows:**
```powershell
ipconfig
# Look for IPv4 Address (e.g., 192.168.1.100)
```

**Mac/Linux:**
```bash
ifconfig
# or
ip addr show
# Look for inet address (e.g., 192.168.1.100)
```

### Update App Configuration

**Android (build.gradle or Config.kt):**
```kotlin
object Config {
    // Development
    const val BASE_URL = "http://192.168.1.100:5000"  // Your computer's IP
    
    // Production
    // const val BASE_URL = "https://your-production-server.com"
}
```

**iOS (Config.swift or Info.plist):**
```swift
struct Config {
    static let baseURL = "http://192.168.1.100:5000"  // Your computer's IP
}
```

---

## Security Notes

⚠️ **Important:**
- USB debugging allows full device access - only enable on trusted computers
- Wireless debugging is less secure - use only on trusted networks
- Release builds should be signed with production certificates
- Never commit signing keys or certificates to version control

---

## Next Steps

After deploying to device:
1. Test all app features
2. Test API connectivity
3. Test QR code scanning (if applicable)
4. Monitor device logs for errors
5. Test on different devices/OS versions

For production deployment, see your platform's app store guidelines.

