# IdentityServer.NET

<p align="left">
  <a href="https://docs.gviewonline.com/other/identityserver-net/en/index.html">
    <img alt="Documentation" src="https://img.shields.io/badge/Documentation-Online-brightgreen?style=flat-square" />
  </a>
  <img alt="License" src="https://img.shields.io/badge/License-Apache%202.0-blue?style=flat-square" />
  <img alt="Version" src="https://img.shields.io/badge/Version-7.26-green?style=flat-square" />
  <img alt="Platform" src="https://img.shields.io/badge/Platform-Windows%20%7C%20Linux-lightgrey?style=flat-square" />
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" />
  <a href="https://hub.docker.com/r/gstalt/identityserver-net">
    <img alt="Docker" src="https://img.shields.io/badge/Docker-gstalt%2Fidentityserver--net-2496ED?style=flat-square&logo=docker" />
  </a>
</p>

A fully self-hosted **OpenID Connect** and **OAuth 2.0** identity provider built on top of [IdentityServer4](https://github.com/IdentityServer/IdentityServer4), packaged as a ready-to-run ASP.NET Core application with a built-in administration UI.

No vendor lock-in, no cloud dependency — runs on-premises, in Docker, or in Kubernetes.

---

## Features

- **OpenID Connect / OAuth 2.0** — authorization code, client credentials, device flow, PKCE, and more
- **Admin UI** — manage clients, API resources, identity resources, users, roles, and secrets through a web interface
- **Multi-Tenancy / Realms** — isolate tenants with separate client and user spaces via the `@realm` convention
- **Pluggable storage** — swap the persistence layer without changing application code
- **Secrets Vault** — encrypted secret storage with version history
- **Consent page** — customizable per-client consent screen with logo upload
- **Appearance editor** — custom logo, background images, and color scheme per installation
- **2FA** — TOTP-based two-factor authentication for users
- **Bot detection** — built-in CAPTCHA on the login page
- **Signing key management** — rotate signing certificates through the UI
- **Docker support** — multi-stage Dockerfile, Linux containers, read-only root filesystem

---

## Storage Providers

| Provider | NuGet / Project |
|---|---|
| File system (default) | built-in |
| LiteDB | `IdentityServerNET.LiteDb` |
| MongoDB | `IdentityServerNET.MongoDb` |
| SQL Server | `IdentityServerNET.SqlServer` |
| SQLite | `IdentityServerNET.Sqlite` |
| PostgreSQL | `IdentityServerNET.Postgres` |
| Azure Blob / Table | `IdentityServerNET.Azure` |

Storage is configured in `appsettings.json` — the application starts with the file-system provider out of the box.

---

## Quick Start

### Run locally

```bash
git clone https://github.com/jugstalt/identityserver.net.git
cd identityserver.net/src/is-net/IdentityServer
dotnet run
```

The admin UI is available at `https://localhost:5001/Admin`.  
Default credentials are seeded from `appsettings.Development.json`.

### Run with .NET Aspire

```bash
cd src/aspire/IdentityServerNET.AppHost
dotnet run
```

### Run in Docker

Pre-built images are available on Docker Hub:

| Image | Purpose |
|---|---|
| [`gstalt/identityserver-net:latest`](https://hub.docker.com/r/gstalt/identityserver-net) | Production base image |
| [`gstalt/identityserver-net-dev:latest`](https://hub.docker.com/r/gstalt/identityserver-net-dev) | Development image (HTTPS, hot-reload config) |

```bash
# Production
docker pull gstalt/identityserver-net:latest
docker run -p 8080:8080 gstalt/identityserver-net:latest

# Development (requires a local HTTPS certificate)
docker pull gstalt/identityserver-net-dev:latest
docker run -p 8080:8080 -p 8443:8443 gstalt/identityserver-net-dev:latest
```

A development HTTPS certificate is required for the dev image. Export it once:

```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
dotnet dev-certs https -ep ./is-net-dev-https.pfx -p is-net-dev
```

To build your own images from source, see [`publish/linux-x64/identityserver/`](publish/linux-x64/identityserver/).

---

## Configuration

The application is configured through `appsettings.json`. Key sections:

| Section | Purpose |
|---|---|
| `IdentityServer` | Clients, resources, signing keys |
| `Storage` | Choose and configure the storage provider |
| `Mail` | SMTP / MailPit settings for email confirmation |
| `Appearance` | Default title, logo, color overrides |
| `BotDetection` | CAPTCHA renderer selection |

Custom configuration can be injected via a server extension project (`IdentityServerNET.ServerExtension.Default`).

---

## Documentation

- [English](https://docs.gviewonline.com/other/identityserver-net/en/index.html)
- [German](https://docs.gviewonline.com/other/identityserver-net/de/index.html)

---

## License

Licensed under the [Apache 2.0 License](LICENSE).
