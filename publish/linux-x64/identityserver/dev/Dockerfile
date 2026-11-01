# --- Stage 1: generate a fresh HTTPS dev certificate at build time ---
# dotnet dev-certs creates a self-signed cert valid for 1 year from creation,
# so every build produces a certificate valid for at least 1 year from that build.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS certgen

# CERT_BUILD busts the layer cache so the certificate is regenerated on each
# (versioned) build instead of being frozen in a cached layer.
ARG CERT_BUILD=dev
RUN echo "cert build: $CERT_BUILD" \
    && dotnet dev-certs https -ep /is-net-dev-https.pfx -p is-net-dev

# --- Stage 2: runtime image ---
# FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
FROM identityserver-net-base:latest

WORKDIR /app
COPY /override .
# Overwrite the (possibly stale) bundled certificate with the freshly generated one.
# --chmod=0644 makes it readable by the non-root app user (the chiseled image runs
# as UID 64198); without it the file is copied root-owned 0600 and Kestrel fails
# with "Access to the path '/app/is-net-dev-https.pfx' is denied".
COPY --from=certgen --chmod=0644 /is-net-dev-https.pfx ./is-net-dev-https.pfx

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://*:8080,https://*.8443
EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "IdentityServer.dll", "--customAppSettings=dev-https"]
