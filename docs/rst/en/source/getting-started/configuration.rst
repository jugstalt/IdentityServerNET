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
            "SigningCredential": {  // default: null => certs only in memory
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
      "Storage": "c:\\apps\\identityserver-net\\storage\\validation",  // any path
      "CertPassword": "..."
    }

To sign **tokens**, **IdentityServerNET** requires certificates with private and public keys. Here, you can specify the storage location for these 
certificates. Additionally, a password can be provided to encrypt the certificates. The private key can then only be accessed by applications that know this password.

If this section is not specified, the certificates will be stored **in memory** only.
(All certificates will be lost upon application restart; this should only be used for testing or development!).

Section ``Login``
-----------------

.. code:: javascript

    "Login": {
        "DenyForgotPasswordChallange": true,    // default: false
        "DenyRememberLogin": true,              // default: false,
        "RememberLoginDefaultValue": true,      // default: false
        "DenyLocalLogin": true                  // default: false  
    }

This section allows control over login behavior and options:

* **DenyForgotPasswordChallange:** If set to ``true``, users will not have the option to reset their password via ``Forgot password``.
* **DenyRememberLogin:** If set to ``true``, the ``Remember my login`` option will not be offered at login.
* **RememberLoginDefaultValue:** If set to ``true``, the ``Remember my login`` option will be selected by default.
* **DenyLocalLogin:** If set to ``true``, users cannot log in with a username/password. 
  This can be useful if login should only be possible via *external identity providers*.

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
        "DenyAdminCreateCerts": true        // default: false
    }

This section allows you to control which *admin tools* are available in the **IdentityServerNET** instance:

* **DenyAdminUsers:** User accounts cannot be created or modified by administrators.
* **DenyAdminRoles:** User roles cannot be created or modified by administrators.
* **DenyAdminResources:** Identity and API resources cannot be created or modified by administrators.
* **DenyAdminClients:** Clients cannot be created or modified by administrators.
* **DenyAdminSecretsVault:** The **Secrets Vault** is not available to the administrator.
* **DenySigningUI:** The **Payload Signing** tool is not available to the administrator.
* **DenyAdminCreateCerts:** The **Self-Signed Certificates** tool is not available to the administrator.

This section can be used to restrict the administrative tools. This can be useful if an **IdentityServer** instance is publicly accessible. If a public instance has no admin tools available, it enhances the security of the **IdentityServer databases**.
Administration can, for example, be restricted to an instance that is not accessible over the internet (only intranet, etc.) and that shares the same 
database as the public instance.

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
        }
        // or
        "MailJet": {
            "FromEmail": "no-reply@identityserver.net",
            "FromName": "IdentityServer NET",
        	"ApiKey": "...",
            "ApiSecret": "..."
        }
        // or
        "SendGrid": {
            "FromEmail": "no-reply@identityserver.net",
            "FromName": "IdentityServer NET",
        	"ApiKey": "...",
        }
    }

For ``Forget Password`` and ``Register new user`` actions, emails are sent to the user. This section allows you to specify how these emails are sent.
By default, ``Smtp``, ``MailJet``, and ``SendGrid`` are available. If no option is specified, the email will not be sent but will be output to *logging* instead.
This option should only be used during development.

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

  



