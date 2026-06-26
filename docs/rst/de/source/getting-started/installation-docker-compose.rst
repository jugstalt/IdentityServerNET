Installation mit Docker Compose
================================

Mit **Docker Compose** lässt sich *IdentityServerNET* am einfachsten in einer eigenen
Umgebung starten. Alle Einstellungen werden in einer zentralen ``.env``-Datei verwaltet
und können von dort direkt in andere Projekte übernommen werden.

Die notwendigen Dateien befinden sich im Repository unter
`dist/docker-compose/ <https://github.com/jugstalt/identityserver.net/tree/master/dist/docker-compose>`_.

Schnellstart
------------

.. code:: bash

    git clone https://github.com/jugstalt/identityserver.net.git
    cd identityserver.net/dist/docker-compose
    docker compose up -d

Die Anwendung ist danach unter http://localhost:8080 erreichbar.

Dateien
-------

``docker-compose.yml``
~~~~~~~~~~~~~~~~~~~~~~

.. code:: yaml

    services:
      identityserver:
        image: gstalt/identityserver-net:latest
        restart: unless-stopped
        ports:
          - "${HOST_PORT:-8080}:8080"
        env_file:
          - .env
        environment:
          ASPNETCORE_URLS: "http://*:8080"
        volumes:
          - identityserver-data:/home/app

    volumes:
      identityserver-data:

Das Volume wird bei ``/home/app`` eingehängt (nicht bei ``/home/app/identityserver-net``),
damit Docker die Dateirechte aus dem Image (Benutzer ``app``) korrekt übernimmt.

``.env``
~~~~~~~~

Alle Einstellungen stehen in der ``.env``-Datei. Die Variablennamen verwenden ``__``
als Trennzeichen für hierarchische ASP.NET Core-Konfigurationswerte.

Wichtige Grundeinstellungen
---------------------------

.. list-table::
   :widths: 45 55
   :header-rows: 1

   * - Variable
     - Beschreibung
   * - ``HOST_PORT``
     - Externer Port auf dem Host (Standard: ``8080``)
   * - ``IDENTITYSERVER__PUBLICORIGIN``
     - Öffentliche URL des Servers, z. B. ``https://auth.example.com``.
       Muss mit der URL übereinstimmen, unter der Clients den Server erreichen.
   * - ``IDENTITYSERVER__APPLICATIONTITLE``
     - Titel in der Titelleiste und auf der Anmeldeseite
   * - ``IDENTITYSERVER__SIGNINGCREDENTIAL__CERTPASSWORD``
     - Passwort für das automatisch erzeugte Signatur-Zertifikat. Unbedingt ändern!

Datenbank / Speicher
--------------------

Standardmäßig wird **LiteDB** verwendet (dateibasiert, kein zusätzlicher Dienst nötig).
Alternativ stehen SQL Server, PostgreSQL, SQLite und MongoDB zur Verfügung:

.. list-table::
   :widths: 45 55
   :header-rows: 1

   * - Variable
     - Beispielwert
   * - ``IDENTITYSERVER__CONNECTIONSTRINGS__LITEDB``
     - ``/home/app/identityserver-net/is_identityserver-net.db``
   * - ``IDENTITYSERVER__CONNECTIONSTRINGS__SQLSERVER``
     - ``Server=db;Database=IdentityServerNET;User Id=sa;Password=...``
   * - ``IDENTITYSERVER__CONNECTIONSTRINGS__POSTGRES``
     - ``Host=db;Database=identityservernet;Username=postgres;Password=...``
   * - ``IDENTITYSERVER__CONNECTIONSTRINGS__SQLITE``
     - ``Data Source=/home/app/identityserver-net/is.sqlite``
   * - ``IDENTITYSERVER__CONNECTIONSTRINGS__MONGODB``
     - ``mongodb://mongo:27017``

Es darf immer nur **eine** Connection String gesetzt sein.

Passkey (WebAuthn / FIDO2)
--------------------------

Passkeys ermöglichen die passwortlose Anmeldung per Biometrie oder Hardware-Schlüssel.

.. list-table::
   :widths: 55 45
   :header-rows: 1

   * - Variable
     - Beschreibung
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__ALLOWPASSWORDLESS``
     - ``true`` – passwortlose Erstfaktor-Anmeldung aktivieren
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__ALLOWSECONDFACTOR``
     - ``true`` – Passkey als zweiten Faktor nach Benutzername/Passwort aktivieren
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__SERVERDOMAIN``
     - WebAuthn Relying Party ID, z. B. ``example.com``. Leer lassen = vom Request-Host ableiten.
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__RELYINGPARTYNAME``
     - Anzeigename im Authenticator während der Registrierung

Migrationen (Seeding)
---------------------

.. note::

    Migrationen werden nur ausgeführt, wenn ``ASPNETCORE_ENVIRONMENT=Development`` gesetzt ist.
    Sie legen fehlende Einträge beim ersten Start an und überschreiben niemals vorhandene Daten.
    Nach dem ersten erfolgreichen Start kann ``ASPNETCORE_ENVIRONMENT`` entfernt werden – oder
    dauerhaft gesetzt bleiben, da nachfolgende Starts nichts Bestehendes verändern.

In der ``.env``-Datei können Migrationen über Umgebungsvariablen definiert werden:

.. code:: bash

    ASPNETCORE_ENVIRONMENT=Development
    IDENTITYSERVER__MIGRATIONS__ADMINPASSWORD=Admin1234!

    # Identity Resources
    IDENTITYSERVER__MIGRATIONS__IDENTITYRESOURCES__0__NAME=openid
    IDENTITYSERVER__MIGRATIONS__IDENTITYRESOURCES__1__NAME=profile
    IDENTITYSERVER__MIGRATIONS__IDENTITYRESOURCES__2__NAME=role

    # API Resource mit Scopes
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__NAME=my-api
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__APISECRET=apisecret
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__SCOPES__0__NAME=read
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__SCOPES__1__NAME=write

    # Rollen
    IDENTITYSERVER__MIGRATIONS__ROLES__0__NAME=admin
    IDENTITYSERVER__MIGRATIONS__ROLES__1__NAME=user

    # Benutzer
    IDENTITYSERVER__MIGRATIONS__USERS__0__NAME=alice@example.com
    IDENTITYSERVER__MIGRATIONS__USERS__0__PASSWORD=Alice1234!
    IDENTITYSERVER__MIGRATIONS__USERS__0__ROLES__0=admin

    # Web Application Client (Authorization Code + PKCE)
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTTYPE=WebApplication
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTID=my-webapp
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTSECRET=secret
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTURL=https://myapp.example.com
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__SCOPES__0=openid
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__SCOPES__1=profile
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__SCOPES__2=role

    # API Client (Client Credentials)
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__CLIENTTYPE=ApiClient
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__CLIENTID=my-service
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__CLIENTSECRET=secret
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__SCOPES__0=my-api
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__SCOPES__1=my-api.read

    # JavaScript / SPA Client (kein Secret)
    IDENTITYSERVER__MIGRATIONS__CLIENTS__2__CLIENTTYPE=JavascriptClient
    IDENTITYSERVER__MIGRATIONS__CLIENTS__2__CLIENTID=my-spa
    IDENTITYSERVER__MIGRATIONS__CLIENTS__2__CLIENTURL=https://spa.example.com
    IDENTITYSERVER__MIGRATIONS__CLIENTS__2__SCOPES__0=openid
    IDENTITYSERVER__MIGRATIONS__CLIENTS__2__SCOPES__1=profile

Hinter einem Reverse Proxy
--------------------------

Wenn *IdentityServerNET* hinter nginx, Traefik oder einem anderen TLS-Terminator läuft,
müssen folgende Einstellungen gesetzt werden:

.. code:: bash

    IDENTITYSERVER__PUBLICORIGIN=https://auth.example.com
    IDENTITYSERVER__CONFIGURE__USEHTTPSREDIRECTION=false
    IDENTITYSERVER__CONFIGURE__ADDXFORWARDEDPROTOMIDDLEWARE=true
