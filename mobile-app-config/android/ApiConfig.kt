package com.mofa.docattestation.scanner.config

/**
 * API Configuration for Mobile App
 * Base URL: http://103.175.122.31:81
 */
object ApiConfig {
    // Production Base URL
    const val BASE_URL = "http://103.175.122.31:81"
    
    // API Endpoints
    const val HEALTH = "$BASE_URL/api/mobile/health"
    const val LOGIN = "$BASE_URL/api/auth/login"
    const val REFRESH_TOKEN = "$BASE_URL/api/auth/refresh"
    const val REGISTER_DEVICE = "$BASE_URL/api/mobile/device/register"
    const val VERIFY_QR = "$BASE_URL/api/mobile/verify"
    const val DEVICE_STATUS = "$BASE_URL/api/mobile/device/status"
    const val DEACTIVATE_DEVICE = "$BASE_URL/api/mobile/device"
    
    // App Settings
    const val APP_IDENTIFIER = "com.mofa.docattestation.scanner"
    const val MINIMUM_APP_VERSION = "1.0.0"
    
    // App Secret for HMAC (must match server)
    const val APP_SECRET = "DocAttestMobileApp2024SecretKey64CharsForHMACSHA256Security!!"
    
    // Request Headers
    const val HEADER_APP_VERSION = "X-App-Version"
    const val HEADER_PLATFORM = "X-Platform"
    const val HEADER_AUTHORIZATION = "Authorization"
    const val HEADER_CONTENT_TYPE = "Content-Type"
    
    // Timeouts
    const val CONNECT_TIMEOUT = 30L // seconds
    const val READ_TIMEOUT = 30L // seconds
    const val WRITE_TIMEOUT = 30L // seconds
}

