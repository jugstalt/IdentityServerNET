string? containerHost = default;

var builder = DistributedApplication.CreateBuilder(args);

var nova = builder.AddIdentityServerNova("is-nova-dev", bridgeNetwork: "is-nova")
       .WithMailDev()
       .WithBindMountPersistance()
       .AsResourceBuilder();

builder.AddProject<Projects.ClientApi>("clientapi")
       .AddReference(nova, "Authorization:Authority");

builder.AddProject<Projects.ClientWeb>("clientweb")
       .AddReference(nova, "OpenIdConnectAuthentication:Authority");

builder.Build().Run();
