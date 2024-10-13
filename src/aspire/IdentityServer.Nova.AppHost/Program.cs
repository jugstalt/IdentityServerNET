var builder = DistributedApplication.CreateBuilder(args);

var maildev = builder.AddMailDev("maildev", smtpPort: 1025);

builder.AddProject<Projects.IdentityServer>("identityserver", launchProfileName: "SelfHost")
       .WithReference(maildev);

builder.Build().Run();
