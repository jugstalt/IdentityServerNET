// Uncomment ONE of the following lines to enable a specific storage backend.
// When a SQL backend is selected, the corresponding container is started automatically.
// Without any define, InMemory storage is used (default).
//#define STORAGE_LITEDB
//#define STORAGE_SQLSERVER
//#define STORAGE_POSTGRE
//#define STORAGE_SQLITE

//#define DBCONTEXT_API   // experimental

// Uncomment to start a Redis container and use it as the authorization parameters message store.
// This keeps OIDC authorize params server-side instead of in the browser ReturnUrl.
//#define USE_REDIS

var builder = DistributedApplication.CreateBuilder(args);

#if USE_REDIS
var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent);
#endif

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithEndpoint(targetPort: 1025, port: 1025, name: "smtp")
    .WithHttpEndpoint(targetPort: 8025, port: 8025, name: "http");

#if STORAGE_SQLSERVER
// AddSqlServer: manages dynamic host port, waits until SQL Server is truly ready,
// and injects the correct connection string (Server=host,PORT;...) at launch time.
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();
var sqlServerDb = sqlServer.AddDatabase("identityserver-db", databaseName: "identityserver");
#elif STORAGE_POSTGRE
// AddContainer with dynamic port avoids conflicts with a local PostgreSQL on port 5432.
// ReferenceExpression resolves the actual host port at launch time.
var postgres = builder.AddContainer("postgres", "postgres", "17")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_DB", "identityserver")
    .WithEndpoint(targetPort: 5432, name: "pg")
    .WithLifetime(ContainerLifetime.Persistent);
var pgEndpoint = postgres.GetEndpoint("pg");
#endif

#if DBCONTEXT_API
var dbContextApi = builder.AddProject<Projects.IdentityServer_DbContext>("identityserver-dbcontext");
#endif

var identityServer = builder.AddProject<Projects.IdentityServer>("identityserver", launchProfileName: "SelfHost")
       .WithEnvironment("IdentityServer__Mail__Smtp__SmtpServer", "localhost")
       .WithEnvironment("IdentityServer__Mail__Smtp__SmtpPort", "1025")
       .WithEnvironment("IdentityServer__Mail__Smtp__FromEmail", "no-reply@dev.local")
       .WithEnvironment("IdentityServer__Mail__Smtp__FromName", "IdentityServer Dev")
       .WaitFor(mailpit)

#if STORAGE_LITEDB
       .WithEnvironment("IdentityServer__ConnectionStrings__LiteDb", "c:\\temp\\identityserver-litedb.db")
#elif STORAGE_SQLSERVER
       // WaitFor(sqlServerDb) waits until SQL Server is actually ready (not just container start).
       // ConnectionStringExpression resolves to Server=host,PORT;... at launch time.
       .WithEnvironment(ctx =>
       {
           ctx.EnvironmentVariables["IdentityServer__ConnectionStrings__SqlServer"] =
               sqlServerDb.Resource.ConnectionStringExpression;
       })
       .WaitFor(sqlServerDb)
#elif STORAGE_POSTGRE
       .WithEnvironment(ctx =>
       {
           ctx.EnvironmentVariables["IdentityServer__ConnectionStrings__Postgres"] =
               ReferenceExpression.Create(
                   $"Host=localhost;Port={pgEndpoint.Property(EndpointProperty.Port)};Database=identityserver;Username=postgres;Password=postgres");
       })
       .WaitFor(postgres)
#elif STORAGE_SQLITE
       .WithEnvironment("IdentityServer__ConnectionStrings__Sqlite", "Data Source=c:\\temp\\identityserver-sqlite.db")
#endif

#if USE_REDIS
       .WithEnvironment("IdentityServer__Stores__ParameterMessageStore", "DistributedRedisCache")
       .WithEnvironment(ctx =>
       {
           ctx.EnvironmentVariables["IdentityServer__Stores__ParameterMessageStoreConnectionString"] =
               redis.Resource.ConnectionStringExpression;
       })
       .WaitFor(redis)
#endif

       //.WithEnvironment(e =>
       //{
       //    e.EnvironmentVariables.Add("IdentityServer__ConnectionStrings__HttpProxy", dbContextApi.Resource.GetEndpoint("https"));
       //})
       //.WaitFor(dbContextApi)
       ;

builder.AddProject<Projects.IdentityServerWebClient>("identityserverwebclient")
       .WithEnvironment("OpenIdConnectAuthentication__Authority", "https://localhost:44300")
       .WithEnvironment("TestClient__Authority", "https://localhost:44300")
       .WithEnvironment("TestClient__ApiClientId", "is-webclient-api")
       .WithEnvironment("TestClient__ApiClientSecret", "secret")
       .WithEnvironment("TestClient__ApiScopes", "is-nova-webapi")
       .WithEnvironment("TestClient__IntrospectionClientId", "is-nova-webapi")
       .WithEnvironment("TestClient__IntrospectionClientSecret", "apisecret")
       .WaitFor(identityServer);

builder.Build().Run();
