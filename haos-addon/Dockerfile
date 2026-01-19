ARG BUILD_FROM
FROM $BUILD_FROM

# Install android-tools (adb)
RUN apk add --no-cache \
    android-tools \
    bash \
    coreutils

# Copy run script
COPY run.sh /usr/bin/
RUN chmod a+x /usr/bin/run.sh

CMD ["/usr/bin/run.sh"]
