using Aspire.Hosting.IdentityServerNova.Utilitities;

var builder = DistributedApplication.CreateBuilder(args);



var nova = builder.AddIdentityServerNova("is-nova-dev")
       .WithMailDev()
       .WithBindMountPersistance()
       .AsResourceBuilder();

//var maildev = builder.AddMailDev("maildev", smtpPort: 1025);

//builder.AddProject<Projects.IdentityServer>("identityserver", launchProfileName: "SelfHost")
//       .WithReference(mailDev);

builder.Build().Run();
