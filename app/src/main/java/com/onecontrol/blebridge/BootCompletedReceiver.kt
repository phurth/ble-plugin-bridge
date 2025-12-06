package com.onecontrol.blebridge

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.SharedPreferences
import android.os.Build
import android.util.Log

class BootCompletedReceiver : BroadcastReceiver() {

    companion object {
        private const val PREFS_NAME = "oc_settings"
        private const val PREF_START_ON_BOOT = "pref_start_on_boot"
    }

    override fun onReceive(context: Context, intent: Intent?) {
        if (intent?.action == Intent.ACTION_BOOT_COMPLETED) {
            val prefs: SharedPreferences =
                context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            val startOnBoot = prefs.getBoolean(PREF_START_ON_BOOT, false)
            if (startOnBoot) {
                Log.i("BootCompletedReceiver", "Starting OneControlBleService after boot")
                val serviceIntent = Intent(context, OneControlBleService::class.java)
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                    context.startForegroundService(serviceIntent)
                } else {
                    context.startService(serviceIntent)
                }
            } else {
                Log.i("BootCompletedReceiver", "Start on boot disabled; not starting service")
            }
        }
    }
}

