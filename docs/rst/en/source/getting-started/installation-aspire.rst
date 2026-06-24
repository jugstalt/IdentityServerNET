Installation with Aspire
========================

During application development, **IdentityServerNET** can be started as a container
via the **.NET Aspire AppHost**.

Install the NuGet package in your AppHost project:

.. code::

    dotnet add package IdentityServerNET.Aspire.Hosting

Quick start
-----------

.. code:: csharp

    var builder = DistributedApplication.CreateBuilder(args);

    var webApp = builder.AddProject<Projects.ClientWeb>("clientweb");
    var webApi = builder.AddProject<Projects.ClientApi>("clientapi");

    var identityServer = builder.AddIdentityServerNET("is-net-dev")
        .WithMailPit()                      // start a local mail catcher
        // .WithMailDev()                   // alternative mail catcher
        // .WithBindMountPersistance()      // persist data between restarts

        .WithConfiguration(config => config
            .WithApplicationTitle("My Dev Identity Server")
            .RememberLoginDefaultValue(true)
            .DenyForgotPasswordChallange()
            .DenyManageAccount()
        )
        .WithMigrations(m => m
            .AddAdminPassword("admin")
            .AddIdentityResources(["openid", "profile", "role"])
            .AddApiResource("my-api", ["read", "write"], apiSecret: "apisecret")
            .AddUserRoles(["editor", "viewer"])
            .WithUser("dev@example.com", "dev", ["editor"])
            .AddWebApplicationClient("my-webapp", "secret", webApp,
                scopes: ["openid", "profile", "role"])
            .AddApiClient("my-api-client", "secret",
                scopes: ["my-api", "my-api.read", "my-api.write"])
        )
        .WithExternalProviders(ext => ext
            .AddMicrosoftIdentityWeb(
                builder.Configuration.GetSection("IdentityServer:External:MicrosoftIdentityWeb"))
        )
        .Build();

    webApi.AddReference(identityServer, "Authorization:Authority")
          .WaitFor(identityServer);

    webApp.AddReference(identityServer, "OpenIdConnectAuthentication:Authority")
          .WaitFor(identityServer);

    builder.Build().Run();

``AddIdentityServerNET(containerName)`` starts a container from the
``gstalt/identityserver-net-dev`` image (https://hub.docker.com/r/gstalt/identityserver-net-dev).
This image is built with a **self-signed development certificate** so that HTTPS works
out of the box — a requirement for many OAuth/OIDC flows.

.. note::

    Because the certificate is self-signed, browsers will show a security warning.
    This is expected for development use — you can safely proceed past the warning.

Container options
-----------------

``WithMailPit()``
~~~~~~~~~~~~~~~~~

Starts an `axllent/mailpit <https://hub.docker.com/r/axllent/mailpit>`_ mail catcher
and automatically wires its SMTP address into IdentityServerNET. Use it to inspect
outgoing emails (registration confirmation, password reset, etc.) in a browser UI.

.. code:: csharp

    builder.AddIdentityServerNET("is-net")
        .WithMailPit()            // optional: smtpPort to pin the host port
        .Build();

``WithMailDev()``
~~~~~~~~~~~~~~~~~

Alternative mail catcher using `maildev/maildev <https://hub.docker.com/r/maildev/maildev>`_.
Same behaviour as ``WithMailPit()``. Choose whichever you prefer.

``WithBindMountPersistance(path?)``
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Mounts a host directory into the container so data (users, clients, signing keys, …) survives
container restarts. Without a parameter, data is written to
``%LOCALAPPDATA%/identityserver-net-aspire``.

``WithVolumePersistance(volumeName?)``
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Same as above but uses a named Docker volume instead of a host directory.

.. note::

    Volume permissions can cause problems with some container runtimes.
    Prefer ``WithBindMountPersistance`` for local development.

``Build()``
~~~~~~~~~~~

Returns the underlying ``IResourceBuilder<IdentityServerNetResource>``, giving access
to all standard Aspire resource methods (``WaitFor``, ``WithEnvironment``, etc.).

``WithConfiguration()``
-----------------------

Fine-tune the IdentityServerNET configuration by chaining the available methods inside
the ``WithConfiguration`` callback.

Application
~~~~~~~~~~~

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Method
     - Description
   * - ``WithApplicationTitle(title)``
     - Title shown in the navbar and on the login page.
   * - ``WithPublicOrigin(url)``
     - Public base URL (e.g. ``https://auth.example.com``). Required behind a reverse proxy.

Login
~~~~~

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Method
     - Description
   * - ``DenyLocalLogin(bool)``
     - Disables username/password login (useful when only external providers are active).
   * - ``DenyForgotPasswordChallange(bool)``
     - Hides the *Forgot password* link.
   * - ``DenyRememberLogin(bool)``
     - Removes the *Remember me* checkbox.
   * - ``RememberLoginDefaultValue(bool)``
     - Pre-checks or unchecks *Remember me* by default.

Passkey (WebAuthn / FIDO2)
~~~~~~~~~~~~~~~~~~~~~~~~~~

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Method
     - Description
   * - ``AllowPasskeyPasswordless(bool)``
     - Enables passwordless first-factor sign-in via passkey.
   * - ``AllowPasskeySecondFactor(bool)``
     - Enables passkey as a second factor after username/password.
   * - ``WithPasskeyServerDomain(domain)``
     - WebAuthn Relying Party ID — must match the effective domain (e.g. ``example.com``).
       Leave empty to inherit from the request host.
   * - ``WithPasskeyRelyingPartyName(name)``
     - Human-readable name shown in the authenticator during registration.

.. code:: csharp

    .WithConfiguration(config => config
        .AllowPasskeyPasswordless()
        .WithPasskeyServerDomain("example.com")
        .WithPasskeyRelyingPartyName("My App")
    )

Account
~~~~~~~

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Method
     - Description
   * - ``DenyRegisterAccount(bool)``
     - Disables self-registration.
   * - ``DenyManageAccount(bool)``
     - Hides the account management pages.

Admin UI
~~~~~~~~

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Method
     - Description
   * - ``DenyAdminUsers(bool)``
     - Hides the Users section in the admin area.
   * - ``DenyAdminRoles(bool)``
     - Hides the Roles section.
   * - ``DenyAdminResources(bool)``
     - Hides the API/Identity Resources section.
   * - ``DenyAdminClients(bool)``
     - Hides the Clients section.
   * - ``DenySigningUI(bool)``
     - Hides the Signing Credentials section.
   * - ``DenyAdminCreateCerts(bool)``
     - Prevents admins from creating new signing certificates.
   * - ``AllowDataTransfer(bool)``
     - Shows the Data Transfer (import/export) section.

``WithMigrations()``
--------------------

Migrations seed data into IdentityServerNET on first startup. They only create objects
that do not yet exist — existing entries are never overwritten.

Admin password
~~~~~~~~~~~~~~

.. code:: csharp

    .WithMigrations(m => m
        .AddAdminPassword("admin")
    )

Identity resources
~~~~~~~~~~~~~~~~~~

.. code:: csharp

    .WithMigrations(m => m
        .AddIdentityResources(["openid", "profile", "email", "role"])
        // or one by one:
        .WithIdentityResource("openid")
    )

API resources
~~~~~~~~~~~~~

Scopes are created as ``{name}.{scope}`` sub-scopes. The top-level scope ``{name}``
is always added automatically.

.. code:: csharp

    .WithMigrations(m => m
        .AddApiResource("my-api", ["read", "write"])

        // With an API secret for token introspection:
        .AddApiResource("my-secured-api", ["query"], apiSecret: "apisecret")
    )

If ``apiSecret`` is set, the resource can be used for token introspection.
The secret is stored as a SHA-256 hash. When configuring introspection clients,
use the API resource credentials — not the OAuth client credentials.

Roles
~~~~~

.. code:: csharp

    .WithMigrations(m => m
        .AddUserRoles(["admin", "editor", "viewer"])
        // or:
        .WithUserRole("custom-role")
    )

Users
~~~~~

.. code:: csharp

    .WithMigrations(m => m
        .WithUser("alice@example.com", "password", ["editor", "viewer"])
    )

Clients
~~~~~~~

There are three ways to register clients:

**1. Convenience methods (recommended)**

.. code:: csharp

    .WithMigrations(m => m
        // Authorization Code + PKCE — URL derived from an Aspire resource:
        .AddWebApplicationClient("my-app", "secret", webApp,
            scopes: ["openid", "profile", "role"])

        // Authorization Code + PKCE — URL as a string:
        .AddWebApplicationClient("partner-app", "secret", "https://partner.example.com",
            scopes: ["openid", "profile"],
            additionalGrantTypes: ["urn:ietf:params:oauth:grant-type:device_code"])

        // Machine-to-machine (Client Credentials):
        .AddApiClient("backend-service", "secret",
            scopes: ["my-api", "my-api.read"])

        // JavaScript / SPA (no client secret, PKCE only):
        .AddJavaScriptClient("my-spa", "https://spa.example.com",
            scopes: ["openid", "profile"])
    )

**2. Generic ``AddClient()``**

.. code:: csharp

    .WithMigrations(m => m
        .AddClient(ClientType.WebApplication,
                   clientId: "my-app",
                   clientSecret: "secret",
                   resource: webApp,           // or: clientUrl: "https://..."
                   scopes: ["openid", "profile"],
                   additionalRedirectUris: ["signin-callback"],
                   additionalGrantTypes: ["urn:ietf:params:oauth:grant-type:device_code"])
    )

**ClientType values**

.. list-table::
   :widths: 25 75
   :header-rows: 1

   * - Value
     - Description
   * - ``ClientType.WebApplication``
     - Authorization Code + PKCE. Requires a ``clientUrl`` or Aspire resource.
   * - ``ClientType.ApiClient``
     - Client Credentials (machine-to-machine). No redirect URI needed.
   * - ``ClientType.JavascriptClient``
     - Authorization Code + PKCE without a client secret (SPA).
   * - ``ClientType.Empty``
     - Blank client — configure everything manually via the admin UI.

**Additional client parameters**

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Parameter
     - Description
   * - ``additionalRedirectUris``
     - Extra redirect URIs relative to ``clientUrl``
       (e.g. ``["ManualCode/Callback"]`` for a manual PKCE flow).
   * - ``additionalGrantTypes``
     - Grant types beyond the template default
       (e.g. ``"urn:ietf:params:oauth:grant-type:device_code"``, ``"password"``).

``WithExternalProviders()``
---------------------------

Registers external identity providers.

``AddMicrosoftIdentityWeb(section)``
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Reads configuration from an ``IConfigurationSection``:

.. code:: json

    "IdentityServer": {
      "External": {
        "MicrosoftIdentityWeb": {
          "Name": "Microsoft Identity",
          "Domain": "mydomain.onmicrosoft.com",
          "TenantId": "...",
          "ClientId": "...",
          "ClientSecret": "..."
        }
      }
    }

.. code:: csharp

    .WithExternalProviders(ext => ext
        .AddMicrosoftIdentityWeb(
            builder.Configuration.GetSection(
                "IdentityServer:External:MicrosoftIdentityWeb"))
    )

References
----------

Link IdentityServerNET to another Aspire resource so its URL is injected as a
configuration value:

.. code:: csharp

    webApi.AddReference(identityServer, "Authorization:Authority")
          .WaitFor(identityServer);

    webApp.AddReference(identityServer, "OpenIdConnectAuthentication:Authority")
          .WaitFor(identityServer);

``configName`` is the configuration key in the target project where the Aspire URL
of IdentityServerNET will be written (using ``__`` as the separator).

Dev seeding directly in the project (without a container)
---------------------------------------------------------

When IdentityServerNET is started as an ASP.NET Core project via Aspire
(``builder.AddProject<Projects.IdentityServer>(...)``) rather than as a Docker
container, dev seeding can be configured in ``appsettings.Development.json``
inside the IdentityServer project.

``DevMigrationService`` reads the ``IdentityServer:Migrations`` section at startup
in *Development* mode and automatically creates clients, resources, roles, and users —
only when an in-memory or fresh database is used.

.. code:: json

    {
      "IdentityServer": {
        "Migrations": {
          "AdminPassword": "admin",
          "IdentityResources": [
            { "Name": "openid" },
            { "Name": "profile" },
            { "Name": "role" }
          ],
          "ApiResources": [
            {
              "Name": "my-api",
              "ApiSecret": "apisecret",
              "Scopes": [
                { "Name": "read" },
                { "Name": "write" }
              ]
            }
          ],
          "Roles": [
            { "Name": "editor" },
            { "Name": "viewer" }
          ],
          "Users": [
            {
              "Name": "dev@example.com",
              "Password": "dev",
              "Roles": [ "editor" ]
            }
          ],
          "Clients": [
            {
              "ClientType": "WebApplication",
              "ClientId": "my-webapp",
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
              "Scopes": [ "my-api", "my-api.read", "my-api.write" ]
            }
          ]
        }
      }
    }

.. note::

    ``Migrations`` (with a capital M) is the correct key name as of the current version.
    This section is only applied in *Development* mode — production databases are never
    modified by this configuration.
