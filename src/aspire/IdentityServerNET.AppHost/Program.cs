var builder = DistributedApplication.CreateBuilder(args);

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithEndpoint(targetPort: 1025, port: 1025, name: "smtp")
    .WithHttpEndpoint(targetPort: 8025, port: 8025, name: "http");

//var maildev = builder.AddMailDev("maildev", smtpPort: 1025);
var dbContextApi = builder.AddProject<Projects.IdentityServer_DbContext>("identityserver-dbcontext");

var identityServer = builder.AddProject<Projects.IdentityServer>("identityserver", launchProfileName: "SelfHost")
       .WithEnvironment("IdentityServer__Mail__Smtp__SmtpServer", "localhost")
       .WithEnvironment("IdentityServer__Mail__Smtp__SmtpPort", "1025")
       .WithEnvironment("IdentityServer__Mail__Smtp__FromEmail", "no-reply@dev.local")
       .WithEnvironment("IdentityServer__Mail__Smtp__FromName", "IdentityServer Dev")
       .WaitFor(mailpit)

       //.WithEnvironment(e =>
       //{
       //    e.EnvironmentVariables.Add("IdentityServer__ConnectionStrings__HttpProxy", dbContextApi.Resource.GetEndpoint("https"));
       //})
       //.WaitFor(dbContextApi)
       ;

builder.AddProject<Projects.IdentityServerWebClient>("identityserverwebclient")
       .WithEnvironment("OpenIdConnectAuthentication__Authority", "https://localhost:44300")
       .WithEnvironment("TestClient__Authority", "https://localhost:44300")
       .WithEnvironment("TestClient__ApiClientId", "is-webclient-api")
       .WithEnvironment("TestClient__ApiClientSecret", "secret")
       .WithEnvironment("TestClient__ApiScopes", "is-nova-webapi")
       .WithEnvironment("TestClient__IntrospectionClientId", "is-nova-webapi")
       .WithEnvironment("TestClient__IntrospectionClientSecret", "apisecret")
       .WaitFor(identityServer);

builder.Build().Run();
