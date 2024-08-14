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

    Die Namenskonvention für API Resource Scopes ist: ``resource-id.scope-name``. Das muss
    auch so eingeben werden. Ansonsten kommt eine Fehlermeldung. Möchte man beabsichtigt 
    für eine Anwendung einen Scope angeben, der nicht der Namenskonvention entspricht,
    muss der Scope folgendermaßen eingeben werden: ``@@scope-name``


API Client erstellen/bearbeiten
-------------------------------

Um einen neuen *Client* zu erstellen, muss eine eindeutige *Client Id* vergeben werden. 
Optional kann auch ein sprechender Name vergeben werden.

Damit nicht alles manuelle eingegeben werden muss, sollte als Template ``API`` eingetragen 
werden. Außerdem sollte für diesem Template 
die Url zur Web Anwendung eingetragen werden. Die Eingabe der **Scopes** ist optional. Diese können 
auch im nächsten Schritt noch bearbeitet werden:

.. image:: api/api3.png

Wurde der Client erfolgreich erzeugt, kommt man zur Seite ``Modify Client: ...``. Hier sind die 
unterschiedlichen Eigenschaften für den Client in Menüpunkten gegliedert:

``Name``:
+++++++++

.. image:: img/api4.png

Hier kann der sprechende Name für den Client verändert werden. Außerdem kann eine Beschreibung 
für den Client eingetragen werden.