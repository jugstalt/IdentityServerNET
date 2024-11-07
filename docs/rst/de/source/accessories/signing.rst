Payload Signing
===============

Mit **Payload Signing** kann ein beliebiger Payload (Dictionary) in einen JWT-Token verpackt werden.
Das Erstellen eines Tokens kann entweder über die *Admin-Seite* oder durch einen API-Aufruf eines 
berechtigten Clients erfolgen.

JWT-Token über die Admin-Seite
------------------------------

Um als Administrator einen **JWT-Token** zu erzeugen, navigiert man auf der *Admin-Seite*
zum Bereich ``Sign Payload UI``.

Hier kann ein **Payload** (Dictionary) im JSON-Format angegeben werden, aus dem 
ein Token erzeugt wird:

.. image:: img/signing1.png
    :width: 300

Der erzeugte Token enthält dann die folgenden Informationen:

.. image:: img/signing2.png
    :width: 300

JWT-Token über die Signing API
------------------------------

Über die **Signing API** kann ein berechtigter Client JWT-Tokens mit benutzerdefiniertem
Payload erstellen. Folgende Voraussetzungen müssen dafür erfüllt sein:

API Resource einrichten
+++++++++++++++++++++++

Zunächst muss die **API Resource** eingerichtet werden (falls noch nicht vorhanden).
Wechseln Sie dazu auf der *Admin-Seite* in den Bereich ``Resources (Identity & APIs)`` und dort 
zum Menüpunkt ``API Resources``. Hier muss eine **API Resource** mit dem Namen ``signing-api``
angelegt werden. Falls diese Resource noch nicht vorhanden ist, kann sie einfach 
über den Bereich ``Add default API Resource`` hinzugefügt werden:

.. image:: img/signing3.png

.. note::

    Unter ``Scopes`` muss für diese **API Resource** ein **Scope** ``signing-api`` vorhanden sein.
    Dieser Scope sollte beim Hinzufügen automatisch erstellt worden sein. Falls nicht, muss er manuell 
    hinzugefügt werden.

API Client erstellen
++++++++++++++++++++

Als Nächstes muss ein *Client* erstellt werden, der Zugriff auf die **Signing API** erhält.
Dazu wechseln Sie auf der *Admin-Seite* in den Bereich ``Clients`` und legen dort einen **API Client**
an:

.. image:: img/signing4.png

Für den Client muss unter ``Client Secrets`` ein **Secret** festgelegt werden, das der Client später übermitteln muss, 
um ein Token abzurufen.
Unter dem Menüpunkt ``Scopes`` muss für diesen Client der **API Scope** ``signing-api`` hinzugefügt
werden:

.. image:: img/signing5.png

Token über HTTP Request erstellen
+++++++++++++++++++++++++++++++++

Zuerst muss ein gültiger **Bearer Token** abgerufen werden:

.. code::

    POST https://localhost:44300/connect/token
    Content-Type: application/x-www-form-urlencoded

    grant_type=client_credentials
    &client_id=signing-client
    &client_secret=secret1
    &scope=signing-api
     
Wenn ein **Access Token** zurückgegeben wird, kann dieses Token verwendet werden, um 
den **Token** zu erstellen:

.. code::

    POST https://localhost:44300/api/signing
    Authorization: Bearer eyJhbGciOiJSUzI1N...
    Content-Type: application/x-www-form-urlencoded

    name=doc1
    &hash=1234567890

Token über IdentityServerNET.Clients abrufen
+++++++++++++++++++++++++++++++++++++++++++++

Das **NuGet**-Paket ``IdentityServerNET.Clients`` bietet folgende Methoden,
um auf die **Signing API** zuzugreifen:

.. code:: bash

    dotnet add package IdentityServerNET.Clients

.. code:: csharp

    using IdentityServerNET.Clients;

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

Ein Token aus der **Signing API** kann an eine Anwendung übergeben werden. Diese Anwendung kann 
die Gültigkeit des Tokens prüfen und einzelne Claims abfragen. .NET Core bietet dafür 
verschiedene Möglichkeiten. Einige Methoden sind ebenfalls im **NuGet**-Paket 
``IdentityServerNET.Clients`` enthalten.

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

