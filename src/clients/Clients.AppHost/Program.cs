var builder = DistributedApplication.CreateBuilder(args);

var webApp = builder.AddProject<Projects.ClientWeb>("clientweb");
var webApi = builder.AddProject<Projects.ClientApi>("clientapi");

var identityServer = builder.AddIdentityServerNET("is-net-dev")
       //.WithMailDev()
       //.WithBindMountPersistance()

       .WithConfiguration(config =>
       {
           config
                //.DenyRememberLogin()
                .RememberLoginDefaultValue(true)
                .DenyForgotPasswordChallange()
                .DenyManageAccount()
                //.DenyLocalLogin()
                ;
       })
       .WithMigrations(migrations =>
            migrations
               .AddAdminPassword("admin")
               .AddIdentityResources(["openid", "profile", "role"])
               .AddApiResource("is-nova-webapi", ["query", "command"])
               .AddApiResource("proc-server", ["list", "execute"])
               .AddUserRoles(["custom-role1", "custom-role2", "custom-role2"])
               .WithUser("test@is.net", "test", ["custom-role2", "custom-role3"])
               .AddClient(ClientType.WebApplication,
                             "is-nova-webclient", "secret",
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
                            "is-nova-webapi-commands", "secret",
                            webApi.Resource,
                            [
                                "is-nova-webapi",
                                "is-nova-webapi.query",
                                "is-nova-webapi.command"
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

builder.Build().Run();
