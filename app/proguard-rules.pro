# Add project specific ProGuard rules here.
# You can control the set of applied configuration files using the
# proguardFiles setting in build.gradle.

# Keep MQTT classes
-keep class org.eclipse.paho.** { *; }
-dontwarn org.eclipse.paho.**

# Keep Gson classes
-keepattributes Signature
-keepattributes *Annotation*
-keep class sun.misc.Unsafe { *; }
-keep class com.google.gson.** { *; }

