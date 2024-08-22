Payload Signing
===============

Mit **Payload Signing** kann ein beliebiger Payload (Dictionary) in einen JWT Token verpacken.
Das erstellen eines Token kann sowohl über die *Admin-Seite* oder durch eine API Aufruf eines 
berechtigten Clients erfolgen.

JWT Token über Admin-Seite
--------------------------

Um als Administrator einen **JWT Token** zu erzeugen, wechselt man über die *Admin-Seite*
zum Bereich ``Sign Payload UI``.

Dort kann ein **Payload** (Dictionary) als JSON angeben werden und daraus ein 
ein Token erzeugt werden:

.. image:: img/signing1.png
    :width: 300

Der erzeugte Token enthält dann folgende Informationen:

.. image:: img/signing2.png
    :width: 300

JWT Token über Signing API
--------------------------

Über die **Signing API** kann ein berechtigter Client, JWT Tokens mit benutzerdefiniertem
Payload erstellen. Folgende Voraussetzungen sind dazu notwendig:

API Resource einrichten
+++++++++++++++++++++++

Im ersten Schritt muss die **API Resource** eingerichtet werden (falls noch nicht eingerichtet).
Dazu wechselt man von der *Admin-Seite* in den Bereich ``Resources (Identity & APIs)`` und dort 
zum Menüpunkt ``API Resources``. Dort muss eine **API Resource** mit dem Namen ``signing-api``
angelegt werden. Ist diese Resource noch nicht vorhanden, kann sie der einfach halber auch 
über den Bereich ``Add default API Resource``:

.. image:: img/signing3.png

.. note::

    Unter ``Scopes`` muss für diese **API Resource** ein **Scope** ``signing-api`` existieren.
    Das sollte automatisch beim Hinzufügen passiert sein. Falls nicht muss der Scope manuell 
    hinzugefügt werden.

API Client erstellen
++++++++++++++++++++

Als nächstes muss ein *Client* erstellt werden, der auf die **Signing API** zugreifen darf.
Dazu muss von der *Admin-Seite* in den Bereich ``Clients`` wechseln. Dort muss ein **API Client**
angelegt werden:

.. image:: img/signing4.png

Für den Client muss unter ``Client Secrets`` ein **Secret** vergeben werden, dass der Client später übergeben muss, 
um einen Token abzuholen.
Unter dem Menüpunkt ``Scopes`` muss für diesen Client noch der **API Scope** ``signing-api`` hinzugefügt
werden:

.. image:: img/signing5.png

Token über HTTP Request erstellen
+++++++++++++++++++++++++++++++++

Zuerst muss ein gültiger **Bearer Token** abgeholt werden:

.. code::

    POST https://localhost:44300/connect/token
    Content-Type: application/x-www-form-urlencoded

    grant_type=client_credentials
    &client_id=signing-client
    &client_secret=secret1
    &scope=signing-api
     
Wird ein **Access Token** zurück gegeben, kann dieser für den zum Erstellen des 
**Tokens** verwendet werden:

.. code::

    POST https://localhost:44300/api/signing
    Authorization: Bearer eyJhbGciOiJSUzI1N...
    Content-Type: application/x-www-form-urlencoded

    name=doc1
    &hash=1234567890

Token über IdentityServer.Nova.Clients abholen
++++++++++++++++++++++++++++++++++++++++++++++

Das **nuget** Packet ``IdentityServer.Nova.Clients`` bietet folgende Methoden,
um auf die **Signing API** zuzugreifen:

.. code:: bash

    dotnet add package IdentityServer.Nova.Clients

.. code:: csharp

    using IdentityServer.Nova.Clients;

    // ...

    string issuerAddress = "https://localhost:44300",
           clientId = "signing-client", 
           clientSecret = "secret";

    var singingApiClient = new SigningApiClient(clientId, clientSecret);
    var signingResponse = await singingApiClient.SignData(issuerAddress, new NameValueCollection()
    {
        { "name", "doc1" },
        { "hash", "1234567890" }
    });

    if (signingResponse.Succeded == false)
    {
        throw new Exception($"Signing response error: {signingResponse.ErrorMessage}");
    }

    var token = signingResponse.SecurityToken

Token validieren 
++++++++++++++++

Ein Token aus der **Signing API** kann einer Application übergeben werden. Diese kann 
die Gültigkeit des Tokens prüfen und einzelne Claims abfragen. Dafür bietet .NET Core 
verschiedene Möglichkeiten. Einige Methoden sind ebenfalls im **nuget** Packet 
``IdentityServer.Nova.Clients`` enthalten

.. code:: csharp

    string issuerAddress = "https://localhost:44300",
           token = "...", 

    // Validate a token and get a claim

    var hash = await SigningApiClient.GetValidatedClaimFromToken(
                token, 
                issuerAddress, 
                "hash"
            );
    // or
    var dictionay = await SigningApiClient.GetValidatedClaimsFromToken(
                token, 
                issuerAddress, 
                ["name", "hash"]
            );

