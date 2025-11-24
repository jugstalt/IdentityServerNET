# FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled

WORKDIR /app
COPY /artifacts .

# readOnlyRootFilesystem = true works with this:
ENV COMPlus_EnableDiagnostics=0

# ENV ASPNETCORE_URLS=http://*:8080,https://*.8443
# EXPOSE 8080
# EXPOSE 8443

#ENTRYPOINT ["dotnet", "IdentityServer.dll", "--customAppSettings=dev-https"]