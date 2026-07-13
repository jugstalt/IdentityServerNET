Realms (Multi-Tenancy)
=======================

Ein **Realm** macht aus einer einzelnen **IdentityServerNET**-Instanz einen Multi-Tenant-Server. Jeder
Realm ist ein isolierter Mandant mit eigenem Administrator, eigenen Clients, Rollen und Ressourcen — und
teilt sich trotzdem eine Installation, eine Datenbank und die globalen Identity-/API-Ressourcen mit allen
anderen.

Die Zugehörigkeit zu einem Realm basiert auf der **E-Mail-Domain**: Ein Realm besitzt eine oder mehrere
E-Mail-Domains (z. B. ``acme.com``), und jeder Benutzer, dessen E-Mail-Adresse auf eine dieser Domains
endet, gilt automatisch als Mitglied dieses Realms. Es gibt keinen eigenen Schritt "Benutzer einem Realm
zuweisen" und auch keinen Suffix am Benutzerkonto selbst — die Zugehörigkeit ergibt sich rein aus der
E-Mail-Domain.

Realm-gebundene Objekte — Clients, Rollen und Identity-/API-Ressourcen — tragen dagegen einen
Namespace-Suffix, ``{name}@{realm}`` (z. B. ein Client ``my-app@acme``). Damit sind die Clients und
Rollen eines Realms klar von den globalen und von denen anderer Realms getrennt, während ein globales
Objekt (ohne ``@realm``-Suffix) standardmäßig nur von Benutzern verwendet werden kann, die **keinem**
Realm angehören — siehe :doc:`cross-realm-access` dazu, wie ein Client auch Benutzer anderer Realms
zulassen kann.

.. note::

    Realms sind eine **optionale** Funktion. Eine Installation, in der nie ein Realm angelegt wird,
    verhält sich exakt wie ein klassischer Single-Tenant-**IdentityServerNET**-Server.

.. toctree::
   :maxdepth: 2
   :caption: Contents:

   managing-realms
   appearance
   cross-realm-access
