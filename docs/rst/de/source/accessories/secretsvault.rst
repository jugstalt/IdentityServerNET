Secrets Vault
=============

Das **Secrets Vault** dient zum zentralen Speichern von **Secrets** wie beispielsweise:

* ConnectionStrings
* Passwörter
* Client Secrets

Die **Secrets** werden sogenannten **Lockers** zugeordnet. Eine Anwendung erhält Zugriff auf einen **Locker** und kann die 
darin enthaltenen **Secrets** abrufen. Für jedes Secret können verschiedene **Versionen** angelegt werden. Ändert sich beispielsweise ein 
*ConnectionString*, kann eine neue Version für ein **Secret** angelegt werden. Sobald alle **Clients** auf die neue Version des *ConnectionStrings*
umgestellt sind, kann die alte **Version** gelöscht werden.

.. note::

    Idealerweise wird ein **Locker** pro *Client* angelegt. Der **Locker** enthält dann nur die für den *Client* relevanten **Secrets**.

.. note::

    Beim Abrufen eines **Secrets** kann auf die Angabe der **Version** verzichtet werden. In diesem Fall sollte bedacht werden,
    dass der Client immer auf die zuletzt erstellte Version zugreift. Wird eine neue **Version** erstellt, ruft der *Client* beim 
    nächsten Zugriff die neue **Version** ab!

Zum Verwalten des **Secrets Vault** klickt man auf der *Admin-Seite* auf die entsprechende Kachel.

Locker anlegen
--------------

Ein neuer Locker wird über das ``Create new locker``-Formular angelegt:

.. image:: img/secretsvault1.png

Secret anlegen
--------------

Um ein **Secret** innerhalb eines **Lockers** zu erstellen, öffnet man im entsprechenden Locker den Menüpunkt ``Secrets`` und erstellt über 
das Formular ``Create new secret`` ein neues **Secret**. Hier müssen nur der Name und optional eine Beschreibung des **Secrets** angegeben werden:

.. image:: img/secretsvault2.png

Secret-Versionen anlegen
------------------------

Um einem **Secret** schließlich einen Wert zuzuweisen, müssen **Versionen** erstellt werden. Dazu wählt man das **Secret** aus der Liste aus
und wechselt zum Menüpunkt ``Versions``:

.. image:: img/secretsvault3.png

In der Liste erscheint die neu erstellte Version des **Secrets**. Erstellt man weitere Versionen, wird die neueste Version immer ganz oben angezeigt.
Mehrere Versionen sind sinnvoll, wenn sich ein ConnectionString ändert, z. B. aufgrund eines neuen Datenbankservers. Hier kann eine neue Version angelegt werden,
und sukzessive alle Clients auf die neue Datenbank umgestellt werden. Danach können alte Versionen wieder gelöscht werden.

Klickt man auf den Link, der bei der **Version** angezeigt wird, öffnet sich ein *JSON* im Browser:

.. image:: img/secretsvault4.png

.. note::

    Der letzte Teil der URL ist der ``versionTimeStamp``. Dieser kann beim Abrufen der **Secret-Version** auch weggelassen werden. 
    In diesem Fall wird immer die zuletzt erstellte Version zurückgegeben. 

    .. image:: img/secretsvault5.png

Secret abfragen
---------------

**Secrets** können mit der oben gezeigten Methode (Klick auf den Link) abgerufen werden. Dieser Link ist jedoch nur für Administratoren mit Browserzugriff
verfügbar. Wenn ein nicht als Administrator angemeldeter Nutzer diesen Link im Browser öffnet, wird er zur *Login*-Seite weitergeleitet.

Der Zugriff für Clients erfolgt über die Übergabe eines **Bearer Tokens**. Um ein gültiges Token abzurufen, sind folgende Schritte notwendig:

API Resources anlegen 
+++++++++++++++++++++

*IdentityServerNET* bietet eine API zum Abrufen von Secrets (https://.../api/api/secretsvault?v=1.0&path={secret-version-path}).
Um diese API verwenden zu können, müssen zunächst die erforderlichen **API Resources** angelegt werden. Dazu wechselt man über die *Admin-Seite* in den 
Bereich **Resources (Identities & APIs)** und dort zum Menüpunkt **API Resources**. Falls noch nicht vorhanden, muss
hier zuerst die API Resource ``secrets-vault`` erstellt werden:

.. image:: img/secretsvault6.png

Für die Resource muss dann unter ``Scopes`` noch ein **Scope** mit dem Namen des **Lockers** eingetragen werden:

.. image:: img/secretsvault7.png

Client anlegen
++++++++++++++

Damit ein Client auf den **Locker** zugreifen kann, wechselt man von der *Admin-Seite* in den Bereich **Clients** und 
erstellt dort einen **API Client**:

.. image:: img/secretsvault8.png

Für den Client muss unter ``Client Secrets`` ein **Secret** festgelegt werden, das der Client später übermitteln muss, um ein Token abzurufen.
Unter ``Scopes`` sind die beiden **Scopes** ``secrets-vault`` und ``secrets-vault.{locker-name}`` aus dem Bereich ``Add existing resource scope``
hinzuzufügen:

.. image:: img/secretsvault9.png

Secret über HTTP Request abholen
++++++++++++++++++++++++++++++++

Zuerst muss ein gültiger **Bearer Token** abgerufen werden:

.. code::

    POST https://localhost:44300/connect/token
    Content-Type: application/x-www-form-urlencoded

    grant_type=client_credentials&
    client_id=my-api-secrets&
    client_secret=secret&
    scope=secrets-vault secrets-vault.my-api-locker

Wird ein **Access Token** zurückgegeben, kann dieses Token zum Abrufen des **Secrets** verwendet werden:

.. code::

    GET https://localhost:44300/api/secretsvault?v=1.0&path=my-api-locker/db-connectionstring
    Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkR...

.. note:: 

    Hier wurde im Pfad keine Version angegeben. Möchte man eine bestimmte Version abrufen, muss diese im Pfad angegeben werden, z. B.
    https://localhost:44300/api/secretsvault?v=1.0&path=my-api-locker/db-connectionstring/{version}


Secret über IdentityServerNET.Clients abholen
+++++++++++++++++++++++++++++++++++++++++++++

Das **NuGet**-Paket ``IdentityServerNET.Clients`` bietet folgende Methoden,
um auf die **Secrets API** zuzugreifen:

.. code:: bash

    dotnet add package IdentityServerNET.Clients

.. code:: csharp

    var secretsVaultClient = new IdentityServerNET.Clients.SecretsVaultClient("my-api-secrets", "secret");
    await secretsVaultClient.OpenLocker("https://localhost:44300", "my-api-locker");
    var secretResponse = await secretsVaultClient.GetSecret("db-connectionstring");

    Console.WriteLine(secretResponse.GetValue())
    









