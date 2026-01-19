ARG BUILD_FROM
FROM $BUILD_FROM

# Install android-tools (adb), eudev for device rules, and utilities
RUN apk add --no-cache \
    android-tools \
    bash \
    coreutils \
    eudev \
    jq

# Add Android udev rules
RUN mkdir -p /etc/udev/rules.d && \
    echo '# Android ADB devices' > /etc/udev/rules.d/51-android.rules && \
    echo 'SUBSYSTEM=="usb", ATTR{idVendor}=="18d1", MODE="0666", GROUP="plugdev"' >> /etc/udev/rules.d/51-android.rules && \
    echo 'SUBSYSTEM=="usb", ATTR{idVendor}=="04e8", MODE="0666", GROUP="plugdev"' >> /etc/udev/rules.d/51-android.rules && \
    echo 'SUBSYSTEM=="usb", ATTR{idVendor}=="0fce", MODE="0666", GROUP="plugdev"' >> /etc/udev/rules.d/51-android.rules && \
    echo 'SUBSYSTEM=="usb", ATTR{idVendor}=="0bb4", MODE="0666", GROUP="plugdev"' >> /etc/udev/rules.d/51-android.rules && \
    echo 'SUBSYSTEM=="usb", ATTR{idVendor}=="22b8", MODE="0666", GROUP="plugdev"' >> /etc/udev/rules.d/51-android.rules && \
    echo 'SUBSYSTEM=="usb", ATTR{idVendor}=="1004", MODE="0666", GROUP="plugdev"' >> /etc/udev/rules.d/51-android.rules && \
    echo 'SUBSYSTEM=="usb", ATTR{idVendor}=="12d1", MODE="0666", GROUP="plugdev"' >> /etc/udev/rules.d/51-android.rules && \
    echo 'SUBSYSTEM=="usb", ATTR{idVendor}=="2717", MODE="0666", GROUP="plugdev"' >> /etc/udev/rules.d/51-android.rules && \
    echo 'SUBSYSTEM=="usb", ATTR{idVendor}=="1949", MODE="0666", GROUP="plugdev"' >> /etc/udev/rules.d/51-android.rules

# Copy run script
COPY run.sh /
RUN chmod a+x /run.sh

CMD [ "/run.sh" ]
