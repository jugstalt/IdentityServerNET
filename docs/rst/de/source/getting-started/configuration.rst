Konfiguration
=============

Die Konfiguration von **IdentityServerNET** erfolgt über JSON-Dateien im Verzeichnis ``_config``.
Der Name der Konfigurationsdatei lautet ``default.identityserver.net.json``.

.. note::

    Theoretisch kann der Präfix ``default`` im Namen auch geändert werden. Setzt man die 
    Umgebungsvariable ``IDENTITY_SERVER_SETTINGS_PREFIX``, wird dieser Wert als Präfix verwendet.
    
Aufbau der Config-Datei:

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
            "SigningCredential": {  // default: dateibasierte Speicherung unter <StorageRootPath>/storage/validation
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

Die gesamte Konfiguration erfolgt in der *Section* ``IdentityServer``. Darin befinden sich Werte und 
weitere *Sections*, auf die im Folgenden eingegangen wird.

Root-Werte
----------

* **AssemblyName:** Die Konfiguration der Services erfolgt in einer Assembly im Programmverzeichnis.
  In dieser Assembly muss eine Klasse mit dem Attribut ``[IdentityServerStartup]`` existieren, die vom 
  Interface ``IIdentityServerStartup`` abgeleitet wurde. Methoden dieser Klasse werden beim 
  Start der Applikation aufgerufen, um *Services* zu registrieren.

  Dadurch kann *IdentityServerNET* einfach an individuelle Bedürfnisse angepasst werden, ohne 
  den Source-Code der ursprünglichen Anwendung zu verändern. So können beispielsweise bestehende 
  User- und Rollendatenbanken eingebunden werden.

  Beispiele folgen später im Abschnitt **IdentityServerNET anpassen/erweitern**.

  Der Wert kann weggelassen werden. In diesem Fall wird die Standard-Assembly 
  ``IdentityServerNET.ServerExtension.Default`` verwendet.

* **ApplicationTitle:** Der Titel der Applikation, wie er in der Titelzeile angezeigt wird.

* **PublicOrigin:** Die URL des *IdentityServerNET*, wie sie im Browser angezeigt wird.
  Dieser Wert ist erforderlich, damit verschiedene Tools des *IdentityServerNET* funktionieren,
  z. B. **Secrets Vault**.

* **StorageRootPath:** Hier kann ein Pfad angegeben werden, in dem *IdentityServerNET* verschiedene Informationen speichern kann, beispielsweise die 
  Zertifikate zum Signieren von Tokens. Unterhalb dieses Ordners werden automatisch Unterordner entsprechend der Konfiguration erstellt, z. B. ``storage``, ``secretsvault``, usw.
  Wird dieser Pfad nicht angegeben, wird ein **Standardpfad** verwendet:

  - Windows: ``C:\\apps\\identityserver-net``
  - Linux/OSX: ``/home/app/identityserver-net``

Abschnitt ``ConnectionStrings``
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

Hier kann ein *ConnectionString* für eine *Datenbank* angegeben werden, in die User, Rollen, Ressourcen, Clients etc. gespeichert werden.

Standardmäßig können die Daten in einer ``LiteDb`` oder im Dateisystem abgelegt werden. Wird kein *ConnectionString* angegeben, werden 
die Daten **InMemory** gespeichert (bei einem Neustart der Applikation sind alle Daten verloren; dies sollte nur für Tests oder zur Entwicklung verwendet werden!).

Alternativ können die einzelnen Datenbanken auch in unterschiedliche Speicherorte abgelegt werden. Dafür 
muss für jede *Klasse* eine separate Datenbankverbindung angegeben werden:

.. code:: javascript

    "ConnectionStrings": {
        "Users": { "LiteDb": "is_net.db" },
        "Roles": { "LiteDb": "is_net.db" },
        "Clients": { "AzureStorage": "UseDevelopmentStorage=true" },
        "Resources": { "MongoDb": "mongodb://localhost:27017" },

        // Fallback (here not necessary) 
        "LiteDb": "is_net.db",
    }

Die einzelnen *Klassen* heißen ``Users``, ``Roles``, ``Clients`` und ``Resources``.
Für jede *Klasse* kann ein eigener ConnectionString definiert werden. Werden nicht alle *Klassen*
einzeln angegeben, kann ein Fallback angegeben werden.

.. note::

    Die beiden Klassen ``Clients`` und ``Resources`` können auch in **Azure Tables**
    oder einer **MongoDB** gespeichert werden.

Seit Version 7 stehen zusätzlich **relationale Datenbanken** als Backend zur Verfügung:

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

* **SqlServer:** Verbindungsstring für eine **Microsoft SQL Server**-Datenbank. Tabellen werden beim ersten Start automatisch erstellt.
* **Postgres:** Verbindungsstring für eine **PostgreSQL**-Datenbank. Tabellen werden beim ersten Start automatisch erstellt.
* **Sqlite:** Verbindungsstring für eine **SQLite**-Datenbankdatei. Die Datei und alle notwendigen Verzeichnisse werden automatisch angelegt.

Auch für SQL-Backends können einzelne *Klassen* auf unterschiedliche Backends aufgeteilt werden:

.. code:: javascript

    "ConnectionStrings": {
        "Users":     { "SqlServer": "Server=...;Database=identityserver;..." },
        "Roles":     { "SqlServer": "Server=...;Database=identityserver;..." },
        "Clients":   { "Postgres":  "Host=...;Database=identityserver;..." },
        "Resources": { "Sqlite":    "Data Source=identityserver.db" }
    }

.. note::

    Alle SQL-Backends speichern Objekte (Benutzer, Rollen, Clients, Ressourcen) als
    verschlüsseltes JSON in einer ``BlobData``-Spalte – dasselbe Prinzip wie bei LiteDb.
    Die Tabellen werden beim ersten Verbindungsaufbau automatisch erstellt, sofern sie noch
    nicht existieren. Bei **PostgreSQL** und **SQLite** werden Benutzernamen und E-Mail-Adressen
    intern immer in Kleinschreibung gespeichert und verglichen.

Abschnitt ``Crypto``
--------------------

.. code:: javascript

    "Crypto": {
        "Method": "key",  // key|data-protection|base64
        "Key": "..."      // protection key, if method=key
    },

Elemente, die vom Administrator erstellt werden (z. B. ``Clients``, ``Resources``, ...), sollten verschlüsselt gespeichert werden, da sie möglicherweise **Secrets** enthalten.

Die Verschlüsselungsmethode kann in diesem Abschnitt festgelegt werden. Folgende Methoden stehen zur Verfügung:

* **key:** Die Daten werden mit einem Schlüssel (Passwort) verschlüsselt. Der Schlüssel muss unter ``Key`` angegeben werden und mindestens 24 Zeichen lang sein.
  Diese Methode ist einfach zu verwenden, auch wenn **IdentityServerNET** auf mehrere Instanzen skaliert wurde. Alle Instanzen müssen dazu in 
  der Konfiguration den gleichen ``Key`` verwenden.
  
* **data-protection:** Zum Verschlüsseln wird die **Data Protection API** von .NET verwendet. Ist **IdentityServerNET** auf mehrere Instanzen skaliert,
  muss sichergestellt sein, dass alle Instanzen denselben Schlüsselkreis nutzen (siehe .NET Core Data Protection API).

* **base64:** Wenn keine der oben genannten Methoden angegeben wird, werden die Daten **nur in Base64** konvertiert. Diese *Verschlüsselung* ist ebenfalls einfach 
  umzusetzen, wenn **IdentityServerNET** auf mehrere Instanzen skaliert wird. Allerdings ist dies technisch gesehen keine *Verschlüsselung*, sondern eine *Codierung*. 
  Die Daten stehen dann nicht mehr im Klartext in der Datenbank.

Abschnitt ``SigningCredential``
-------------------------------

.. code:: javascript

    "SigningCredential": {
      "Storage": "c:\\apps\\identityserver-net\\storage\\validation",  // optional, default: <StorageRootPath>/storage/validation
      "CertPassword": "...",                                          // optional, default: zufälliges Passwort pro Installation
      "InMemoryOnly": true                                            // optional, default: false
    }

Zum Signieren von **Tokens** benötigt **IdentityServerNET** Zertifikate mit privaten und öffentlichen Schlüsseln.

Standardmäßig werden diese Zertifikate als passwortgeschützte Dateien unter ``<StorageRootPath>/storage/validation``
gespeichert (siehe ``StorageRootPath`` oben) — das ist der Standard, selbst wenn der Abschnitt ``SigningCredential``
komplett weggelassen wird. Zertifikate werden automatisch im Hintergrund erneuert, alte, nicht mehr aktive
Zertifikate werden nach einer Aufbewahrungsfrist automatisch gelöscht. Die vollständige Funktionsweise
(Rotation, aktives Fenster, Aufräumen) sowie die erweiterten Einstellungen ``CheckInterval``/
``RenewIfOlderThanDays``/``CacheDuration`` sind unter :doc:`../internals/signing-certificates`
beschrieben.

* **Storage:** Überschreibt den Speicherort für die Zertifikate. Optional — Standard ist
  ``<StorageRootPath>/storage/validation``.

* **CertPassword:** Das Passwort, mit dem die exportierten Zertifikatsdateien verschlüsselt werden. Optional —
  wenn nicht gesetzt, wird einmal pro Installation ein zufälliges Passwort erzeugt und (verschlüsselt über die
  .NET Data Protection API) zusammen mit den Zertifikaten gespeichert, statt sich auf ein festes, geteiltes
  Standardpasswort zu verlassen.

* **InMemoryOnly:** Wenn auf ``true`` gesetzt, werden die Zertifikate nur im Speicher gehalten statt auf der
  Festplatte persistiert. Bei jedem Neustart der Applikation gehen dann alle Zertifikate verloren — nur für
  Tests oder zur Entwicklung verwenden, niemals in Produktion.

Abschnitt ``Login``
-------------------

.. code:: javascript

    "Login": {
        "DenyForgotPasswordChallange": true,    // default: false
        "DenyRememberLogin": true,              // default: false,
        "RememberLoginDefaultValue": true,      // default: false
        "DenyLocalLogin": true,                 // default: false
        "Passkey": {
            "AllowPasswordless": true,          // default: false
            "AllowSecondFactor": true,          // default: false
            "ServerDomain": "identity.mein-server.com",
            "RelyingPartyName": "Meine App"     // default: "IdentityServer"
        }
    }

Hier kann das Verhalten und die Möglichkeiten beim Login gesteuert werden:

* **DenyForgotPasswordChallange:** Wenn auf ``true`` gesetzt, hat ein Anwender keine Möglichkeit, sein Passwort über ``Passwort vergessen`` zurückzusetzen.
* **DenyRememberLogin:** Wenn auf ``true`` gesetzt, wird die Option ``Remember my login`` beim Login nicht angeboten.
* **RememberLoginDefaultValue:** Wenn auf ``true`` gesetzt, ist die Option ``Remember my login`` standardmäßig ausgewählt.
* **DenyLocalLogin:** Wenn auf ``true`` gesetzt, können sich Anwender nicht mit Benutzername/Passwort anmelden.
  Dies kann sinnvoll sein, wenn die Anmeldung ausschließlich über *externe Identity Provider* erfolgen soll.

Unterabschnitt ``Passkey``
~~~~~~~~~~~~~~~~~~~~~~~~~~

Mit diesem Unterabschnitt wird die **Passkey**-Unterstützung (WebAuthn) konfiguriert. Passkeys ermöglichen eine
sichere Anmeldung über Hardware-Sicherheitsschlüssel, biometrische Sensoren (Fingerabdruck, Gesichtserkennung)
oder den Geräte-PIN – ohne herkömmliches Passwort.

* **AllowPasswordless:** Wenn auf ``true`` gesetzt, können sich Benutzer direkt mit einem Passkey anmelden –
  ohne Benutzername und Passwort. Auf der Login-Seite erscheint eine ``Sign in with passkey``-Schaltfläche.
  Benutzer können Passkeys unter *Manage Account → Passkeys* registrieren.

* **AllowSecondFactor:** Wenn auf ``true`` gesetzt, wird nach erfolgreicher Passworteingabe eine zusätzliche
  Passkey-Verifizierung verlangt – sofern der Benutzer mindestens einen Passkey registriert hat. Dies bietet
  starke Zwei-Faktor-Authentifizierung (2FA) ohne separaten Authenticator.

* **ServerDomain:** Die Domain des *Relying Party*, also der Hostname, unter dem **IdentityServerNET**
  erreichbar ist (z. B. ``identity.mein-server.com``). Dieser Wert muss dem Hostnamen der ``PublicOrigin``-URL
  entsprechen – Browser speichern Passkeys domaingebunden und verweigern die Verwendung auf anderen Domains.

* **RelyingPartyName:** Der Anzeigename, der beim Registrieren eines Passkeys im Browser-Dialog erscheint.
  Standardwert: ``IdentityServer``.

.. note::

    Passkeys sind an die Domain gebunden. Ein Passkey, der für ``identity.mein-server.com`` registriert wurde,
    kann nicht auf einer anderen Domain verwendet werden. ``ServerDomain`` muss daher der tatsächlichen
    öffentlichen Domain des Servers entsprechen.

Abschnitt ``Admin``
-------------------

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

Hier kann bestimmt werden, welche *Admin Tools* in der **IdentityServerNET**-Instanz zur Verfügung stehen:

* **DenyAdminUsers:** Benutzerkonten können nicht von Administratoren erstellt und bearbeitet werden.
* **DenyAdminRoles:** Benutzerrollen können nicht von Administratoren erstellt und bearbeitet werden.
* **DenyAdminResources:** Identity- und API-Ressourcen können nicht von Administratoren erstellt und bearbeitet werden.
* **DenyAdminClients:** Clients können nicht von Administratoren erstellt und bearbeitet werden.
* **DenyAdminSecretsVault:** Das **Secrets Vault** steht dem Administrator nicht zur Verfügung.
* **DenySigningUI:** Das **Payload Signing**-Werkzeug steht dem Administrator nicht zur Verfügung.
* **DenyAdminCreateCerts:** Das **Selbst-Signierte Zertifikate**-Werkzeug steht dem Administrator nicht zur Verfügung.

* **DenyAdminCreateCerts:** Das **Selbst-Signierte Zertifikate**-Werkzeug steht dem Administrator nicht zur Verfügung.

* **AllowDataTransfer:** Wenn auf ``true`` gesetzt, erscheint im Admin-Bereich die Kachel **Data Transfer**.
  Administratoren können damit alle Benutzer, Rollen, Clients und Ressourcen als JSON-Datei exportieren
  und in eine andere Instanz importieren. Bestehende Einträge werden beim Import übersprungen, nie überschrieben.
  Diese Option sollte nur während der Installations-/Migrationsphase aktiviert sein und danach wieder deaktiviert werden.

  .. note::

      Passwort-Hashes werden mit exportiert – sie sind kompatibel, solange Quell- und Zielinstanz denselben
      ASP.NET Identity-Hashing-Algorithmus verwenden. Passkeys sind im Export enthalten, funktionieren jedoch
      auf einer anderen Domain nicht (WebAuthn ist domain-gebunden).

Mit diesem Abschnitt können die Administrationswerkzeuge eingeschränkt werden. Dies kann sinnvoll sein, wenn eine **IdentityServer**-Instanz öffentlich
zugänglich ist. Wenn eine öffentliche Instanz keine Administrationswerkzeuge besitzt, erhöht dies die Sicherheit der **IdentityServer-Datenbanken**.
Die Administration kann hier beispielsweise nur über eine Instanz erfolgen, die nicht über das Internet erreichbar ist (nur Intranet, ...) und auf die gleiche
Datenbank zugreift wie die öffentliche Instanz.

Abschnitt ``Security``
----------------------

.. code:: javascript

    "Security": {
        "PasswordHashing": {
            "Template": "{password}{username}"   // default: "{password}"
        }
    }

Über diesen optionalen Abschnitt kann das Eingabe-Format für den Passwort-Hasher konfiguriert werden.
Standardmäßig wird nur das Passwort gehasht. Für Migrationen von Legacy-Systemen, die dem Passwort
zusätzliche Benutzerdaten als Salz beigefügt haben, kann das Template entsprechend angepasst werden.

Unterstützte Platzhalter (werden immer durch Kleinschreibung ersetzt):

* ``{password}`` — das eingegebene Klartext-Passwort (immer erforderlich)
* ``{email}`` — die E-Mail-Adresse des Benutzers
* ``{username}`` — der Benutzername

Beispiel für ein Legacy-System, das ``password + username`` zusammengehasht hat::

    "Template": "{password}{username}"

.. note::

    Dieses Template betrifft sowohl das **Erstellen neuer Hashes** als auch die **Verifikation** bestehender.
    Nach einer erfolgreichen Migration kann das Template wieder auf ``"{password}"`` (Standard) zurückgesetzt
    werden — bereits auf PBKDF2 aktualisierte Hashes bleiben gültig, da der ``SecurePasswordHasher`` auch
    bei aktiviertem Rehashing korrekt verfährt.

Abschnitt ``Account``
---------------------

.. code:: javascript

   "Account": {
        "DenyManageAccount": true,   // default: false
        "DenyRegisterAccount": true, // default: false
   }

Hier können Einschränkungen im Bezug auf *User Accounts* festgelegt werden:

* **DenyManageAccount:** Ein angemeldeter Benutzer kann keine eigenständigen Änderungen an seinem Account vornehmen. Dies kann sinnvoll sein, wenn nur Administratoren 
  die Benutzerkonten verwalten sollen oder wenn die Account-Verwaltung bereits über eine andere Anwendung erfolgt.

* **DenyRegisterAccount:** Benutzer können sich beim IdentityServer nicht selbst registrieren.

Abschnitt ``Cookie``
--------------------

.. code:: javascript 

    "Cookie": {
        "Name": "identityserver-net-identity",
        "Domain": "identity.my-server.com",
        "Path": "/",
        "ExpireDays": 365
    }

Der **IdentityServerNET** erzeugt für einen angemeldeten Benutzer ein *Cookie*. Hier kann genauer bestimmt werden, wie dieses *Cookie* aufgebaut ist:

* **Name:** Der Name des *Cookies*
* **Domain:** Gibt an, für welche *Domain* das *Cookie* gültig ist
* **Path:** Der Pfad, für den das *Cookie* gültig ist
* **ExpireDays:** Gibt an, wie lange das *Cookie* gültig ist

Über **Domain** und **Path** kann eingeschränkt werden, wann ein *Cookie* vom Browser zum Server geschickt wird. Grundsätzlich sollte dieses *Cookie* nur 
an den **IdentityServerNET** gesendet werden!

Abschnitt ``Mail``
------------------

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

Bei ``Forget Password`` und ``Register new user`` werden E-Mails an den Benutzer gesendet. In diesem Abschnitt
kann festgelegt werden, wie diese E-Mails verschickt werden. Standardmäßig stehen ``Smtp``, ``MailJet`` und
``SendGrid`` zur Verfügung. Wird keine Option angegeben, wird die E-Mail nicht verschickt, sondern ins
*Logging* ausgegeben – diese Möglichkeit sollte nur für die Entwicklung verwendet werden.

E-Mail-Templates
~~~~~~~~~~~~~~~~

**IdentityServerNET** verwendet anpassbare HTML-Templates für den E-Mail-Versand. Beim Start werden Templates
aus dem Verzeichnis ``TemplatesPath`` geladen (Standard: ``custom/mails`` relativ zum Programmverzeichnis).
Wird für ein Template keine Datei gefunden, greift ein eingebautes Standard-Template.

Folgende Template-Dateien werden unterstützt:

* ``confirm-email.html`` – Bestätigungsmail nach der Registrierung eines neuen Benutzers
* ``reset-password.html`` – Mail zum Zurücksetzen des Passworts
* ``generic.html`` – Standardtemplate für alle anderen E-Mails

In den Templates können folgende Platzhalter verwendet werden:

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Platzhalter
     - Bedeutung
   * - ``{{applicationName}}``
     - Name der Anwendung (aus ``ApplicationTitle``)
   * - ``{{subject}}``
     - Betreff der E-Mail
   * - ``{{email}}``
     - E-Mail-Adresse des Empfängers
   * - ``{{link}}``
     - Aktions-URL (z. B. Bestätigungs- oder Reset-Link)
   * - ``{{content}}``
     - Originaler HTML-Nachrichteninhalt (nur im ``generic``-Template sinnvoll)
   * - ``{{year}}``
     - Aktuelles Jahr (für Copyright-Zeilen im Footer)

.. note::

    Ein absoluter Pfad bei ``TemplatesPath`` wird direkt verwendet. Ein relativer Pfad wird relativ zum
    Programmverzeichnis aufgelöst. Wird der Ordner oder eine Datei nicht gefunden, wird ohne Fehler auf das
    eingebaute Template zurückgegriffen.

Abschnitt ``Stores``
--------------------

Dieser optionale Abschnitt steuert serverseitige Stores, die während des Autorisierungsflows verwendet werden.

.. code:: javascript

    "Stores": {
        // Autorisierungsparameter-Store (Login ReturnUrl)
        "ParameterMessageStore": "DistributedMemoryCache",
        // oder
        "ParameterMessageStore": "DistributedRedisCache",
        "ParameterMessageStoreConnectionString": "localhost:6379",

        // PAR request_uri Store
        "PushedAuthorizationStore": "DistributedMemoryCache",
        // oder
        "PushedAuthorizationStore": "DistributedRedisCache",
        "PushedAuthorizationStoreConnectionString": "localhost:6379"
    }

Wird kein Store konfiguriert, greift jeweils der eingebaute Fallback:

.. list-table::
   :widths: 30 30 40
   :header-rows: 1

   * - Einstellung
     - Standard (keine Konfiguration)
     - Hinweis
   * - ``ParameterMessageStore``
     - Parameter in ``ReturnUrl``
     - Lange Login-URL, aber keine Secrets exponiert
   * - ``PushedAuthorizationStore``
     - In-Process ``ConcurrentDictionary``
     - Lazy Expiry, nur Einzelinstanz

**Hintergrund — Pushed Authorization Requests (PAR)**

**IdentityServerNET** unterstützt `Pushed Authorization Requests (PAR) <https://www.rfc-editor.org/rfc/rfc9126>`_
(RFC 9126). Bei PAR schickt ein OIDC-Client zuerst alle Autorisierungsparameter per POST an den
``/connect/par``-Endpoint (Server-zu-Server, Backchannel). Der Server liefert eine kurzlebige
``request_uri`` zurück. Der Browser folgt anschließend nur einem Redirect mit ``client_id`` und
``request_uri`` – sensible Parameter (``client_secret``, ``scope``, ``code_challenge`` usw.) sind
damit weder in der Browser-URL noch in Server-Access-Logs sichtbar.

Die ASP.NET Core OIDC-Middleware (``Microsoft.AspNetCore.Authentication.OpenIdConnect``, ab .NET 9)
nutzt PAR automatisch, sobald der Server es im Discovery-Dokument ankündigt. Das Verhalten wird über
``PushedAuthorizationBehavior`` gesteuert:

.. code:: csharp

    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.UseIfAvailable; // Standard

**ParameterMessageStore — Autorisierungsparameter serverseitig halten**

Beim interaktiven Login-Flow muss IdentityServer die Autorisierungsparameter über den
Login-Seiten-Redirect hinweg mitführen. Standardmäßig werden sie in den ``ReturnUrl``-Querystring
kodiert, was die Login-URL lang aber nicht unsicher macht. Um die URL kurz zu halten und die Parameter
vollständig serverseitig zu speichern, kann ein ``ParameterMessageStore`` konfiguriert werden:

* **DistributedMemoryCache** — Parameter werden im In-Process-Memory-Cache gespeichert. Einfach,
  ohne zusätzliche Infrastruktur – aber **nicht geeignet für Multi-Instanz-Deployments** (jede
  Instanz hat ihren eigenen Speicher).

* **DistributedRedisCache** — Parameter werden in einem Redis-Cache gespeichert. Für Produktion und
  Multi-Instanz-Deployments geeignet. Erfordert ``ParameterMessageStoreConnectionString``.

.. code:: javascript

    // In-Process-Memory (Einzelinstanz / Entwicklung)
    "Stores": {
        "ParameterMessageStore": "DistributedMemoryCache"
    }

    // Redis (Produktion, Multi-Instanz)
    "Stores": {
        "ParameterMessageStore": "DistributedRedisCache",
        "ParameterMessageStoreConnectionString": "redis-host:6379"
    }

Mit einem konfigurierten ``ParameterMessageStore`` ändert sich die Login-URL von:

.. code::

    /Account/Login?ReturnUrl=/connect/authorize/callback?client_id=...&scope=...&code_challenge=...

zu:

.. code::

    /Account/Login?ReturnUrl=/connect/authorize/callback?authzId=<kurze-opake-ID>

**PushedAuthorizationStore — Verteilter Store für PAR request_uri**

Die PAR-``request_uri`` wird serverseitig mit einer TTL von 60 Sekunden gespeichert. Standardmäßig
wird ein In-Process-``ConcurrentDictionary`` verwendet. Abgelaufene Einträge werden nur lazy
(beim nächsten Zugriff) entfernt, was bei hoher Last zu unbegrenztem Speicherwachstum führen kann.
Außerdem ist der Store nicht instanzübergreifend geteilt. Ein verteilter Store behebt beides:

* **DistributedMemoryCache** — In-Process-Memory mit automatischer TTL-Ablaufsteuerung. Geeignet
  für Einzelinstanz-Deployments.
* **DistributedRedisCache** — Redis-basiert, von allen Instanzen geteilt. Erforderlich für
  Multi-Instanz-Deployments.

.. code:: javascript

    // Einzelinstanz / Entwicklung
    "Stores": {
        "PushedAuthorizationStore": "DistributedMemoryCache"
    }

    // Produktion, Multi-Instanz
    "Stores": {
        "PushedAuthorizationStore": "DistributedRedisCache",
        "PushedAuthorizationStoreConnectionString": "redis-host:6379"
    }

.. note::

    **Aspire:** Wenn das Präprozessorsymbol ``#define USE_REDIS`` in
    ``IdentityServerNET.AppHost/Program.cs`` aktiv ist, wird automatisch ein Redis-Container
    gestartet und **beide** Stores – ``ParameterMessageStore`` und ``PushedAuthorizationStore`` –
    werden über Umgebungsvariablen konfiguriert. Ein manueller Connection String ist nicht
    erforderlich.

Abschnitt ``Endpoints``
-----------------------

Dieser optionale Abschnitt steuert, welche IdentityServer-Protokoll-Endpoints aktiv sind.

.. code:: javascript

    "Endpoints": {
        "EnablePushedAuthorization": "false"   // Standard: true
    }

* **EnablePushedAuthorization:** Wird auf ``false`` gesetzt, wird der ``/connect/par``-Endpoint
  deaktiviert und ``pushed_authorization_request_endpoint`` verschwindet aus dem Discovery-Dokument.
  Die ASP.NET Core OIDC-Middleware fällt dann automatisch auf den normalen Authorization Code Flow
  ohne PAR zurück. Nützlich zum Testen des reinen PKCE-Flows oder in Deployments, bei denen PAR
  nicht gewünscht ist.

**RequirePushedAuthorization bei Clients**

Einzelne Clients können so konfiguriert werden, dass PAR zwingend erforderlich ist. Im Admin-UI
unter *Clients → Options* wird dazu ``RequirePushedAuthorization = true`` gesetzt. Ein direkter
Aufruf von ``/connect/authorize`` ohne vorherigen PAR-Request wird dann mit ``invalid_request``
abgewiesen.

Abschnitt ``RateLimiting``
---------------------------

.. code:: javascript

    "RateLimiting": {
        "TokenEndpoint": {
            "PermitLimit": 30,      // optional, Standard: 30
            "WindowSeconds": 60     // optional, Standard: 60
        }
    }

Die interaktive Login-Seite hat eine eigene Bot-Erkennung/CAPTCHA, aber der OAuth-Token-Endpoint
(``/connect/token`` — Password Grant, Client Credentials, ...) ist eine eigene Angriffsfläche, die
davon komplett unberührt bleibt. Anfragen an ``/connect/token`` werden pro Client-IP-Adresse über ein
gleitendes Zeitfenster begrenzt; alle anderen Endpoints sind davon nicht betroffen.

* **PermitLimit:** Maximale Anzahl an Anfragen an ``/connect/token`` pro IP-Adresse innerhalb von
  ``WindowSeconds``. Weitere Anfragen erhalten ``429 Too Many Requests``.
* **WindowSeconds:** Länge des gleitenden Zeitfensters in Sekunden.

Abschnitt ``Configure``
-----------------------

Hier kann das Verhalten der **IdentityServerNET**-Anwendung über *Middlewares* gesteuert werden.

.. code:: javascript

    "Configure": {
        "UseHttpsRedirection": "false",         // default: true
        "AddXForwardedProtoMiddleware": "true"  // default: false
    }

* **UseHttpsRedirection:** Der IdentityServer leitet automatisch auf HTTPS-Verbindungen um. Läuft die Anwendung in einem *Kubernetes* Cluster, ist das nicht immer 
  wünschenswert. Innerhalb des Clusters läuft die Anwendung oft über das HTTP-Protokoll, ist jedoch über den *Ingress* nur über HTTPS erreichbar.

* **AddXForwardedProtoMiddleware:** Für **IdentityServerNET** ist ein Aufruf über HTTPS erforderlich! Wenn die automatische Umleitung mit **UseHttpsRedirection** deaktiviert wird,
  funktioniert der **IdentityServer** möglicherweise nicht wie erwartet. Die **XForwardedProtoMiddleware** sorgt dafür, dass der ``X-Forwarded-Proto`` Header berücksichtigt wird. 
  Wenn der **IdentityServer** in einem *Kubernetes* Cluster über den *Ingress* mit HTTPS aufgerufen wird, bleibt die Funktionalität des Servers auch dann bestehen, 
  wenn die Kommunikation innerhalb des Clusters über HTTP erfolgt.
  



