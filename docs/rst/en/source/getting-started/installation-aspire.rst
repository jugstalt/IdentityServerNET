Installation with Aspire
========================

During application development, **IdentityServerNET** can be started as a container 
via the **Aspire Host**.

The required NuGet package is:

.. code:: 

    dotnet add package Aspire.Hosting.IdentityServer.Hosting

In the code of the *Aspire AppHost* application, *IdentityServerNET* can be added with the 
following command:

.. code:: csharp

    var builder = DistributedApplication.CreateBuilder(args);

    var webApp = builder.AddProject<Projects.ClientWeb>("clientweb");
    var webApi = builder.AddProject<Projects.ClientApi>("clientapi");

    var identityServer = builder.AddIdentityServerNET("is-net-dev")
       //.WithMailDev()
       //.WithBindMountPersistance()  // we dont need persitance, everything is setup on start with migrations

       .WithConfiguration(config =>
       {
           config
                //.DenyRememberLogin()
                .RememberLoginDefaultValue(true)
                .DenyForgotPasswordChallange()
                .DenyManageAccount()
                //.DenyLocalLogin()
                ;
       })
       .WithMigrations(migrations =>
            migrations
               .AddAdminPassword("admin")
               .AddIdentityResources(["openid", "profile", "role"])
               .AddApiResource("is-nova-webapi", ["query", "command"])
               .AddApiResource("proc-server", ["list", "execute"])
               .AddUserRoles(["custom-role1", "custom-role2", "custom-role2"])
               .WithUser("test@is.net", "test", ["custom-role2", "custom-role3"])
               .AddClient(ClientType.WebApplication,
                          "is-net-webclient", "secret",
                          webApp.Resource,
                          [
                              "openid", "profile", "role"
                          ])
               .AddClient(ClientType.WebApplication,
                          "local-webgis-portal", "secret",
                          "https://localhost:44320",
                          [
                                "openid", "profile",
                          ])
               .AddClient(ClientType.ApiClient,
                            "is-net-webapi-commands", "secret",
                            webApi.Resource,
                            [
                                "is-net-webapi",
                                "is-net-webapi.query",
                                "is-net-webapi.command"
                           ])
       )
       .WithExternalProviders(external =>
       {
           external.AddMicrosoftIdentityWeb(
               builder.Configuration.GetSection("IdentityServer:External:MicrosoftIdentityWeb"));
       })
       .Build();


    webApi.AddReference(identityServer, "Authorization:Authority")
          .WaitFor(identityServer);

    webApp.AddReference(identityServer, "OpenIdConnectAuthentication:Authority")
          .WaitFor(identityServer);

    builder.Build().Run();

Using ``AddIdentityServerNET(containerName)`` starts a container with the 
``identityserver-net-dev`` image (https://hub.docker.com/r/gstalt/identityserver-net-dev).

This image was specifically created for development. Since many workflows 
for *IdentityServerNET* login require an HTTPS connection, this image was built 
with a *self-signed development certificate* for SSL connections.

.. note:: 

    Since the connection to *IdentityServerNET* uses a *self-signed certificate*, 
    warnings may appear in the browser. As this image is intended solely for 
    development, these warnings can be ignored in the browser.

Optional Methods
----------------

The ``IdentityServerNETResourceBuilder`` allows additional optional methods to be applied:

* ``WithMailDev()``: Also starts a MailDev server, which can be used to test 
  email functions, such as for a newly registered user who needs to verify their email.

* ``WithBindMountPersistance()``: To save settings within the development environment
  of *IdentityServerNET*, a path for data storage can be specified using this method. 
  If no parameter is provided, data is stored in the ``%USER%/identityserver-net-aspire`` directory.

* ``WithVolumePersistance()``: Similar to the above, but stores data in a Docker volume. **Note:** This may cause 
  access issues due to container user permissions.

* ``WithConfiguration(config => {})``: Here, the *IdentityServerNET* configuration can be customized.

* ``WithMigrations(migrations => {})``: Migrations allow objects such as ``Client``, ``Resources``, ``User``, and ``Roles`` to be created 
  when *IdentityServerNET* starts. An administrator password can also be set here.

* ``WithExternalProviders(external => {})``: External identity providers can be specified here.
  Currently, *MicrosoftIdentityWeb* is implemented. Configuration for ``AddMicrosoftIdentityWeb`` 
  is defined in a configuration section:

  .. code:: json

    "IdentityServer": {
      // ...
      "External": {
        "MicrosoftIdentityWeb": {
          "Name": "Microsoft Identity",
          "Domain": "mydomain.onmicrosoft.com",
          "TenantId": "...",
          "ClientId": "...",
          "ClientSecret": ""
        }
      }
    }

* ``Builder()``: Converts the ``IdentityServerNETResourceBuilder`` into an 
  ``IResourceBuilder``, allowing all other Aspire resource methods to be applied.

References
----------

An *IdentityServerNET* instance can be linked to a project with ``.AddReference(identityServer, configName)``.
``configName`` is the name of the key in the project’s configuration where the (Aspire) URL of **IdentityServerNET**
should be written.

Dev Seeding directly in the project (without a container)
---------------------------------------------------------

When **IdentityServerNET** is started as an ASP.NET Core project directly via *Aspire*
(e.g. ``builder.AddProject<Projects.IdentityServer>(...)``) rather than as a Docker container, dev seeding
can be configured through ``appsettings.Development.json`` in the IdentityServer project.

The ``DevMigrationService`` reads the ``IdentityServer:Migragions`` section at startup in *Development* mode
and automatically creates clients, resources, roles, and users — provided an in-memory database is used.

.. code:: json

    {
      "IdentityServer": {
        "Migragions": {
          "AdminPassword": "admin",
          "IdentityResources": [
            { "Name": "openid" }, { "Name": "profile" }, { "Name": "role" }
          ],
          "ApiResources": [
            {
              "Name": "my-api",
              "ApiSecret": "apisecret",
              "Scopes": [ { "Name": "query" }, { "Name": "command" } ]
            }
          ],
          "Roles": [ { "Name": "admin" }, { "Name": "user" } ],
          "Users": [
            { "Name": "test@example.com", "Password": "test", "Roles": [ "user" ] }
          ],
          "Clients": [
            {
              "ClientType": "WebApplication",
              "ClientId": "my-webclient",
              "ClientSecret": "secret",
              "ClientUrl": "https://localhost:44360",
              "AdditionalRedirectUris": [ "ManualCode/Callback" ],
              "AdditionalGrantTypes": [ "urn:ietf:params:oauth:grant-type:device_code" ],
              "Scopes": [ "openid", "profile", "role", "offline_access" ]
            },
            {
              "ClientType": "ApiClient",
              "ClientId": "my-api-client",
              "ClientSecret": "secret",
              "Scopes": [ "my-api", "my-api.query", "my-api.command" ]
            }
          ]
        }
      }
    }

**ClientType values:**

* ``WebApplication`` – Interactive client (Authorization Code + PKCE). Requires ``ClientUrl``.
* ``ApiClient`` – Machine-to-machine client (Client Credentials).

**Optional client fields:**

* ``AdditionalRedirectUris`` – Additional redirect URIs relative to ``ClientUrl``
  (e.g. ``"ManualCode/Callback"`` for a manual PKCE flow).
* ``AdditionalGrantTypes`` – Extra grant types beyond what the client template provides
  (e.g. ``"urn:ietf:params:oauth:grant-type:device_code"`` or ``"password"``).

**``ApiSecret`` on API resources:**

If ``ApiSecret`` is set, the resource can be used for token introspection. The secret is stored as a
SHA-256 hash. When configuring introspection, the API resource credentials (not the OAuth client) must
be used.

.. note::

    The key ``Migragions`` (with the typo) matches the actual key name in the source code — use it as-is.
    This section is only applied in *Development* mode with in-memory databases; production databases
    are not modified by this configuration.


