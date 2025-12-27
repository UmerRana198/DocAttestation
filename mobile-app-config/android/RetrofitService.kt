package com.mofa.docattestation.scanner.network

import com.mofa.docattestation.scanner.config.ApiConfig
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object RetrofitService {
    
    private val loggingInterceptor = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.BODY
    }
    
    private val okHttpClient = OkHttpClient.Builder()
        .addInterceptor(loggingInterceptor)
        .addInterceptor { chain ->
            val original = chain.request()
            val requestBuilder = original.newBuilder()
                .header(ApiConfig.HEADER_CONTENT_TYPE, "application/json")
                .header(ApiConfig.HEADER_APP_VERSION, ApiConfig.MINIMUM_APP_VERSION)
                .header(ApiConfig.HEADER_PLATFORM, "Android")
            
            // Add Authorization header if token exists
            val token = TokenManager.getAccessToken()
            if (!token.isNullOrEmpty()) {
                requestBuilder.header(ApiConfig.HEADER_AUTHORIZATION, "Bearer $token")
            }
            
            chain.proceed(requestBuilder.build())
        }
        .connectTimeout(ApiConfig.CONNECT_TIMEOUT, TimeUnit.SECONDS)
        .readTimeout(ApiConfig.READ_TIMEOUT, TimeUnit.SECONDS)
        .writeTimeout(ApiConfig.WRITE_TIMEOUT, TimeUnit.SECONDS)
        .build()
    
    private val retrofit = Retrofit.Builder()
        .baseUrl(ApiConfig.BASE_URL)
        .client(okHttpClient)
        .addConverterFactory(GsonConverterFactory.create())
        .build()
    
    val apiService: ApiService = retrofit.create(ApiService::class.java)
}

// Token Manager (store in SharedPreferences or secure storage)
object TokenManager {
    private const val PREFS_NAME = "app_prefs"
    private const val KEY_ACCESS_TOKEN = "access_token"
    private const val KEY_REFRESH_TOKEN = "refresh_token"
    
    fun saveAccessToken(token: String) {
        // Save to SharedPreferences or secure storage
    }
    
    fun getAccessToken(): String? {
        // Retrieve from SharedPreferences or secure storage
        return null
    }
    
    fun saveRefreshToken(token: String) {
        // Save to SharedPreferences or secure storage
    }
    
    fun getRefreshToken(): String? {
        // Retrieve from SharedPreferences or secure storage
        return null
    }
    
    fun clearTokens() {
        // Clear tokens
    }
}

