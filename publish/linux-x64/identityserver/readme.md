# IdentityServerNET Notes

## Dev Certificat

Create New:

```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Export:

```powershell
dotnet dev-certs https -ep ./is-net-dev-https.pfx -p is-net-dev
```

-   `-p`: Certificat password
