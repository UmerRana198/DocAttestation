#!/bin/bash
# Build script for Android APK with production base URL

echo "========================================"
echo "Building APK with Production Base URL"
echo "Base URL: http://103.175.122.31:81"
echo "========================================"
echo ""

# Check if we're in Android project directory
if [ ! -f "build.gradle" ] && [ ! -f "settings.gradle" ]; then
    echo "❌ Error: Not in Android project directory!"
    echo "Please navigate to your Android project root and run this script."
    exit 1
fi

# Clean previous builds
echo "🧹 Cleaning previous builds..."
./gradlew clean

# Build debug APK
echo ""
echo "🔨 Building Debug APK..."
./gradlew assembleDebug

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Debug APK built successfully!"
    echo "📦 Location: app/build/outputs/apk/debug/app-debug.apk"
    echo ""
    
    # Get APK size
    APK_SIZE=$(du -h app/build/outputs/apk/debug/app-debug.apk | cut -f1)
    echo "📊 APK Size: $APK_SIZE"
    echo ""
    
    # Check if device is connected
    if adb devices | grep -q "device$"; then
        echo "📱 Device detected. Install APK? (y/n)"
        read -r response
        if [[ "$response" =~ ^[Yy]$ ]]; then
            echo "📲 Installing APK..."
            adb install -r app/build/outputs/apk/debug/app-debug.apk
            if [ $? -eq 0 ]; then
                echo "✅ APK installed successfully!"
                echo ""
                echo "🚀 Launching app..."
                adb shell am start -n com.mofa.docattestation.scanner/.MainActivity
            fi
        fi
    else
        echo "ℹ️  No device connected. APK ready for manual installation."
    fi
else
    echo ""
    echo "❌ Build failed! Check errors above."
    exit 1
fi

echo ""
echo "========================================"
echo "Build Complete!"
echo "========================================"

