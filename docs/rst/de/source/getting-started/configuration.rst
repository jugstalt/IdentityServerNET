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
      "Storage": "c:\\apps\\identityserver-net\\storage\\validation",  // any path
      "CertPassword": "..."
    }

Zum Signieren von **Tokens** benötigt **IdentityServerNET** Zertifikate mit privaten und öffentlichen Schlüsseln. Hier kann der Speicherort für diese 
Zertifikate angegeben werden. Zusätzlich kann ein Passwort festgelegt werden, mit dem die Zertifikate verschlüsselt werden. Der private Schlüssel kann 
dann nur von Anwendungen ausgelesen werden, die dieses Passwort kennen.

Wird dieser Abschnitt nicht angegeben, werden die Zertifikate nur **InMemory** gespeichert.
(Bei einem Neustart der Applikation sind alle Zertifikate verloren; dies sollte nur für Tests oder zur Entwicklung verwendet werden!).

Abschnitt ``Login``
-------------------

.. code:: javascript

    "Login": {
        "DenyForgotPasswordChallange": true,    // default: false
        "DenyRememberLogin": true,              // default: false,
        "RememberLoginDefaultValue": true,      // default: false
        "DenyLocalLogin": true                  // default: false  
    }

Hier kann das Verhalten und die Möglichkeiten beim Login gesteuert werden:

* **DenyForgotPasswordChallange:** Wenn auf ``true`` gesetzt, hat ein Anwender keine Möglichkeit, sein Passwort über ``Passwort vergessen`` zurückzusetzen.
* **DenyRememberLogin:** Wenn auf ``true`` gesetzt, wird die Option ``Remember my login`` beim Login nicht angeboten.
* **RememberLoginDefaultValue:** Wenn auf ``true`` gesetzt, ist die Option ``Remember my login`` standardmäßig ausgewählt.
* **DenyLocalLogin:** Wenn auf ``true`` gesetzt, können sich Anwender nicht mit Benutzername/Passwort anmelden. 
  Dies kann sinnvoll sein, wenn die Anmeldung ausschließlich über *externe Identity Provider* erfolgen soll.

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
        "DenyAdminCreateCerts": true        // default: false
    }

Hier kann bestimmt werden, welche *Admin Tools* in der **IdentityServerNET**-Instanz zur Verfügung stehen:

* **DenyAdminUsers:** Benutzerkonten können nicht von Administratoren erstellt und bearbeitet werden.
* **DenyAdminRoles:** Benutzerrollen können nicht von Administratoren erstellt und bearbeitet werden.
* **DenyAdminResources:** Identity- und API-Ressourcen können nicht von Administratoren erstellt und bearbeitet werden.
* **DenyAdminClients:** Clients können nicht von Administratoren erstellt und bearbeitet werden.
* **DenyAdminSecretsVault:** Das **Secrets Vault** steht dem Administrator nicht zur Verfügung.
* **DenySigningUI:** Das **Payload Signing**-Werkzeug steht dem Administrator nicht zur Verfügung.
* **DenyAdminCreateCerts:** Das **Selbst-Signierte Zertifikate**-Werkzeug steht dem Administrator nicht zur Verfügung.

Mit diesem Abschnitt können die Administrationswerkzeuge eingeschränkt werden. Dies kann sinnvoll sein, wenn eine **IdentityServer**-Instanz öffentlich 
zugänglich ist. Wenn eine öffentliche Instanz keine Administrationswerkzeuge besitzt, erhöht dies die Sicherheit der **IdentityServer-Datenbanken**.
Die Administration kann hier beispielsweise nur über eine Instanz erfolgen, die nicht über das Internet erreichbar ist (nur Intranet, ...) und auf die gleiche 
Datenbank zugreift wie die öffentliche Instanz.

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

Bei ``Forget Password`` und ``Register new user`` werden E-Mails an den Benutzer gesendet. In diesem Abschnitt kann festgelegt werden, wie diese E-Mails verschickt werden.
Standardmäßig stehen ``Smtp``, ``MailJet`` und ``SendGrid`` zur Verfügung. Wird keine Option angegeben, wird die E-Mail nicht verschickt, sondern ins *Logging* ausgegeben.
Diese Möglichkeit sollte nur für die Entwicklung verwendet werden.

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
  



