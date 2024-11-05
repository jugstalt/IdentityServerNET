Installation mit Aspire
=======================

Bei der Entwicklung von Anwendungen kann **IdentityServerNET** über 
den **Aspire Host** als Container gestartet werden.

Voraussetzung ist das Nuget Packet:

.. code:: 

    dotnet add package Aspire.Hosting.IdentityServer.Hosting

Im Code der *Aspire AppHost* Anwendungen kann der *IdentityServerNET* mit
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

Mit ``AddIdentityServerNET(containerName)`` wird eine Container mit dem
``identityserver-net-dev`` image gestartet (https://hub.docker.com/r/gstalt/identityserver-net-dev)

Dieses Image ist speziell für die Entwicklung erstellt worden. Da für viele Workflows 
bei der Anmeldung am *IdentityServerNET* eine HTTPS Verbindung notwendig ist,
wurde diese Image mit einem *selbst-signiertem Dev Zertifikat* für die SSL Verbindungen 
erstellt.

.. note:: 

    Da die Verbindung zum *IdentityServerNET* über eine *selbst-signiertes Zertifikat* 
    erfolgt, können im Browser Warnungen angezeigt. Das dieses Image nur für die 
    Entwicklung verwendet werden sollte, können diese Warnungen im Browser ignoriert werden.

Optional Methoden
-----------------

Auf den ``IdentityServerNETResourceBuilder`` können optional noch weiter Methoden
angewendet werden:

* ``WithMailDev()``: Started zusätzlich einen MailDev Server der zum Testen des 
  Mailings verwendet werden kann, zB neu registrierter User muss seine E-Mail 
  verifizieren.

* ``WithBindMountPersistance()``: Damit Einstellungen in der Entwicklungsumgebung
  des *IdentityServerNET* gespeichert bleiben, kann mit dieser Methode ein Pfad
  für die Speicherung angeführt werden. Wird kein Parameter übergeben, erfolgt 
  die Speicherung der Daten im ``%USER%/identityserver-net-aspire`` Verzeichnis.

* ``WithVolumePersistance()``: Ähnlich wie oben, nur das die Speicherung der 
  Daten in einem Docker Volume erfolgt. **Achtung:** hier kann es aufgrund 
  der Rechte des Container Users zu Zugriffsproblemen kommen.

* ``WithConfiguration(config => {})`` Hier kann die Konfiguration des IdentityServerNET angepasst werden.

* ``WithMigrations(migrations => {})`` Über Migrations könne beim Start des IdentityServerNET
  Objekte wie ``Client``, ``Resources``, ``User``, ``Rollen`` angelegt werden. 
  Hier kann ebenfalls ein Administrator Passwort festgelegt werden.

* ``WithExternalProviders(external => {})`` Hier können externe IdentityProvider angeben werden.
  Derzeit ist *MicrosoftIdentityWeb* implementiert. Die Konfiguration für ``AddMicrosoftIdentityWeb``
  wird in einer Config Section definiert:

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
  ``IResourceBuilder`` um, auf den alle anderen Aspire Resource Methoden angewendet 
  werden können.

Referenzen
----------

Eine *IdentityServerNET* Instanz kann mit ``.AddReference(identityServer, configName)`` an ein
Projekt gebunden werden. ``configName`` ist dabei der Name des Wertes aus der Konfiguration
des Projektes, in das der die (Aspire) Url von **IdentityServerNET** geschrieben werden soll.

