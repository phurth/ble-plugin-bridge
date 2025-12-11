plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

import java.io.ByteArrayOutputStream

fun runGitCommand(vararg args: String): String? {
    return try {
        val stdout = ByteArrayOutputStream()
        exec {
            commandLine("git", *args)
            standardOutput = stdout
            isIgnoreExitValue = true
        }
        stdout.toString().trim().ifEmpty { null }
    } catch (_: Exception) {
        null
    }
}

fun gitDescribe(): String? = runGitCommand("describe", "--tags", "--dirty")
fun gitCommitCount(): Int? = runGitCommand("rev-list", "--count", "HEAD")?.toIntOrNull()

android {
    namespace = "com.onecontrol.blebridge"
    compileSdk = 34

    val defaultVersionName = "1.0.5"
    val defaultVersionCode = 5
    val isReleaseTask = gradle.startParameter.taskNames.any { it.contains("Release", ignoreCase = true) }
    val gitVersionName = if (isReleaseTask) gitDescribe() else null
    val gitVersionCode = if (isReleaseTask) gitCommitCount() else null

    defaultConfig {
        applicationId = "com.onecontrol.blebridge"
        minSdk = 26  // Android 8.0 (for BLE support)
        targetSdk = 34
        versionCode = gitVersionCode ?: defaultVersionCode
        versionName = gitVersionName ?: defaultVersionName

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    signingConfigs {
        create("release") {
            val storeFilePath = (findProperty("ONECONTROL_STORE_FILE") as? String)?.trim()
                ?: error("ONECONTROL_STORE_FILE is not set")
            storeFile = file(storeFilePath)
            storePassword = (findProperty("ONECONTROL_STORE_PASSWORD") as? String)?.trim()
                ?: error("ONECONTROL_STORE_PASSWORD is not set")
            keyAlias = (findProperty("ONECONTROL_KEY_ALIAS") as? String)?.trim()
                ?: error("ONECONTROL_KEY_ALIAS is not set")
            keyPassword = (findProperty("ONECONTROL_KEY_PASSWORD") as? String)?.trim()
                ?: error("ONECONTROL_KEY_PASSWORD is not set")
        }
    }

    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("release")
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
    
    // Use namespace instead of applicationId in defaultConfig (Gradle 8.0+)
    packaging {
        resources {
            excludes += "/META-INF/{AL2.0,LGPL2.1}"
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_1_8
        targetCompatibility = JavaVersion.VERSION_1_8
    }
    kotlinOptions {
        jvmTarget = "1.8"
    }
}

dependencies {
    // AndroidX Core
    implementation("androidx.core:core-ktx:1.12.0")
    implementation("androidx.appcompat:appcompat:1.6.1")
    implementation("com.google.android.material:material:1.11.0")
    implementation("androidx.constraintlayout:constraintlayout:2.1.4")
    
    // MQTT Client (Eclipse Paho - core library only, no Android Service wrapper)
    implementation("org.eclipse.paho:org.eclipse.paho.client.mqttv3:1.2.5")
    
    // JSON handling (Gson)
    implementation("com.google.code.gson:gson:2.10.1")
    
    // WorkManager (for background tasks)
    implementation("androidx.work:work-runtime-ktx:2.9.0")
    
    // Lifecycle components
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.7.0")
    implementation("androidx.lifecycle:lifecycle-service:2.7.0")
    
    // LocalBroadcastManager (for in-app broadcasts)
    implementation("androidx.localbroadcastmanager:localbroadcastmanager:1.1.0")
    
    // Testing
    testImplementation("junit:junit:4.13.2")
    androidTestImplementation("androidx.test.ext:junit:1.1.5")
    androidTestImplementation("androidx.test.espresso:espresso-core:3.5.1")
}

