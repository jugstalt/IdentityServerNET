using Aspire.Hosting.IdentityServerNova.Utilitities;

var builder = DistributedApplication.CreateBuilder(args);

var maildev = builder.AddMailDev("maildev", smtpPort: 1025);
var dbContextApi = builder.AddProject<Projects.IdentityServer_DbContext>("identityserver-dbcontext");

builder.AddProject<Projects.IdentityServer>("identityserver", launchProfileName: "SelfHost")
       //.WithReference(mailDev)
    
       //.WithEnvironment(e =>
       //{
       //    e.EnvironmentVariables.Add("IdentityServer__ConnectionStrings__HttpProxy", dbContextApi.Resource.GetEndpoint("https"));
       //})
       //.WaitFor(dbContextApi)
       
       .WaitFor(maildev);


builder.Build().Run();
