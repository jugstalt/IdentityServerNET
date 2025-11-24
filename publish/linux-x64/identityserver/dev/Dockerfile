# FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
FROM identityserver-net-base:latest

WORKDIR /app
COPY /override .

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://*:8080,https://*.8443
EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "IdentityServer.dll", "--customAppSettings=dev-https"]