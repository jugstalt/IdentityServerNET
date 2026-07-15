Configuration
=============

The configuration of **IdentityServerNET** is managed through JSON files in the ``_config`` directory.
The configuration file is named ``default.identityserver.net.json``.

.. note::

    In theory, the prefix ``default`` in the filename can also be changed. If the 
    environment variable ``IDENTITY_SERVER_SETTINGS_PREFIX`` is set, that value will be used as the prefix.
    
Structure of the config file:


.. code:: javascript

    {
        "IdentityServer": {  
            "AssemblyName": "...",  // default: IdentityServerNET.ServerExtension.Default
            "ApplicationTitle": "...", // default "IdentityServerNET",
            "PublicOrigin": "https://localhost:44300",
            "StorageRootPath": "c:\\apps\\identityserver-net",
            "ConnectionStrings": {  // default: null => all DBs in Memory
                // ...
            },
            "Crypto": {
                // ...
            },
            "SigningCredential": {  // default: file-based storage under <StorageRootPath>/storage/validation
                // ...
            },
            "Login": {
                // ...
            },
            "Admin": {
                // ...
            },
            "Account": {
                // ...
            },
            "Cookie": {
                // ...
            },
            "Mail": {
                // ...
            },
            "Stores": {
                // ...
            },
            "Configure": {
                // ...
            }
        }
    }

All configuration takes place in the ``IdentityServer`` *section*. This section contains values and 
additional *sections*, which are described below.

Root Values
-----------

* **AssemblyName:** Service configuration is done within an assembly in the program directory.
  This assembly must include a class with the ``[IdentityServerStartup]`` attribute that derives from 
  the ``IIdentityServerStartup`` interface. Methods of this class are called at application startup to 
  register *services*.

  This allows *IdentityServerNET* to be easily customized without modifying the source code of the original application, 
  enabling, for instance, the integration of existing user/role databases.

  Examples will follow later in the section **Customizing/Extending IdentityServerNET**.

  This value can be omitted. In that case, the default assembly 
  ``IdentityServerNET.ServerExtension.Default`` will be used.

* **ApplicationTitle:** The title of the application as displayed in the title bar.

* **PublicOrigin:** The URL of *IdentityServerNET* as displayed in the browser.
  This value is required for various *IdentityServerNET* tools to function,
  such as **Secrets Vault**.

* **StorageRootPath:** Here, you can specify a path where *IdentityServerNET* can store various information, such as the 
  certificates for signing tokens. Within this folder, subfolders are automatically created according to the configuration, e.g., ``storage``, ``secretsvault``, etc.
  If this path is not provided, a **default path** will be used:

  - Windows: ``C:\\apps\\identityserver-net``
  - Linux/OSX: ``/home/app/identityserver-net``


Section ``ConnectionStrings``
-------------------------------

.. code:: javascript

    "ConnectionStrings": {
        "LiteDb": "c:\\apps\\identityserver-net\\is_net.db"
        // or
        "LiteDb": "is_net.db"  // store db in StorageRootPath
        // or
        ...
        "FilesDb": "c:\\apps\\identityserver-net\\storage"  // any path
        // or
        "FilesDB": "~"  // use the StorageRootPath as location
    }

Here, a *connection string* for a *database* can be specified to store users, roles, resources, clients, etc.

By default, data can be stored in a ``LiteDb`` or the file system. If no *connection string* is provided, 
the data will be stored **in memory** (all data is lost when the application restarts; this should only be used for testing or development!).

Alternatively, individual databases can also be stored in different locations. In this case, 
a separate database connection must be specified for each *class*:

.. code:: javascript

    "ConnectionStrings": {
        "Users": { "LiteDb": "is_net.db" },
        "Roles": { "LiteDb": "is_net.db" },
        "Clients": { "AzureStorage": "UseDevelopmentStorage=true" },
        "Resources": { "MongoDb": "mongodb://localhost:27017" },

        // Fallback (here not necessary) 
        "LiteDb": "is_net.db",
    }

The individual *classes* are named ``Users``, ``Roles``, ``Clients``, and ``Resources``.
A connection string can be defined for each *class*. If not all *classes* are specified individually, a fallback connection can be provided.

.. note::

    The ``Clients`` and ``Resources`` classes can also be stored in **Azure Tables**
    or a **MongoDB**.

Starting with version 7, **relational databases** are also available as storage backends:

.. code:: javascript

    "ConnectionStrings": {
        // Microsoft SQL Server
        "SqlServer": "Server=localhost,1433;Database=identityserver;User Id=sa;Password=...;TrustServerCertificate=True"

        // PostgreSQL
        "Postgres": "Host=localhost;Port=5432;Database=identityserver;Username=postgres;Password=postgres"

        // SQLite
        "Sqlite": "Data Source=identityserver.db"
        // or with explicit path
        "Sqlite": "Data Source=c:\\apps\\identityserver-net\\identityserver.db"
    }

* **SqlServer:** Connection string for a **Microsoft SQL Server** database. Tables are created automatically on first startup.
* **Postgres:** Connection string for a **PostgreSQL** database. Tables are created automatically on first startup.
* **Sqlite:** Connection string for a **SQLite** database file. The file and all required directories are created automatically.

SQL backends also support per-class routing to different databases:

.. code:: javascript

    "ConnectionStrings": {
        "Users":     { "SqlServer": "Server=...;Database=identityserver;..." },
        "Roles":     { "SqlServer": "Server=...;Database=identityserver;..." },
        "Clients":   { "Postgres":  "Host=...;Database=identityserver;..." },
        "Resources": { "Sqlite":    "Data Source=identityserver.db" }
    }

.. note::

    All SQL backends store objects (users, roles, clients, resources) as encrypted JSON
    in a ``BlobData`` column — the same pattern as LiteDb. Tables are created automatically
    on the first connection if they do not yet exist. For **PostgreSQL** and **SQLite**,
    usernames and email addresses are always stored and compared in lowercase.

Section ``Crypto``
------------------

.. code:: javascript

    "Crypto": {
        "Method": "key",  // key|data-protection|base64
        "Key": "..."      // protection key, if method=key
    },

Elements created by the administrator (e.g., ``Clients``, ``Resources``, ...) should be stored in an encrypted form, as they may contain **secrets**.

The encryption method can be defined in this section. The following methods are available:

* **key:** Data is encrypted using a key (password). The key must be specified under ``Key`` and be at least 24 characters long.
  This method is easy to use, even if **IdentityServerNET** is scaled across multiple instances. All instances must have the 
  same ``Key`` in their configuration.
  
* **data-protection:** The **Data Protection API** from .NET is used for encryption. If **IdentityServerNET** is scaled across multiple instances,
  it is important to ensure that all instances use the same key ring (see .NET Core Data Protection API).

* **base64:** If none of the above methods are specified, data is **converted to Base64** only. This *encryption* is also easy 
  to implement when **IdentityServerNET** is scaled across multiple instances. However, strictly speaking, this is not *encryption* 
  but *encoding*. The data simply will not appear in plaintext in the database.

Section ``SigningCredential``
-----------------------------

.. code:: javascript

    "SigningCredential": {
      "Storage": "c:\\apps\\identityserver-net\\storage\\validation",  // optional, default: <StorageRootPath>/storage/validation
      "CertPassword": "...",                                          // optional, default: random per-installation password
      "InMemoryOnly": true                                            // optional, default: false
    }

To sign **tokens**, **IdentityServerNET** requires certificates with private and public keys.

By default, these certificates are stored as password-protected files under ``<StorageRootPath>/storage/validation``
(see ``StorageRootPath`` above) — this is the default even if the ``SigningCredential`` section is omitted entirely.
Certificates are renewed automatically in the background and old, no-longer-active certificates are deleted
automatically after a retention period. See :doc:`../internals/signing-certificates` for the full
mechanism (rotation, active window, cleanup) and the advanced ``CheckInterval``/``RenewIfOlderThanDays``/
``CacheDuration`` tuning options.

* **Storage:** Overrides the storage location for the certificates. Optional — defaults to
  ``<StorageRootPath>/storage/validation``.

* **CertPassword:** The password used to encrypt the exported certificate files. Optional — if not set,
  a random password is generated once per installation and persisted (encrypted via the .NET Data
  Protection API) alongside the certificates, instead of relying on a fixed, shared default.

* **InMemoryOnly:** If set to ``true``, certificates are kept in memory only instead of being persisted
  to disk. All certificates are lost on every application restart — only use this for testing or
  development, never in production.

Section ``Login``
-----------------

.. code:: javascript

    "Login": {
        "DenyForgotPasswordChallange": true,    // default: false
        "DenyRememberLogin": true,              // default: false,
        "RememberLoginDefaultValue": true,      // default: false
        "DenyLocalLogin": true,                 // default: false
        "Passkey": {
            "AllowPasswordless": true,          // default: false
            "AllowSecondFactor": true,          // default: false
            "ServerDomain": "identity.my-server.com",
            "RelyingPartyName": "My App"        // default: "IdentityServer"
        }
    }

This section allows control over login behavior and options:

* **DenyForgotPasswordChallange:** If set to ``true``, users will not have the option to reset their password via ``Forgot password``.
* **DenyRememberLogin:** If set to ``true``, the ``Remember my login`` option will not be offered at login.
* **RememberLoginDefaultValue:** If set to ``true``, the ``Remember my login`` option will be selected by default.
* **DenyLocalLogin:** If set to ``true``, users cannot log in with a username/password.
  This can be useful if login should only be possible via *external identity providers*.

Subsection ``Passkey``
~~~~~~~~~~~~~~~~~~~~~~

This subsection configures **Passkey** support (WebAuthn). Passkeys enable secure authentication using
hardware security keys, biometric sensors (fingerprint, face recognition), or a device PIN — without a
traditional password.

* **AllowPasswordless:** If set to ``true``, users can sign in directly with a passkey — no username or
  password required. A ``Sign in with passkey`` button appears on the login page. Users can register
  passkeys under *Manage Account → Passkeys*.

* **AllowSecondFactor:** If set to ``true``, a passkey verification is required after successful password
  entry — provided the user has at least one passkey registered. This provides strong two-factor
  authentication (2FA) without a separate authenticator app.

* **ServerDomain:** The *Relying Party* domain — the hostname under which **IdentityServerNET** is
  accessible (e.g. ``identity.my-server.com``). This value must match the hostname in the ``PublicOrigin``
  URL, since browsers bind passkeys to their registration domain and refuse to use them on other domains.

* **RelyingPartyName:** The display name shown in the browser dialog when registering a new passkey.
  Default: ``IdentityServer``.

.. note::

    Passkeys are domain-bound. A passkey registered for ``identity.my-server.com`` cannot be used on a
    different domain. Ensure ``ServerDomain`` matches the actual public hostname of the server.

Section ``Admin``
-----------------

.. code:: javascript

    "Admin": {
        "DenyAdminUsers": true,             // default: false
        "DenyAdminRoles": true,             // default: false
        "DenyAdminResources": true,         // default: false
        "DenyAdminClients": true,           // default: false
        "DenyAdminSecretsVault": true,      // default: false
        "DenySigningUI": true,              // default: false
        "DenyAdminCreateCerts": true,       // default: false
        "AllowDataTransfer": true           // default: false
    }

This section allows you to control which *admin tools* are available in the **IdentityServerNET** instance:

* **DenyAdminUsers:** User accounts cannot be created or modified by administrators.
* **DenyAdminRoles:** User roles cannot be created or modified by administrators.
* **DenyAdminResources:** Identity and API resources cannot be created or modified by administrators.
* **DenyAdminClients:** Clients cannot be created or modified by administrators.
* **DenyAdminSecretsVault:** The **Secrets Vault** is not available to the administrator.
* **DenySigningUI:** The **Payload Signing** tool is not available to the administrator.
* **DenyAdminCreateCerts:** The **Self-Signed Certificates** tool is not available to the administrator.

* **DenyAdminCreateCerts:** The **Self-Signed Certificates** tool is not available to the administrator.

* **AllowDataTransfer:** If set to ``true``, a **Data Transfer** tile appears in the admin area.
  Administrators can export all users, roles, clients, and resources as a JSON file and import them into
  another instance. Existing entries are skipped during import — nothing is ever overwritten.
  This option should only be enabled during the installation or migration phase and disabled afterwards.

  .. note::

      Password hashes are included in the export and remain compatible as long as the source and target
      instance use the same ASP.NET Identity hashing algorithm. Passkeys are exported but will not work
      on a different domain (WebAuthn is domain-bound).

This section can be used to restrict the administrative tools. This can be useful if an **IdentityServer** instance is publicly accessible. If a public instance has no admin tools available, it enhances the security of the **IdentityServer databases**.
Administration can, for example, be restricted to an instance that is not accessible over the internet (only intranet, etc.) and that shares the same
database as the public instance.

Section ``Security``
--------------------

.. code:: javascript

    "Security": {
        "PasswordHashing": {
            "Template": "{password}{username}"   // default: "{password}"
        }
    }

This optional section configures the input format used by the password hasher.
By default, only the password itself is hashed. For migrations from legacy systems that appended
additional user data as a salt, the template can be adjusted accordingly.

Supported placeholders (always replaced with lowercase values from the user profile):

* ``{password}`` — the plain-text password provided by the user (always required)
* ``{email}`` — the user's email address
* ``{username}`` — the user's username

Example for a legacy system that hashed ``password + username``::

    "Template": "{password}{username}"

.. note::

    The template affects both **new hash creation** and **verification** of existing hashes.
    After a completed migration, the template can be reset to the default ``"{password}"`` —
    hashes that have already been upgraded to PBKDF2 remain valid because the ``SecurePasswordHasher``
    handles the rehash signal correctly.

Section ``Account``
-------------------

.. code:: javascript

   "Account": {
        "DenyManageAccount": true,   // default: false
        "DenyRegisterAccount": true, // default: false
   }

This section allows restrictions related to *user accounts* to be defined:

* **DenyManageAccount:** A logged-in user cannot make changes to their own account. This can be useful if only administrators 
  should manage user accounts, or if account management is handled by another application.

* **DenyRegisterAccount:** Users cannot self-register with IdentityServer.

Section ``Cookie``
------------------

.. code:: javascript 

    "Cookie": {
        "Name": "identityserver-net-identity",
        "Domain": "identity.my-server.com",
        "Path": "/",
        "ExpireDays": 365
    }

The **IdentityServerNET** generates a *cookie* for a logged-in user. Here, you can specify the exact structure of this *cookie*:

* **Name:** The name of the *cookie*
* **Domain:** Specifies the *domain* for which the *cookie* is valid
* **Path:** The path for which the *cookie* is valid
* **ExpireDays:** Specifies how long the *cookie* is valid

Using **Domain** and **Path**, you can restrict when a *cookie* is sent from the browser to the server. Ideally, this *cookie* should only 
be sent to the **IdentityServerNET**!

Section ``Mail``
----------------

.. code:: javascript

    "Mail": {
        "Smtp": {
            "FromEmail": "no-reply@identityserver.net",
            "FromName": "IdentityServer NET",
            "SmtpServer": "localhost",
            "SmtpPort": 1025
        },
        // or
        "MailJet": {
            "FromEmail": "no-reply@identityserver.net",
            "FromName": "IdentityServer NET",
            "ApiKey": "...",
            "ApiSecret": "..."
        },
        // or
        "SendGrid": {
            "FromEmail": "no-reply@identityserver.net",
            "FromName": "IdentityServer NET",
            "ApiKey": "..."
        },
        "TemplatesPath": "custom/mails"   // optional, default: custom/mails
    }

For ``Forget Password`` and ``Register new user`` actions, emails are sent to the user. This section allows
you to specify how these emails are sent. By default, ``Smtp``, ``MailJet``, and ``SendGrid`` are available.
If no option is specified, the email will not be sent but will be output to *logging* instead — this should
only be used during development.

Email Templates
~~~~~~~~~~~~~~~

**IdentityServerNET** supports customizable HTML templates for outgoing emails. On startup, templates are
loaded from the directory specified by ``TemplatesPath`` (default: ``custom/mails`` relative to the
application directory). If a template file is not found, a built-in default template is used as a fallback.

The following template files are supported:

* ``confirm-email.html`` – Sent when a user registers and needs to confirm their email address
* ``reset-password.html`` – Sent when a user requests a password reset
* ``generic.html`` – Default template for any other emails

The following placeholders are available inside templates:

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Placeholder
     - Description
   * - ``{{applicationName}}``
     - Application name (from ``ApplicationTitle``)
   * - ``{{subject}}``
     - Email subject line
   * - ``{{email}}``
     - Recipient email address
   * - ``{{link}}``
     - Action URL (e.g. confirmation or password reset link)
   * - ``{{content}}``
     - Original HTML message content (most useful in the ``generic`` template)
   * - ``{{year}}``
     - Current year (for copyright lines in the footer)

.. note::

    An absolute path for ``TemplatesPath`` is used as-is. A relative path is resolved relative to the
    application base directory. If the folder or a specific template file is not found, the built-in
    fallback template is used silently — no error is raised.

Section ``Stores``
------------------

This optional section controls server-side stores used during the authorization flow.

.. code:: javascript

    "Stores": {
        // Authorization parameters store (login ReturnUrl)
        "ParameterMessageStore": "DistributedMemoryCache",
        // or
        "ParameterMessageStore": "DistributedRedisCache",
        "ParameterMessageStoreConnectionString": "localhost:6379",

        // PAR request_uri store
        "PushedAuthorizationStore": "DistributedMemoryCache",
        // or
        "PushedAuthorizationStore": "DistributedRedisCache",
        "PushedAuthorizationStoreConnectionString": "localhost:6379"
    }

If a store is not configured, the built-in fallback is used:

.. list-table::
   :widths: 30 25 45
   :header-rows: 1

   * - Setting
     - Default (no config)
     - Notes
   * - ``ParameterMessageStore``
     - Parameters in ``ReturnUrl``
     - Long login URL, but no secrets exposed
   * - ``PushedAuthorizationStore``
     - In-process ``ConcurrentDictionary``
     - Lazy expiry, single-instance only

**Background — Pushed Authorization Requests (PAR)**

By default, **IdentityServerNET** supports `Pushed Authorization Requests (PAR) <https://www.rfc-editor.org/rfc/rfc9126>`_
(RFC 9126). With PAR, an OIDC client first POSTs all authorization parameters to the ``/connect/par``
endpoint (server-to-server, backchannel). The server returns a short-lived ``request_uri``. The browser
then only follows a redirect containing ``client_id`` and ``request_uri`` — no sensitive parameters
(``client_secret``, ``scope``, ``code_challenge``, etc.) are ever visible in the browser URL or server
access logs.

The ASP.NET Core OIDC middleware (``Microsoft.AspNetCore.Authentication.OpenIdConnect``, .NET 9+)
automatically uses PAR when the server advertises it in the discovery document. The ``PushedAuthorizationBehavior``
option controls this behavior:

.. code:: csharp

    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.UseIfAvailable; // default

**ParameterMessageStore — Keeping authorize parameters server-side**

During an interactive login flow, IdentityServer must carry the authorization parameters across the
login-page redirect. By default it encodes them into the ``ReturnUrl`` query string, which makes the
login URL long but contains no secrets. To keep the URL short and the parameters entirely server-side,
configure a ``ParameterMessageStore``:

* **DistributedMemoryCache** — parameters are stored in an in-process memory cache. Simple, no
  additional infrastructure required, but **not suitable for multi-instance deployments** (each
  instance has its own memory).

* **DistributedRedisCache** — parameters are stored in a Redis cache. Suitable for production and
  multi-instance deployments. Requires ``ParameterMessageStoreConnectionString``.

.. code:: javascript

    // In-process memory (single instance / development)
    "Stores": {
        "ParameterMessageStore": "DistributedMemoryCache"
    }

    // Redis (production, multi-instance)
    "Stores": {
        "ParameterMessageStore": "DistributedRedisCache",
        "ParameterMessageStoreConnectionString": "redis-host:6379"
    }

When a ``ParameterMessageStore`` is configured, the login-page URL changes from:

.. code::

    /Account/Login?ReturnUrl=/connect/authorize/callback?client_id=...&scope=...&code_challenge=...

to:

.. code::

    /Account/Login?ReturnUrl=/connect/authorize/callback?authzId=<short-opaque-id>

**PushedAuthorizationStore — Distributed store for PAR request_uri**

The PAR ``request_uri`` is stored server-side with a 60-second TTL. By default an in-process
``ConcurrentDictionary`` is used. Expired entries are evicted lazily (on next access), which can
cause unbounded memory growth under heavy load, and the store is not shared across instances.
Configure a distributed store to fix both:

* **DistributedMemoryCache** — in-process memory with automatic TTL eviction. Suitable for
  single-instance deployments.
* **DistributedRedisCache** — Redis-backed, shared across all instances. Required for
  multi-instance deployments.

.. code:: javascript

    // Single instance / development
    "Stores": {
        "PushedAuthorizationStore": "DistributedMemoryCache"
    }

    // Production, multi-instance
    "Stores": {
        "PushedAuthorizationStore": "DistributedRedisCache",
        "PushedAuthorizationStoreConnectionString": "redis-host:6379"
    }

.. note::

    **Aspire:** When the ``#define USE_REDIS`` preprocessor symbol is active in
    ``IdentityServerNET.AppHost/Program.cs``, a Redis container is started automatically and
    **both** the ``ParameterMessageStore`` and the ``PushedAuthorizationStore`` are configured
    via environment variables — no manual connection string setup is needed.

Section ``Endpoints``
---------------------

This optional section controls which IdentityServer protocol endpoints are active.

.. code:: javascript

    "Endpoints": {
        "EnablePushedAuthorization": "false"   // default: true
    }

* **EnablePushedAuthorization:** If set to ``false``, the ``/connect/par`` endpoint is disabled and
  ``pushed_authorization_request_endpoint`` is removed from the discovery document. The ASP.NET Core
  OIDC middleware then falls back to the standard authorization code flow without PAR. Useful for
  testing the plain PKCE flow or for deployments where PAR is not desired.

**RequirePushedAuthorization on Clients**

Individual clients can be required to use PAR exclusively. In the Admin UI under
*Clients → Options*, set ``RequirePushedAuthorization = true``. Any direct call to
``/connect/authorize`` without a prior PAR request will then be rejected with
``invalid_request``.

Section ``RateLimiting``
-------------------------

.. code:: javascript

    "RateLimiting": {
        "TokenEndpoint": {
            "PermitLimit": 30,      // optional, default: 30
            "WindowSeconds": 60     // optional, default: 60
        }
    }

The interactive login page has its own bot-detection/CAPTCHA, but the OAuth token endpoint
(``/connect/token`` — password grant, client credentials, ...) is a separate attack surface that
bypasses it entirely. Requests to ``/connect/token`` are rate limited per client IP address using a
sliding window; every other endpoint is unaffected.

* **PermitLimit:** Maximum number of requests to ``/connect/token`` allowed per IP address within
  ``WindowSeconds``. Additional requests receive ``429 Too Many Requests``.
* **WindowSeconds:** Length of the sliding window, in seconds.

Section ``Configure``
---------------------

Here, the behavior of the **IdentityServerNET** application can be controlled through *middlewares*.

.. code:: javascript

    "Configure": {
        "UseHttpsRedirection": "false",         // default: true
        "AddXForwardedProtoMiddleware": "true"  // default: false
    }

* **UseHttpsRedirection:** The IdentityServer automatically redirects to HTTPS connections. When running in a *Kubernetes* cluster, this may not always 
  be desirable. Within the cluster, the application often runs over the HTTP protocol, but it is accessible via HTTPS only through the *Ingress*.

* **AddXForwardedProtoMiddleware:** **IdentityServerNET** requires access over HTTPS! If the automatic redirection is disabled using **UseHttpsRedirection**,
  the **IdentityServer** may not work as expected. The **XForwardedProtoMiddleware** ensures that the ``X-Forwarded-Proto`` header is respected. 
  If the **IdentityServer** is accessed in a *Kubernetes* cluster through the *Ingress* over HTTPS, the server will still function correctly, 
  even if communication within the cluster uses HTTP.

  



