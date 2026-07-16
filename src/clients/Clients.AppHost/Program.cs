var builder = DistributedApplication.CreateBuilder(args);

var webApp = builder.AddProject<Projects.ClientWeb>("clientweb");
var webApi = builder.AddProject<Projects.ClientApi>("clientapi");
var testClient = builder.AddProject<Projects.IdentityServerWebClient>("identityserverwebclient");

var identityServer = builder.AddIdentityServerNET("is-net-dev")
       //.WithMailDev()
       .WithMailPit()
       .WithBindMountPersistance()

       .WithConfiguration(config =>
       {
           config
                //.DenyRememberLogin()
                .RememberLoginDefaultValue(true)
                .DenyForgotPasswordChallange()
                //.DenyManageAccount()
                //.DenyLocalLogin()
                .AllowPasskeyPasswordless()
              
                .WithApplicationTitle("xxx")
                ;
       })
       .WithMigrations(migrations =>
            migrations
               .AddAdminPassword("admin")
               .AddIdentityResources(["openid", "profile", "role"])
               .AddApiResource("is-net-webapi", ["query", "command"])
               .AddApiResource("proc-server", ["list", "execute"])
               .AddUserRoles(["custom-role1", "custom-role2", "custom-role2"])
               .WithUser("test@is.net", "test", ["custom-role2", "custom-role3"])
               .AddClient(ClientType.WebApplication,
                             "is-net-webclient", "secret",
                            webApp.Resource,
                            [
                                "openid", "profile", "role"
                            ])
               .AddClient(ClientType.WebApplication,
                          "local-webgis-portal", "secret",
                          "https://localhost:44320",
                          [
                                "openid", "profile",
                          ])
               .AddClient(ClientType.ApiClient,
                            "is-net-webapi-commands", "secret",
                            webApi.Resource,
                            [
                                "is-net-webapi",
                                "is-net-webapi.query",
                                "is-net-webapi.command"
                           ])
               .AddClient(ClientType.WebApplication,
                            "is-webclient-test", "secret",
                            testClient.Resource,
                            [
                                "openid", "profile", "role", "offline_access"
                            ])
               .AddClient(ClientType.ApiClient,
                            "is-webclient-api", "secret",
                            testClient.Resource,
                            [
                                "is-net-webapi",
                                "is-net-webapi.query",
                                "is-net-webapi.command"
                           ])
       )
       .WithExternalProviders(external =>
       {
           external.AddMicrosoftIdentityWeb(
               builder.Configuration.GetSection("IdentityServer:External:MicrosoftIdentityWeb"));
       })
       .Build();


webApi
       //.WithHealthCheck("/health")
       .AddReference(identityServer, "Authorization:Authority")
       .WaitFor(identityServer);

webApp
       //.WithHealthCheck("/health")
       .AddReference(identityServer, "OpenIdConnectAuthentication:Authority")
       .WaitFor(identityServer);

testClient
       .AddReference(identityServer, "OpenIdConnectAuthentication:Authority")
       .AddReference(identityServer, "TestClient:Authority")
       .WaitFor(identityServer);

builder.Build().Run();
