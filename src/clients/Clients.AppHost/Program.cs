var builder = DistributedApplication.CreateBuilder(args);

var nova = builder.AddIdentityServerNova("is-nova-dev")
       .WithMailDev()
       .WithBindMountPersistance()
       .AsResourceBuilder();

builder.AddProject<Projects.ClientApi>("clientapi")
       .WithEnvironment("Authorization__Authority", nova.Resource.HttpsEndpoint);

builder.AddProject<Projects.ClientWeb>("clientweb")
       .WithEnvironment("OpenIdConnectAuthentication__Authority", nova.Resource.HttpsEndpoint)
       .WithReference(nova);

builder.Build().Run();
