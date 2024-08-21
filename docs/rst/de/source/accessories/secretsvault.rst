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