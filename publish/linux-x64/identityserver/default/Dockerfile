FROM identityserver-net-base:latest

WORKDIR /app
COPY /override .

ENV ASPNETCORE_URLS=http://*:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "IdentityServer.dll"]