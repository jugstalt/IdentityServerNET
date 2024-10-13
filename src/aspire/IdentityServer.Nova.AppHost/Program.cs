using Aspire.Hosting.IdentityServerNova.Utilitities;

var builder = DistributedApplication.CreateBuilder(args);

//var maildev = builder.AddMailDev("maildev", smtpPort: 1025);

//builder.AddProject<Projects.IdentityServer>("identityserver", launchProfileName: "SelfHost")
//       .WithReference(maildev);

builder.AddIdentityServerNova("is-nova-dev")
       .WithPersistance()
       .WithMailDev();

builder.Build().Run();
