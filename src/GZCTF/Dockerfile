FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS build
ARG TARGETPLATFORM
COPY publish /build
RUN cp -r /build/${TARGETPLATFORM} /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG TARGETPLATFORM

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8

WORKDIR /app
RUN apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        wget libpcap0.8t64 libgdiplus libkrb5-3 zip unzip \
        ca-certificates libicu74 tzdata && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build --chown=$APP_UID:$APP_UID /publish .

EXPOSE 8080

HEALTHCHECK --interval=5m --timeout=3s --start-period=10s --retries=1 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:3000/healthz || exit 1

ENTRYPOINT ["dotnet", "GZCTF.dll"]
