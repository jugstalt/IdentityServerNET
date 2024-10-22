using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Distribution.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.MapInvokeEndpoints<IClientDbContextModify>("api/clients");

app.Run();
