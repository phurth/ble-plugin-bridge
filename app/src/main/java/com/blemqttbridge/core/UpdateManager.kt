package com.blemqttbridge.core

import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Build
import androidx.core.content.FileProvider
import com.blemqttbridge.BuildConfig
import kotlinx.coroutines.*
import java.io.File
import java.io.FileOutputStream
import java.net.URL
import org.json.JSONArray
import org.json.JSONObject

data class Release(
    val tagName: String,
    val name: String,
    val prerelease: Boolean,
    val body: String,
    val apkAsset: ReleaseAsset?
)

data class ReleaseAsset(
    val name: String,
    val downloadUrl: String,
    val size: Long
)

class UpdateManager(private val context: Context) {
    private val scope = CoroutineScope(Dispatchers.Default + Job())
    private val cacheDir = File(context.cacheDir, "apk_downloads")
    
    init {
        cacheDir.mkdirs()
    }
    
    /**
     * Fetch releases from GitHub API
     * Returns list of releases (including pre-releases if requested)
     */
    suspend fun fetchReleases(includePrerelease: Boolean = false): Result<List<Release>> = withContext(Dispatchers.IO) {
        try {
            val url = "https://api.github.com/repos/phurth/ble-plugin-bridge/releases"
            val connection = URL(url).openConnection()
            connection.connectTimeout = 10000
            connection.readTimeout = 10000
            
            val response = connection.inputStream.bufferedReader().use { it.readText() }
            val jsonArray = JSONArray(response)
            
            val releases = mutableListOf<Release>()
            for (i in 0 until jsonArray.length()) {
                val jsonObject = jsonArray.getJSONObject(i)
                val prerelease = jsonObject.optBoolean("prerelease", false)
                
                // Skip pre-releases if not requested
                if (prerelease && !includePrerelease) continue
                
                val tagName = jsonObject.getString("tag_name")
                val name = jsonObject.getString("name")
                val body = jsonObject.optString("body", "")
                
                // Find APK asset
                val apkAsset = extractApkAsset(jsonObject.getJSONArray("assets"))
                
                releases.add(Release(
                    tagName = tagName,
                    name = name,
                    prerelease = prerelease,
                    body = body,
                    apkAsset = apkAsset
                ))
            }
            
            Result.success(releases)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    /**
     * Get latest available release (excluding pre-release unless current version is pre-release)
     */
    suspend fun getLatestRelease(includePrerelease: Boolean = false): Result<Release?> = withContext(Dispatchers.IO) {
        try {
            val releasesResult = fetchReleases(includePrerelease = true)
            if (!releasesResult.isSuccess) {
                return@withContext Result.failure(releasesResult.exceptionOrNull() ?: Exception("Unknown error"))
            }
            
            val releases = releasesResult.getOrNull() ?: return@withContext Result.success(null)
            val currentVersion = BuildConfig.VERSION_NAME
            val currentIsPrerelease = currentVersion.contains("-", ignoreCase = true) || 
                                     currentVersion.contains("beta", ignoreCase = true) ||
                                     currentVersion.contains("alpha", ignoreCase = true)
            
            // Filter out pre-releases unless they're explicitly requested or current is pre-release
            val filteredReleases = if (includePrerelease || currentIsPrerelease) {
                releases
            } else {
                releases.filter { !it.prerelease }
            }
            
            val latest = filteredReleases.firstOrNull { it.apkAsset != null }
            Result.success(latest)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    /**
     * Check if update is available
     */
    suspend fun isUpdateAvailable(includePrerelease: Boolean = false): Boolean = withContext(Dispatchers.IO) {
        try {
            val latestResult = getLatestRelease(includePrerelease)
            if (!latestResult.isSuccess) return@withContext false
            
            val latest = latestResult.getOrNull() ?: return@withContext false
            val currentVersion = normalizeVersion(BuildConfig.VERSION_NAME)
            val latestVersion = normalizeVersion(latest.tagName)
            
            compareVersions(latestVersion, currentVersion) > 0
        } catch (e: Exception) {
            false
        }
    }
    
    /**
     * Download APK from release
     * Returns File path if successful
     */
    suspend fun downloadApk(release: Release, onProgress: (downloaded: Long, total: Long) -> Unit = { _, _ -> }): Result<File> = withContext(Dispatchers.IO) {
        try {
            val asset = release.apkAsset ?: return@withContext Result.failure(Exception("No APK asset found"))
            
            val apkFile = File(cacheDir, asset.name)
            
            // Check if already downloaded and size matches
            if (apkFile.exists() && apkFile.length() == asset.size) {
                return@withContext Result.success(apkFile)
            }
            
            val url = URL(asset.downloadUrl)
            val connection = url.openConnection()
            connection.connectTimeout = 30000
            connection.readTimeout = 30000
            
            val totalSize = connection.contentLength.toLong()
            var downloadedSize = 0L
            
            connection.inputStream.use { input ->
                apkFile.outputStream().use { output ->
                    val buffer = ByteArray(8192)
                    var read: Int
                    while (input.read(buffer).also { read = it } != -1) {
                        output.write(buffer, 0, read)
                        downloadedSize += read
                        onProgress(downloadedSize, totalSize)
                    }
                }
            }
            
            Result.success(apkFile)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    /**
     * Install APK by opening system package installer
     */
    fun installApk(apkFile: File) {
        try {
            val uri = FileProvider.getUriForFile(
                context,
                "${context.packageName}.fileprovider",
                apkFile
            )
            
            val intent = Intent(Intent.ACTION_VIEW).apply {
                setDataAndType(uri, "application/vnd.android.package-archive")
                addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            }
            
            context.startActivity(intent)
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }
    
    /**
     * Clean up old APK files from cache
     */
    fun cleanupCache() {
        try {
            cacheDir.listFiles()?.forEach { file ->
                // Keep only the newest file
                if (file.isFile && file.extension == "apk") {
                    val dayMillis = 24 * 60 * 60 * 1000
                    if (System.currentTimeMillis() - file.lastModified() > dayMillis * 7) {
                        file.delete()
                    }
                }
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }
    
    fun shutdown() {
        scope.cancel()
    }
    
    // Helper functions
    
    private fun compareVersions(v1: List<Int>, v2: List<Int>): Int {
        val maxLen = maxOf(v1.size, v2.size)
        for (i in 0 until maxLen) {
            val num1 = v1.getOrNull(i) ?: 0
            val num2 = v2.getOrNull(i) ?: 0
            if (num1 > num2) return 1
            if (num1 < num2) return -1
        }
        return 0
    }
    
    private fun extractApkAsset(assetsArray: JSONArray): ReleaseAsset? {
        for (i in 0 until assetsArray.length()) {
            val asset = assetsArray.getJSONObject(i)
            val name = asset.getString("name")
            
            if (name.endsWith(".apk")) {
                return ReleaseAsset(
                    name = name,
                    downloadUrl = asset.getString("browser_download_url"),
                    size = asset.getLong("size")
                )
            }
        }
        return null
    }
    
    private fun normalizeVersion(version: String): List<Int> {
        return version
            .removePrefix("v")
            .split(Regex("[.-]"))
            .filter { it.all { c -> c.isDigit() } }
            .map { it.toIntOrNull() ?: 0 }
            .takeIf { it.isNotEmpty() } ?: listOf(0)
    }
}
