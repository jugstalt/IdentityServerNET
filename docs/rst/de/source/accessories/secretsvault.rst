Secrets Vault
=============

Das **Secrets Vault** dient zum zentralen Speichern von **Secrets** wie beispielsweise:

* ConnectionString
* Passwörter
* Client Secrets

Die **Secrets** werden sogenannten **Lockers** zugeordnet. Eine Anwendung bekommt beispielsweise Zugriff auf einen **Locker** und kann die 
darin enthaltenen **Secrets** abgreifen. Für jedes Secret können unterschiedliche **Versionen** angelegt werden. Ändert sich beispielsweise ein 
*ConnectionString* kann eine neue Version für ein **Secret** angelegt werden. Sind alle **Clients** auf die neue Version des *ConnectionStrings*
umgestellt, kann die alte **Version** gelöscht werden.

.. note::

    Idealerweise wird ein **Locker** pro *Client* angelegt. Im **Locker** gibt es nur die für den *Client* relevanten **Secrets**.

.. note::

    Beim Abholen eine **Secrets** kann auch auf die Angabe der **Version** verzichtet werden. Dann muss allerdings berücksichtigt werden,
    dass der Client immer auf die zuletzt erstellte Version zugreift. Führt man eine neue **Version** an, holt sich ein *Client* beim 
    nächsten Zugriff die neu **Version** ab!

Zum Verwalten des **Secrets Vault** klickt man auf der *Admin Seite* auf die entsprechende Kachel.

Locker anlegen
--------------

Ein neuer Locker wird über das ``Create new locker`` Formular angelegt:

.. image:: img/secretsvault1.png

Secret anlegen
--------------

Um eine **Secret** innerhalb eines **Lockers** anzulegen, öffnet man im entsprechenden Locker den Menüpunkt ``Secrets`` und erstellt über 
das Formular ``Create new secret`` ein neues **Secret**. Hier muss einmal nur der Name und optional die Beschreibung des **Secrets** angeführt 
werden:

.. image:: img/secretsvault2.png

Secret Versionen anlegen
------------------------

Um schließlich dem **Secret** einen Wert zu geben, müssen **Versionen** angelegt werden. Dazu wählt man zuerst das **Secret** aus der Liste aus
und wechselt zum Menüpunkt ``Versions``:

.. image:: img/secretsvault3.png

In der Liste erscheint die neue erstellte Version des **Secrets**. Erstellt man weiter Versionen, wird das neueste Version immer ganz oben angezeigt.
Mehrere Versionen machen Sinn, wenn sich ein ConnectionString ändern kann, zB neuer Datenbankserver. Hier kann eine neue Version angelegt werden,
und sukzessive alle Clients auf die neue Datenbank umgestellt werden. Danach können alte Versionen wieder gelöscht werden.

Klickt man auf den Link, der bei der **Version** angezeigt wird, öffnet sich ein *JSON* im Browser:

.. image:: img/secretsvault4.png

.. note::

    Der letzte Teil der Url ist der ``versionTimeStamp``. Dieser kann auch beim Abholen der **Secret-Version** weggelassen werden. 
    Damit wird immer die zuletzt erstellte Version zurück gegeben. 

    .. image:: img/secretsvault5.png

Secret abfragen
---------------

**Secrets** können mit der oben gezeigten Methode (Klick auf Link) abgeholt werden. Dieser Link ist allerdings nur für Administrator mit Browser
möglich. Möchte man diesen Link im Browser öffnen und ist nicht als Administrator angemeldet, wird man zum *Login* weiter geleitet.

Der Zugriff für Clients mit übergabe eines **Bearer Tokens**. Damit ein gültiger *Token* abgeholt werden sind folgende Schritte notwendig:

API Resources anlegen 
+++++++++++++++++++++

*IdentityServer.Nova* bietet eine API zum abfragen von Secrets (https://.../api/api/secretsvault?v=1.0&path={secret-version-path}).
Um diese API verwenden zu können, müssen zuerst die notwendigen **API Resourcen** angelegt werden. Dazu muss über die *Admin-Seite* in den 
Bereich **Resources (Identities & APIs)** und dort zum Menüpunkt **API Resources** gewechselt werden. Falls noch nicht vorhanden, muss
hier zuerst die API Resource ``secrets-vault`` erstellt werden:

.. image:: img/secretsvault6.png

Für die Resource muss dann unter ``Scopes`` noch ein **Scope** mit dem Namen es **Lockers** eingereicht werden:

.. image:: img/secretsvault7.png

Client anlegen
++++++++++++++

Damit ein Client auf den **Locker** zugreifen kann, muss von der *Admin Seite* in den Bereich **Clients** gewechselt werden und 
dort ein **API Client** erstellt werden:

.. image:: img/secretsvault8.png

Für den Client muss unter ``Client Secrets`` ein **Secret** vergeben werden, dass der Client später übergeben muss, um einen Token abzuholen.
Unter ``Scopes`` sind die beiden **Scopes** ``secrets-vault`` und ``secrets-vault.{locker-name}`` aus dem Bereich ``Add existing resource scope``
hinzuzufügen:

.. image:: img/secretsvault9.png

Secret über HTTP Request abholen
++++++++++++++++++++++++++++++++

Zuerst muss ein gültiger **Bearer Token** abgeholt werden:

.. code::

    POST https://localhost:44300/connect/token
    Content-Type: application/x-www-form-urlencoded

    grant_type=client_credentials&
    client_id=my-api-secrets&
    client_secret=secret&
    scope=secrets-vault secrets-vault.my-api-locker

Wird ein **Access Token** zurück gegeben, kann dieser für den zum Abfragen des **Secrets** verwendet werden:

.. code::

    GET https://localhost:44300/api/secretsvault?v=1.0&path=my-api-locker/db-connectionstring
    Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkR...

.. note:: 

    Hier wurde im Pfad keine Version mitgegeben. Möchte man eine bestimmte Version Abfragen muss diese im Pfad mitgegeben werden, zB
    https://localhost:44300/api/secretsvault?v=1.0&path=my-api-locker/db-connectionstring/{version}


Secret über IdentityServer.Nova.Clients abholen
+++++++++++++++++++++++++++++++++++++++++++++++

Das **nuget** Packet ``IdentityServer.Nova.Clients`` bietet folgende Methoden,
um auf die **Secrets API** zuzugreifen:

.. code:: bash

    dotnet add package IdentityServer.Nova.Clients

.. code:: csharp

    var secretsVaultClient = new IdentityServer.Nova.Clients.SecretsVaultClient("my-api-secrets", "secret");
    await secretsVaultClient.OpenLocker("https://localhost:44300", "my-api-locker");
    var secretResponse = await secretsVaultClient.GetSecret("db-connectionstring");

    Console.WriteLine(secretResponse.GetValue())
    









