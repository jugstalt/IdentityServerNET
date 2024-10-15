Installation mit Aspire
=======================

Bei der Entwicklung von Anwendungen kann **IdentityServer.Nova** über 
den **Aspire Host** als Container gestartet werden.

Voraussetzung ist das Nuget Packet:

.. code:: 

    dotnet add package Aspire.Hosting.IdentityServer.Hosting

Im Code der *Aspire AppHost* Anwendungen kann der *IdentityServer.Nova* mit
folgendem Befehl hinzugefügt werden:

.. code:: csharp

    var builder = DistributedApplication.CreateBuilder(args);

    // ...

    var nova = builder.AddIdentityServerNova(containerName: "is-nova-dev")
       .WithMailDev()              // optional
       .WithBindMountPersistance() // optional
       .AsResourceBuilder();       // optional

    // ...

    builder.AddProject<Projects.ClientApi>("clientapi")
           .AddReference(nova, "Authorization:Authority");

    builder.AddProject<Projects.ClientWeb>("clientweb")
           .AddReference(nova, "OpenIdConnectAuthentication:Authority");

Mit ``AddIdentityServerNova(containerName)`` wird eine Container mit dem
``identityserver-nova-dev`` image gestartet (https://hub.docker.com/r/gstalt/identityserver-nova-dev)

Dieses Image ist speziell für die Entwicklung erstellt worden. Da für viele Workflows 
bei der Anmeldung am *IdentityServer.Nova* eine HTTPS Verbindung notwendig ist,
wurde diese Image mit einem *selbst-signiertem Dev Zertifikat* für die SSL Verbindungen 
erstellt.

.. note:: 

    Da die Verbindung zum *IdentityServer.Nova* über eine *selbst-signiertes Zertifikat* 
    erfolgt, können im Browser Warnungen angezeigt. Das dieses Image nur für die 
    Entwicklung verwendet werden sollte, können diese Warnungen im Browser ignoriert werden.

Optional Methoden
-----------------

Auf den ``IdentityServerNovaResourceBuilder`` können optional noch weiter Methoden
angewendet werden:

* ``WithMailDev()``: Started zusätzlich einen MailDev Server der zum Testen des 
  Mailings verwendet werden kann, zB neu registrierter User muss seine E-Mail 
  verifizieren.

* ``WithBindMountPersistance()``: Damit Einstellungen in der Entwicklungsumgebung
  des *IdentityServer.Nova* gespeichert bleiben, kann mit dieser Methode ein Pfad
  für die Speicherung angeführt werden. Wird kein Parameter übergeben, erfolgt 
  die Speicherung der Daten im ``%USER%/identityserver-nova-aspire`` Verzeichnis.

* ``WithVolumePersistance()``: Ähnlich wie oben, nur das die Speicherung der 
  Daten in einem Docker Volume erfolgt. **Achtung:** hier kann es aufgrund 
  der Rechte des Container Users zu Zugriffsproblemen kommen.

* ``AsResourceBuilder()``: Wandelt den ``IdentityServerNovaResourceBuilder`` in einen 
  ``IResourceBuilder`` um, auf den alle anderen Aspire Resource Methoden angewendet 
  werden können.

.. note::

    Verwendet man Aspire nicht auf Windows mit *Docker Desktop* kann die Hostadresse des 
    Docker Host nicht automatisch bestimmt werden (in Windows steht dafür die Constante 
    ``host.docker.internal`` zur Verfügung). Damit *IdentityServer.Nova* mit *MailDev* 
    kommunizieren kann müssen beide Anwendungen im selbem **bridge** Network laufen.

    Dazu muss zuerst ein **bridge** Network erstellt werden:
    ``docker network create is-nova``.


    .. code:: csharp

       var nova = builder.AddIdentityServerNova(
                            containerName: "is-nova-dev", 
                            brigeNetwork: "is-nova"
                        ).WithMailDev();

Referenzen
----------

Eine *IdentityServer.Nova* Instanz kann mit ``.AddReference(nova, configName)`` an ein
Projekt gebunden werden. ``configName`` ist dabei der Name des Wertes aus der Konfiguration
des Projektes, in das der die (Aspire) Url von **IdentityServer.Nova** geschrieben werden soll.

