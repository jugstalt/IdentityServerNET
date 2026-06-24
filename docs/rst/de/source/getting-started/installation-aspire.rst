Installation mit Aspire
=======================

Bei der Entwicklung von Anwendungen kann **IdentityServerNET** über den
**.NET Aspire AppHost** als Container gestartet werden.

NuGet-Paket im AppHost-Projekt installieren:

.. code::

    dotnet add package IdentityServerNET.Aspire.Hosting

Schnellstart
------------

.. code:: csharp

    var builder = DistributedApplication.CreateBuilder(args);

    var webApp = builder.AddProject<Projects.ClientWeb>("clientweb");
    var webApi = builder.AddProject<Projects.ClientApi>("clientapi");

    var identityServer = builder.AddIdentityServerNET("is-net-dev")
        .WithMailPit()                      // lokalen Mail-Catcher starten
        // .WithMailDev()                   // alternative Mail-Catcher
        // .WithBindMountPersistance()      // Daten zwischen Neustarts erhalten

        .WithConfiguration(config => config
            .WithApplicationTitle("Mein Dev Identity Server")
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

Mit ``AddIdentityServerNET(containerName)`` wird ein Container aus dem Image
``gstalt/identityserver-net-dev`` gestartet (https://hub.docker.com/r/gstalt/identityserver-net-dev).
Dieses Image ist mit einem **selbstsignierten Entwicklerzertifikat** ausgestattet,
damit HTTPS direkt funktioniert — eine Voraussetzung für viele OAuth/OIDC-Flows.

.. note::

    Da das Zertifikat selbstsigniert ist, erscheinen im Browser Sicherheitswarnungen.
    Das ist bei der Verwendung für die Entwicklung erwartet und kann bedenkenlos
    ignoriert werden.

Container-Optionen
------------------

``WithMailPit()``
~~~~~~~~~~~~~~~~~

Startet einen `axllent/mailpit <https://hub.docker.com/r/axllent/mailpit>`_-Mail-Catcher
und trägt dessen SMTP-Adresse automatisch in IdentityServerNET ein. Ausgehende E-Mails
(Registrierungsbestätigung, Passwort-Reset usw.) können so im Browser-UI eingesehen werden.

.. code:: csharp

    builder.AddIdentityServerNET("is-net")
        .WithMailPit()          // optional: smtpPort für fixen Host-Port
        .Build();

``WithMailDev()``
~~~~~~~~~~~~~~~~~

Alternative zu ``WithMailPit()`` auf Basis von
`maildev/maildev <https://hub.docker.com/r/maildev/maildev>`_. Verhält sich identisch —
je nach Präferenz kann einer der beiden verwendet werden.

``WithBindMountPersistance(path?)``
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Bindet ein Host-Verzeichnis in den Container ein, sodass Daten (Benutzer, Clients,
Signing Keys usw.) Containerneustarts überleben. Ohne Parameter wird ins Verzeichnis
``%LOCALAPPDATA%/identityserver-net-aspire`` gespeichert.

``WithVolumePersistance(volumeName?)``
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Wie oben, aber mit einem benannten Docker-Volume statt einem Host-Verzeichnis.

.. note::

    Volume-Berechtigungen können bei manchen Container-Runtimes Probleme verursachen.
    Für die lokale Entwicklung empfiehlt sich ``WithBindMountPersistance``.

``Build()``
~~~~~~~~~~~

Wandelt den ``IdentityServerNETResourceBuilder`` in einen
``IResourceBuilder<IdentityServerNetResource>`` um, auf dem alle Standard-Aspire-Methoden
(``WaitFor``, ``WithEnvironment`` usw.) verwendet werden können.

``WithConfiguration()``
-----------------------

Konfigurationseinstellungen des IdentityServerNET können im ``WithConfiguration``-Callback
durch Verkettung der verfügbaren Methoden angepasst werden.

Anwendung
~~~~~~~~~

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Methode
     - Beschreibung
   * - ``WithApplicationTitle(title)``
     - Titel in der Navigationsleiste und auf der Login-Seite.
   * - ``WithPublicOrigin(url)``
     - Öffentliche Basis-URL (z. B. ``https://auth.example.com``). Erforderlich hinter
       einem Reverse Proxy.

Login
~~~~~

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Methode
     - Beschreibung
   * - ``DenyLocalLogin(bool)``
     - Deaktiviert den Benutzername-/Passwort-Login (sinnvoll, wenn nur externe
       Provider genutzt werden).
   * - ``DenyForgotPasswordChallange(bool)``
     - Blendet den *Passwort vergessen*-Link aus.
   * - ``DenyRememberLogin(bool)``
     - Entfernt die *Angemeldet bleiben*-Checkbox.
   * - ``RememberLoginDefaultValue(bool)``
     - Setzt den Standardwert der *Angemeldet bleiben*-Checkbox.

Passkey (WebAuthn / FIDO2)
~~~~~~~~~~~~~~~~~~~~~~~~~~

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Methode
     - Beschreibung
   * - ``AllowPasskeyPasswordless(bool)``
     - Aktiviert passwortlose Anmeldung (First Factor) per Passkey.
   * - ``AllowPasskeySecondFactor(bool)``
     - Aktiviert Passkey als zweiten Faktor nach Benutzername/Passwort.
   * - ``WithPasskeyServerDomain(domain)``
     - WebAuthn Relying Party ID — muss der effektiven Domain entsprechen
       (z. B. ``example.com``). Leer lassen, um die Domain des Requests zu übernehmen.
   * - ``WithPasskeyRelyingPartyName(name)``
     - Anzeigename, der dem Benutzer beim Registrieren eines Passkeys angezeigt wird.

.. code:: csharp

    .WithConfiguration(config => config
        .AllowPasskeyPasswordless()
        .WithPasskeyServerDomain("example.com")
        .WithPasskeyRelyingPartyName("Meine App")
    )

Konto
~~~~~

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Methode
     - Beschreibung
   * - ``DenyRegisterAccount(bool)``
     - Deaktiviert die Selbstregistrierung.
   * - ``DenyManageAccount(bool)``
     - Blendet die Konto-Verwaltungsseiten aus.

Admin-Bereich
~~~~~~~~~~~~~

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Methode
     - Beschreibung
   * - ``DenyAdminUsers(bool)``
     - Blendet den Bereich *Benutzer* im Admin aus.
   * - ``DenyAdminRoles(bool)``
     - Blendet den Bereich *Rollen* aus.
   * - ``DenyAdminResources(bool)``
     - Blendet den Bereich *API-/Identity-Ressourcen* aus.
   * - ``DenyAdminClients(bool)``
     - Blendet den Bereich *Clients* aus.
   * - ``DenySigningUI(bool)``
     - Blendet die Signing-Credentials-Verwaltung aus.
   * - ``DenyAdminCreateCerts(bool)``
     - Verhindert, dass Administratoren neue Signing-Zertifikate erstellen.
   * - ``AllowDataTransfer(bool)``
     - Zeigt den Bereich *Datentransfer* (Import/Export) an.

``WithMigrations()``
--------------------

Migrations befüllen IdentityServerNET beim ersten Start mit Daten. Bereits vorhandene
Einträge werden dabei nie überschrieben.

Admin-Passwort
~~~~~~~~~~~~~~

.. code:: csharp

    .WithMigrations(m => m
        .AddAdminPassword("admin")
    )

Identity-Ressourcen
~~~~~~~~~~~~~~~~~~~

.. code:: csharp

    .WithMigrations(m => m
        .AddIdentityResources(["openid", "profile", "email", "role"])
        // oder einzeln:
        .WithIdentityResource("openid")
    )

API-Ressourcen
~~~~~~~~~~~~~~

Sub-Scopes werden als ``{name}.{scope}`` angelegt. Der übergeordnete Scope ``{name}``
wird immer automatisch ergänzt.

.. code:: csharp

    .WithMigrations(m => m
        .AddApiResource("my-api", ["read", "write"])

        // Mit API-Secret für Token Introspection:
        .AddApiResource("my-secured-api", ["query"], apiSecret: "apisecret")
    )

Wird ``apiSecret`` angegeben, kann diese Ressource für Token Introspection genutzt
werden. Das Secret wird als SHA-256-Hash gespeichert. Beim Konfigurieren von
Introspection-Clients müssen die Credentials der API-Ressource (nicht die des
OAuth-Clients) verwendet werden.

Rollen
~~~~~~

.. code:: csharp

    .WithMigrations(m => m
        .AddUserRoles(["admin", "editor", "viewer"])
        // oder:
        .WithUserRole("custom-role")
    )

Benutzer
~~~~~~~~

.. code:: csharp

    .WithMigrations(m => m
        .WithUser("alice@example.com", "passwort", ["editor", "viewer"])
    )

Clients
~~~~~~~

Es gibt drei Wege, Clients zu registrieren:

**1. Komfort-Methoden (empfohlen)**

.. code:: csharp

    .WithMigrations(m => m
        // Authorization Code + PKCE — URL aus einer Aspire-Ressource:
        .AddWebApplicationClient("my-app", "secret", webApp,
            scopes: ["openid", "profile", "role"])

        // Authorization Code + PKCE — URL als String:
        .AddWebApplicationClient("partner-app", "secret", "https://partner.example.com",
            scopes: ["openid", "profile"],
            additionalGrantTypes: ["urn:ietf:params:oauth:grant-type:device_code"])

        // Machine-to-Machine (Client Credentials):
        .AddApiClient("backend-service", "secret",
            scopes: ["my-api", "my-api.read"])

        // JavaScript / SPA (kein Client Secret, nur PKCE):
        .AddJavaScriptClient("my-spa", "https://spa.example.com",
            scopes: ["openid", "profile"])
    )

**2. Generisches ``AddClient()``**

.. code:: csharp

    .WithMigrations(m => m
        .AddClient(ClientType.WebApplication,
                   clientId: "my-app",
                   clientSecret: "secret",
                   resource: webApp,           // oder: clientUrl: "https://..."
                   scopes: ["openid", "profile"],
                   additionalRedirectUris: ["signin-callback"],
                   additionalGrantTypes: ["urn:ietf:params:oauth:grant-type:device_code"])
    )

**ClientType-Werte**

.. list-table::
   :widths: 25 75
   :header-rows: 1

   * - Wert
     - Beschreibung
   * - ``ClientType.WebApplication``
     - Authorization Code + PKCE. Erfordert ``clientUrl`` oder eine Aspire-Ressource.
   * - ``ClientType.ApiClient``
     - Client Credentials (Machine-to-Machine). Keine Redirect-URI erforderlich.
   * - ``ClientType.JavascriptClient``
     - Authorization Code + PKCE ohne Client Secret (SPA).
   * - ``ClientType.Empty``
     - Leerer Client — alles manuell über das Admin-UI konfigurieren.

**Zusätzliche Client-Parameter**

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Parameter
     - Beschreibung
   * - ``additionalRedirectUris``
     - Weitere Redirect-URIs relativ zur ``clientUrl``
       (z. B. ``["ManualCode/Callback"]`` für manuellen PKCE-Flow).
   * - ``additionalGrantTypes``
     - Grant Types zusätzlich zum Template-Standard
       (z. B. ``"urn:ietf:params:oauth:grant-type:device_code"``, ``"password"``).

``WithExternalProviders()``
---------------------------

Registriert externe Identity-Provider.

``AddMicrosoftIdentityWeb(section)``
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Liest die Konfiguration aus einer ``IConfigurationSection``:

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

Referenzen
----------

Eine IdentityServerNET-Instanz kann mit ``.AddReference(identityServer, configName)``
an ein Projekt gebunden werden. ``configName`` ist der Konfigurationsschlüssel im
Zielprojekt, in den die Aspire-URL von IdentityServerNET geschrieben wird
(``__`` als Trennzeichen für verschachtelte Schlüssel).

.. code:: csharp

    webApi.AddReference(identityServer, "Authorization:Authority")
          .WaitFor(identityServer);

    webApp.AddReference(identityServer, "OpenIdConnectAuthentication:Authority")
          .WaitFor(identityServer);

Dev-Seeding direkt im Projekt (ohne Container)
----------------------------------------------

Wenn **IdentityServerNET** nicht als Docker-Container, sondern als ASP.NET Core-Projekt
direkt über Aspire gestartet wird (``builder.AddProject<Projects.IdentityServer>(...)``),
kann das Dev-Seeding über ``appsettings.Development.json`` im IdentityServer-Projekt
konfiguriert werden.

Der ``DevMigrationService`` liest beim Start im *Development*-Modus den Abschnitt
``IdentityServer:Migrations`` und legt automatisch Clients, Ressourcen, Rollen und
Benutzer an — sofern eine InMemory- oder frische Datenbank verwendet wird.

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

    ``Migrations`` (mit großem M, korrekte Schreibweise) ist der gültige Schlüsselname
    ab der aktuellen Version. Dieser Abschnitt wird nur im *Development*-Modus
    ausgewertet — produktive Datenbanken werden durch diese Konfiguration nicht verändert.
