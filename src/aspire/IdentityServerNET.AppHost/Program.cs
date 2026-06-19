var builder = DistributedApplication.CreateBuilder(args);

//var maildev = builder.AddMailDev("maildev", smtpPort: 1025);
var dbContextApi = builder.AddProject<Projects.IdentityServer_DbContext>("identityserver-dbcontext");

var identityServer = builder.AddProject<Projects.IdentityServer>("identityserver", launchProfileName: "SelfHost")
       //.WithReference(mailDev)

       //.WithEnvironment(e =>
       //{
       //    e.EnvironmentVariables.Add("IdentityServer__ConnectionStrings__HttpProxy", dbContextApi.Resource.GetEndpoint("https"));
       //})
       //.WaitFor(dbContextApi)

       //.WaitFor(maildev)
       ;

builder.AddProject<Projects.IdentityServerWebClient>("identityserverwebclient")
       .WithEnvironment("OpenIdConnectAuthentication__Authority", "https://localhost:44300")
       .WithEnvironment("TestClient__Authority", "https://localhost:44300")
       .WithEnvironment("TestClient__ApiClientId", "is-nova-webapi")
       .WithEnvironment("TestClient__ApiClientSecret", "apisecret")
       .WaitFor(identityServer);

builder.Build().Run();
