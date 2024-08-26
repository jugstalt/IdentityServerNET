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
        "ApplicationTitle": "IdentityServer.Nova",
        "IdentityServer": {
            "PublicOrigin": "https://localhost:44300"
        },
        "ConnectionStrings": {
            // ...
        },

    }