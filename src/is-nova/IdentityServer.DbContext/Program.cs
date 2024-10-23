using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Distribution.Extensions;
using IdentityServer.Nova.Distribution.Services;
using IdentityServer.Nova.Services.DbContext;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.AddServiceDefaults();
#endif

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IClientDbContextModify, InMemoryClientDb>();
builder.Services.AddTransient<IResourceDbContextModify, InMemoryResourceDb>();
builder.Services.AddTransient<IAdminRoleDbContext, InMemoryRoleDb>();
builder.Services.AddTransient<IAdminUserDbContext, InMemoryUserDb>();

builder.Services.AddTransient<InvokerService<IClientDbContextModify>>();
builder.Services.AddTransient<InvokerService<IResourceDbContextModify>>();
builder.Services.AddTransient<InvokerService<IAdminRoleDbContext>>();
builder.Services.AddTransient<InvokerService<IAdminUserDbContext>>();


var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.MapInvokeEndpoints<IClientDbContextModify>("api/clients");
app.MapInvokeEndpoints<IResourceDbContextModify>("api/resources");
app.MapInvokeEndpoints<IAdminRoleDbContext>("api/roles");
app.MapInvokeEndpoints<IAdminUserDbContext>("api/users");

app.Run();
