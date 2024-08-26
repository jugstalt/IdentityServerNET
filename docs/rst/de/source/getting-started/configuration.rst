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
                "UseHttpsRedirection": "false",   // default: false
                "AddXForwardedProtoMiddleware": "true"  // default: false
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

Abschnitt ``SigningCredential``
-------------------------------

Abschnitt ``Login``
-------------------

Abschnitt ``Admin``
-------------------

Abschnitt ``Account``
---------------------

Abschnitt ``Cookie``
--------------------

Abschnitt ``Mail``
------------------

Abschnitt ``Configure``
-----------------------

