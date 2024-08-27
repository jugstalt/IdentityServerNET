Konfiguration
=============

Die Konfiguration von **IdentityServer.Nova** erfolgt über JSON Dateien in Verzeichnis ``_config``.
Der Name der Konfigurationsdaten ist ``default.identityserver.nova.json``. 

.. note::

    Theoretisch kann der Prefix ``default`` im Namen auch geändert werden. Setzt man die 
    Umgebungsvariable ``IDENTITY_SERVER_SETTINGS_PREFIX`` wird als Präfix dieser Wert verwendet.
    
Aufbau der Config Datei:

.. code:: javascript

    {
        "IdentityServer": {  
            "NovaAssemblyName": "...",  // default: IdentityServer.Nova.ServerExtension.Default
            "ApplicationTitle": "...", // default "IdentityServer.Nova",
            "PublicOrigin": "https://localhost:44300",
            "ConnectionStrings": {  // default: null => all DBs in Memory
                // ...
            },
            "SigningCredential": {  // default: null => certs only in memory
                // ..
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

Die gesamte Konfiguration erfolgt in der *Section* ``IdentityServer``. Darum befinden sich Werte und 
weiter *Sections* auf die in der Folge eingegangen wird.

Root-Werte
----------

* **NovaAssemblyName:** Die Konfiguration der Services erfolgt ein einer Assembly im Programmverzeichnis.
  In diese Assembly muss eine Klasse mit dem Attribut ``[IdentityServerNovaStartup]`` das vom 
  Interface ``IIdentityServerNovaStartup`` abgeleitet wurde. Methoden dieser Klasse werden beim 
  Start der Applikation aufgerufen, um *Services* zu registrieren.

  Damit kann der *IdentityServer.Nova* einfach den eigenen Bedürfnissen angepasst werden, ohne 
  den Source Code der ursprünglichen Anwendung zu verändern. So können beispielsweise bestehende 
  User/Rollen Datenbanken eingebunden werden.

  Beispiele folgen später im Abschnitt **IdentityServer.Nova anpassen/erweitern**

  Der Wert kann weggelassen werden. In dem Fall wird die Default Assembly 
  ``IdentityServer.Nova.ServerExtension.Default`` verwendet.

* **ApplicationTitle:** Der Title der Applikation, wie er in der Titelzeile angezeigt wird.

* **PublicOrigin:** Die Url des *IdentityServer.Nova*, wie er im Browser angezeigt wird.
  Dieser Wert ist erforderlich, damit diverse Tools des *IdentityServer.Nova* funktionieren,
  zB. **Secrets Vault**

Abschnitt ``ConnectionStrings``
-------------------------------

.. code:: javascript

    "ConnectionStrings": {
        "LiteDb": "c:\\apps\\identityserver-nova\\is_nova.db"
        // or
        ...
        "FilesDb": "c:\\apps\\identityserver-nova\\storage"  // any path
    }

Hier kann ein *ConnectionString* für eine *Datenbank* angegeben werden, in die User, Rollen, Resourcen, Clients etc gespeichert werden.

Standardmäßig können die Daten ein einer ``LiteDb`` oder im FileSystem abgelegt werden. Wird kein *ConnectionString* angegeben, werden 
die Daten **InMemory** gespeichert (Bei einem Neustart der Applikation sind alle Daten weg, sollte nur zur Test oder zum Entwickeln verwendet werden!)

Abschnitt ``SigningCredential``
-------------------------------

.. code:: javascript

    "SigningCredential": {
      "Storage": "c:\\apps\\identityserver-nova\\storage\\validation",  // any path
      "CertPassword": "..."
    }

Zum Signieren von **Tokens** benötigt der **IdentityServer.Nova** Zertifikate mit privaten und öffentlichen Schlüsseln. Hier kann der Speicherort für diese 
Zertifikate angegeben werden. Zusätzlich kann noch ein Passwort angegeben werden, mir denen die Zertifikate verschlüsselt werden. Der private Schlüssel kann 
dann nur von Anwendungen ausgelesen werden, die diesen Passwort kennen.

Wird dieser Abschnitt nicht angegeben, werden die Zertifikate nur **InMemory** gespeichert 
(Bei einem Neustart der Applikation sind alle Zertifikate weg, sollte nur zur Test oder zum Entwickeln verwendet werden!).

Abschnitt ``Login``
-------------------

.. code:: javascript

    "Login": {
        "DenyForgotPasswordChallange": true,    // default: false
        "DenyRememberLogin": true,              // default: false,
        "RememberLoginDefaultValue": true       // default: false
    }

Hier kann das Verhalten und die Möglichkeiten beim Login gesteuert werden:

* **DenyForgotPasswordChallange:** wenn ``true`` hat ein Anwender keine Möglichkeit, sein Passwort über ``Passwort vergessen`` zu ändern.
* **DenyRememberLogin:** wenn ``true`` wird die Option ``Remember my login`` beim Login nicht angeboten
* **RememberLoginDefaultValue:** wenn ``true`` ist die Option ``Remember my login`` automatisch ausgewählt

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

Hier kann bestimmt werden, welche *Admin Tools* in der **IdentityServer.Nova** Instanz zur Verfügung stehen:

* **DenyAdminUsers:** User Accounts können von Administrator erstellt und bearbeitet werden.
* **DenyAdminRoles:** User Rollen können von Administrator erstellt und bearbeitet werden.
* **DenyAdminResources:** Identity und API Resourcen können von Administrator erstellt und bearbeitet werden.
* **DenyAdminClients:** Clients können von Administrator erstellt und bearbeitet werden.
* **DenyAdminSecretsVault:** Das **Secrets Vault** steht dem Administrator zur Verfügung.
* **DenySigningUI:** Das **Payload Signing** Werkzeug steht dem Administrator zur Verfügung.
* **DenyAdminCreateCerts:** Das **Selbst-Signierte Zertifikate** Werkzeug steht dem Administrator zur Verfügung.

Mit diesem Abschnitt können die Administrationswerkzeuge eingeschränkt werden. Das kann Sinn machen, wenn eine **IdentityServer** Instanz öffentlich 
zugänglich ist. Stehen einer öffentlichen Instanz keine Administrationswerkzeuge zur Verfügung, erhöht das die Sicherheit der **IdentityServer Datenbanken**.
Die Administration kann hier beispielsweise nur über eine Instanz erfolgen, die nicht über das Internet erreichbar ist (nur Intranet, ...) und auf die gleiche 
Datenbank schreibt, wie die öffentliche Instanz.

Abschnitt ``Account``
---------------------

.. code:: javascript

   "Account": {
        "DenyManageAccount": true,   // default: false
        "DenyRegisterAccount": true, // default: false
   }

Hier können Einschränken im Bezug auf *User Accounts* getroffen werden:

* **DenyManageAccount:** Ein angemeldeter User kann selbstständig keine Änderungen an seinem Account vornehmen. Das kann Sinn machen, wenn nur Administrator 
  Die Benutzerkonten verwalten soll, bzw. wenn die Administration von Accounts bereits über eine anderer Anwendung erfolgt.

* **DenyRegisterAccount:** Anwender können sich beim IdentityServer nicht selbst registrieren.  

Abschnitt ``Cookie``
--------------------

.. code:: javascript 

    "Cookie": {
        "Name": "identityserver-nova-identity",
        "Domain": "identity.my-server.com",
        "Path": "/",
        "ExpireDays": 365
    }

Der **IdentityServer.Nova** erzeugt für einen angemeldeten Benutzer ein *Cookie*. Hier kann genauer bestimmt werden, wie dieses *Cookie* aufgebaut ist:

* **Name:** der Name des *Cookie*
* **Domain:** gibt an, für welche *Domain* das *Cookie* gültig ist
* **Path:** der Pfad, für den das *Cookie* gültig ist
* **ExpireDays:** gibt an, wie lange das *Cookie* gültig ist

Über **Domain** und **Pfad** kann eingeschränkt werden, wann ein *Cookie* vom Browser zum Server geschickt wird. Grundsätzlich sollte diese *Cookie* nur 
zum **IdentityServer.Nova** geschickt werden!

Abschnitt ``Mail``
------------------

.. code:: javascript

    "Mail": {
        "Smtp": {
            "FromEmail": "no-reply@is-nova.com",
            "FromName": "IdentityServer Nova",
            "SmtpServer": "localhost",
            "SmtpPort": 1025
        }
        // or
        "MailJet": {
            "FromEmail": "no-reply@is-nova.com",
            "FromName": "IdentityServer Nova",
        	"ApiKey": "...",
            "ApiSecret": "..."
        }
        // or
        "SendGrid": {
            "FromEmail": "no-reply@is-nova.com",
            "FromName": "IdentityServer Nova",
        	"ApiKey": "...",
        }
    }

Bei ``Forget Password`` und ``Register new user``, werden an den User E-Mails geschickt. In diesem Abschnitt kann festgelegt werden, wie diese Mails verschickt werden.
Standardmäßig wird bisher ``Smtp`` oder ``MailJet`` oder ``SendGrid`` angeboten. Gibt man nichts an, wird die Mail nicht verschickt, sondern in *Logging* ausgegeben.
Diese Möglichkeit sollte nur der Entwicklung verwendet werden. 

Abschnitt ``Configure``
-----------------------

Hier können das Verhalten der **IdentityServer.Nova** Anwendung über *Middlewares* gesteuert werden.

.. code:: javascript

    "Configure": {
        "UseHttpsRedirection": "false",         // default: true
        "AddXForwardedProtoMiddleware": "true"  // default: false
    }

* **UseHttpsRedirection:** Der IdentityServer leitet automatisch auf HTTPS Verbindungen um. Läuft die Anwendung in einen *Kubernetes* Cluster, ist das nicht immer 
  wünschenswert. Hier läuft die Anwendung im Cluster über das HTTP Protokoll, über den *Ingress* ist sie allerdings nur über HTTP aufrufbar.

* **AddXForwardedProtoMiddleware:** Für **IdentityServer.Nova** ist ein Aufruf über HTTPS erforderlich! Ändert man mit **UseHttpsRedirection** die automatische Umleitung,
  funktioniert der **IdentityServer** eventuell nicht mehr wie erwartet. Mit dir **XForwardedProtoMiddleware** wird gewährleistet, der ``X-Forwarded-Proto`` Header 
  berücksichtigt wird. Wird der **IdentityServer** in einen *Kubernetes* Cluster über den *Ingress* mit HTTPS aufgerufen, funktioniert der Server auch noch, wenn die 
  Kommunikation innerhalb des Clusters mit HTTP funktioniert.
  



