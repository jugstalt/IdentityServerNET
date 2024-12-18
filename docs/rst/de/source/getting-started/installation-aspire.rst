Installation mit Aspire
=======================

Bei der Entwicklung von Anwendungen kann **IdentityServerNET** über 
den **Aspire Host** als Container gestartet werden.

Voraussetzung ist das NuGet-Paket:

.. code:: 

    dotnet add package Aspire.Hosting.IdentityServer.Hosting

Im Code der *Aspire AppHost*-Anwendung kann der *IdentityServerNET* mit
folgendem Befehl hinzugefügt werden:

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

Mit ``AddIdentityServerNET(containerName)`` wird ein Container mit dem
``identityserver-net-dev``-Image gestartet (https://hub.docker.com/r/gstalt/identityserver-net-dev).

Dieses Image wurde speziell für die Entwicklung erstellt. Da für viele Workflows 
bei der Anmeldung am *IdentityServerNET* eine HTTPS-Verbindung erforderlich ist,
wurde dieses Image mit einem *selbstsignierten Dev-Zertifikat* für die SSL-Verbindungen 
erstellt.

.. note:: 

    Da die Verbindung zum *IdentityServerNET* über ein *selbstsigniertes Zertifikat* 
    erfolgt, können im Browser Warnungen angezeigt werden. Da dieses Image nur für die 
    Entwicklung verwendet werden sollte, können diese Warnungen im Browser ignoriert werden.

Optionale Methoden
------------------

Auf den ``IdentityServerNETResourceBuilder`` können optional noch weitere Methoden 
angewendet werden:

* ``WithMailDev()``: Startet zusätzlich einen MailDev-Server, der zum Testen des 
  Mailversands verwendet werden kann, z. B. für die E-Mail-Bestätigung eines neu registrierten Users.

* ``WithBindMountPersistance()``: Damit Einstellungen in der Entwicklungsumgebung
  des *IdentityServerNET* gespeichert bleiben, kann mit dieser Methode ein Pfad
  für die Speicherung angegeben werden. Wird kein Parameter übergeben, erfolgt 
  die Speicherung der Daten im Verzeichnis ``%USER%/identityserver-net-aspire``.

* ``WithVolumePersistance()``: Ähnlich wie oben, nur dass die Speicherung der 
  Daten in einem Docker-Volume erfolgt. **Achtung:** Hier kann es aufgrund 
  der Rechte des Container-Users zu Zugriffsproblemen kommen.

* ``WithConfiguration(config => {})``: Hier kann die Konfiguration des *IdentityServerNET* angepasst werden.

* ``WithMigrations(migrations => {})``: Über Migrations können beim Start des *IdentityServerNET*
  Objekte wie ``Client``, ``Resources``, ``User``, ``Roles`` angelegt werden. 
  Hier kann ebenfalls ein Administratorpasswort festgelegt werden.

* ``WithExternalProviders(external => {})``: Hier können externe Identity-Provider angegeben werden.
  Derzeit ist *MicrosoftIdentityWeb* implementiert. Die Konfiguration für ``AddMicrosoftIdentityWeb``
  wird in einer Konfigurationssektion definiert:

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

* ``Builder()``: Wandelt den ``IdentityServerNETResourceBuilder`` in einen 
  ``IResourceBuilder`` um, auf den alle anderen Aspire-Resource-Methoden angewendet 
  werden können.

Referenzen
----------

Eine *IdentityServerNET*-Instanz kann mit ``.AddReference(identityServer, configName)`` an ein
Projekt gebunden werden. ``configName`` ist dabei der Name des Wertes aus der Konfiguration
des Projekts, in den die (Aspire-)URL von **IdentityServerNET** geschrieben werden soll.

