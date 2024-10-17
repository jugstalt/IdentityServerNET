string? containerHost = default;

var builder = DistributedApplication.CreateBuilder(args);

var nova = builder.AddIdentityServerNova("is-nova-dev"/*, bridgeNetwork: "is-nova"*/)
       //.WithMailDev()
       //.WithBindMountPersistance()

       // Migrations
       .WithIdentityResources(["openid", "profile", "role"])
       .WithApiResource("my-api", ["query", "command"])
       .WithApiResource("proc-server", ["list", "execute"])
       .WithUserRoles(["custom-role1", "custom-role2", "custom-role2"])
       .WithUser("test@is.nova", "test", ["custom-role2", "custom-role3"])
       .WithClient(ClientType.WebApplication, "web-client", "secret", "http://localhost:8765", ["openid", "profile" ] )

       .AsResourceBuilder();

builder.AddProject<Projects.ClientApi>("clientapi")
       .AddReference(nova, "Authorization:Authority");

builder.AddProject<Projects.ClientWeb>("clientweb")
       .AddReference(nova, "OpenIdConnectAuthentication:Authority");

builder.Build().Run();
