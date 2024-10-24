var builder = DistributedApplication.CreateBuilder(args);

var webApp = builder.AddProject<Projects.ClientWeb>("clientweb");
var webApi = builder.AddProject<Projects.ClientApi>("clientapi");

var nova = builder.AddIdentityServerNET("is-nova-dev"/*, bridgeNetwork: "is-nova"*/)
       .WithMailDev()
       //.WithBindMountPersistance()

       .WithMigrations(migrations =>
            migrations
               .AddAdminPassword("admin")
               .AddIdentityResources(["openid", "profile", "role"])
               .AddApiResource("is-nova-webapi", ["query", "command"])
               .AddApiResource("proc-server", ["list", "execute"])
               .AddUserRoles(["custom-role1", "custom-role2", "custom-role2"])
               .WithUser("test@is.nova", "test", ["custom-role2", "custom-role3"])
               .AddClient(ClientType.WebApplication,
                             "is-nova-webclient", "secret",
                            webApp.Resource,
                            [
                                "openid", "profile", "role"
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
       .Build();


webApi
       //.WithHealthCheck("/health")
       .AddReference(nova, "Authorization:Authority")
       .WaitFor(nova);

webApp
       //.WithHealthCheck("/health")
       .AddReference(nova, "OpenIdConnectAuthentication:Authority")
       .WaitFor(nova);

builder.Build().Run();
