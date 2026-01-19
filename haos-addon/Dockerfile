ARG BUILD_FROM
FROM $BUILD_FROM

# Install android-tools (adb)
RUN apk add --no-cache \
    android-tools \
    bash \
    coreutils

# Copy run script to s6-overlay service directory
RUN mkdir -p /etc/services.d/adb-bridge
COPY run.sh /etc/services.d/adb-bridge/run
RUN chmod a+x /etc/services.d/adb-bridge/run
