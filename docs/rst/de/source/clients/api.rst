API Client
==========

Ein *API Client* ist eine Anwendung, die auf eine **Web API** zugreifen muss, die einen
gültigen **Bearer Token** erfordert, der vom **IdentityServer.Nova** ausgestellt wurde.

API Resource
------------

Um einen **Bearer Token** für eine API auszustellen, muss diese API im ersten Schritt als
**API Resource** angelegt werden. Dazu navigiert man über die ``Admin`` Seite zu 
``Resources (Identity & APIs)``/``API Resources``.
Dort kann eine neue **API Resource** angelegt werden:

.. image:: img/api1.png

Im nächsten Schritt müssen für die **API Resource** mögliche **Scopes** angelegt werden:

.. image:: img/api2.png

.. note::

    Die Namenskonvention für API Resource Scopes ist: ``api-resource-name.scope-name``. 
    Gibt man einen Scope ein, wird dieser automatisch in diese Konvention umgewandelt. Eine Ausnahme ist ein Scope, 
    der den gleichen Namen hat, wie die ``api-resource``. Möchte man einen Scope anlegen, der nicht diese 
    Konvention entspricht, muss dieser mit vorangestelltem ``@@`` angegeben werden, zB ``@@scope-name``

Für eine API werden nach dem erstellen automatisch folgende **Scopes** angelegt:

* ``{api-name}``: Allgemeiner Zugriff auf die API
* ``{api-name}.query``: Lesender Zugriff auf die von der API bereitgestellten Daten
* ``{api-name}.command``: Zusätzlich schreibender Zugriff auf die von der API bereitgestellten Daten



API Client erstellen/bearbeiten
-------------------------------

Um einen neuen *Client* zu erstellen, muss eine eindeutige *Client Id* vergeben werden. 
Optional kann auch ein sprechender Name vergeben werden.

Damit nicht alles manuelle eingegeben werden muss, sollte als Template ``API`` eingetragen 
werden. Außerdem sollte für diesem Template 
die Url zur Web Anwendung eingetragen werden. Die Eingabe der **Scopes** ist optional. Diese können 
auch im nächsten Schritt noch bearbeitet werden:

.. image:: img/api3.png

Wurde der Client erfolgreich erzeugt, kommt man zur Seite ``Modify Client: ...``. Hier sind die 
unterschiedlichen Eigenschaften für den Client in Menüpunkten gegliedert:

``Name``:
+++++++++

.. image:: img/api4.png

Hier kann der sprechende Name für den Client verändert werden. Außerdem kann eine Beschreibung 
für den Client eingetragen werden.

``Client Secrets``
++++++++++++++++++

Hier muss ein Secret angegeben werden, mit dem sich der Client am Identity Server Anmelden muss. Über den
**Random Secret Generator** kann ein sicheres Secret erzeugt werden. Der Einfachheit halber verwenden wir hier 
allerdings als Secret einfach ``secret``:

.. image:: img/api5.png

``Allowed Grants``
++++++++++++++++++

Da beim Erstellen des Clients der Typ ``ApiClient`` gewählt wurde, sollte hier ``ClientCredentials`` ausgewählt sein:

.. image:: img/api6.png

``Allowed Scopes``
++++++++++++++++++

Hier müssen die Scopes hinzugefügt werden, für die ``API Resource`` angelegt worden sind. Die **Scopes** bestimmen in der 
API später spezielle Rechte für den Zugriff auf die API. Beim ``my-api-command`` Client, macht es hier Sinn, den ``my-api`` und 
den ``my-api.command`` Scope aus ``Add existing resource scope`` Bereich zu übernehmen:

.. image:: img/api7.png

``Advanced Properties``
+++++++++++++++++++++++

Hier kann beispielsweise die lebensdauer für einen *AccessToken* definiert werden:

.. image:: img/api8.png

.. note::

    Alle weiteren Menüpunkte sind für *API Clients* weniger relevant und werden nicht im Detail aufgelistete.

Abholen eines AccessTokens
--------------------------

Eine Client Anwendung kann über einen **HTTP Post** Request, mit den notwendigen Parametern im Body, einen AccessToken von *IdentityServer.Nova* abholen.
Die Scopes werden über den Parameter ``scope`` mit leerzeichen als Trennzeichen übergeben:

.. code:: 
    
    POST https://localhost:44300/connect/token
    Content-Type: application/x-www-form-urlencoded

    grant_type=client_credentials&client_id=my-api-commands&client_secret=secret&scope=my-api my-api.command

.. code::

    {
        "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IkVCM...",
        "expires_in": 3600,
        "token_type": "Bearer",
        "scope": "my-api my-api.command"
    }