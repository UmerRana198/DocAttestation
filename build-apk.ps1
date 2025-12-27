# PowerShell script to build Android APK with production base URL

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building APK with Production Base URL" -ForegroundColor Cyan
Write-Host "Base URL: http://103.175.122.31:81" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if we're in Android project directory
if (-not (Test-Path "build.gradle") -and -not (Test-Path "settings.gradle")) {
    Write-Host "❌ Error: Not in Android project directory!" -ForegroundColor Red
    Write-Host "Please navigate to your Android project root and run this script." -ForegroundColor Yellow
    exit 1
}

# Clean previous builds
Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
.\gradlew clean

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Clean failed!" -ForegroundColor Red
    exit 1
}

# Build debug APK
Write-Host ""
Write-Host "🔨 Building Debug APK..." -ForegroundColor Yellow
.\gradlew assembleDebug

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Debug APK built successfully!" -ForegroundColor Green
    Write-Host "📦 Location: app\build\outputs\apk\debug\app-debug.apk" -ForegroundColor Cyan
    Write-Host ""
    
    # Get APK size
    $apkPath = "app\build\outputs\apk\debug\app-debug.apk"
    if (Test-Path $apkPath) {
        $apkSize = (Get-Item $apkPath).Length / 1MB
        Write-Host "📊 APK Size: $([math]::Round($apkSize, 2)) MB" -ForegroundColor Cyan
        Write-Host ""
    }
    
    # Check if device is connected
    $devices = adb devices
    if ($devices -match "device$") {
        $response = Read-Host "📱 Device detected. Install APK? (y/n)"
        if ($response -eq "y" -or $response -eq "Y") {
            Write-Host "📲 Installing APK..." -ForegroundColor Yellow
            adb install -r $apkPath
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ APK installed successfully!" -ForegroundColor Green
                Write-Host ""
                Write-Host "🚀 Launching app..." -ForegroundColor Yellow
                adb shell am start -n com.mofa.docattestation.scanner/.MainActivity
            }
        }
    } else {
        Write-Host "ℹ️  No device connected. APK ready for manual installation." -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "❌ Build failed! Check errors above." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

