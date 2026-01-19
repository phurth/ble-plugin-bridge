ARG BUILD_FROM
FROM $BUILD_FROM

# Install android-tools (adb)
RUN apk add --no-cache \
    android-tools \
    bash \
    coreutils

# Copy run script
COPY run.sh /
RUN chmod +x /run.sh

CMD ["/run.sh"]
