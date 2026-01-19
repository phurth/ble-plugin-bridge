ARG BUILD_FROM
FROM $BUILD_FROM

# Install android-tools (adb) and jq for JSON parsing
RUN apk add --no-cache \
    android-tools \
    bash \
    coreutils \
    jq

# Copy run script
COPY run.sh /
RUN chmod a+x /run.sh

CMD [ "/run.sh" ]
