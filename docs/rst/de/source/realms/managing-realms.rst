Realms anlegen und verwalten
==============================

Das Anlegen eines Realms — wie auch das Löschen — ist eine **systemweite** Berechtigung. Sie erfordert
die Rolle ``identityserver-realm-administrator``, die (anders als die übrigen Admin-Rollen) niemals an
einen Realm-Admin delegiert wird: Ein Realm-Admin verwaltet seinen eigenen Realm, kann aber keine
weiteren Realms anlegen.

Einen Realm anlegen
---------------------

Ist man als Systemadministrator mit der Realm-Administrator-Rolle angemeldet, erscheint auf der
Admin-Startseite die Kachel ``Realms...``. Sie führt zur Realm-Liste und zum Formular
**Create new realm**:

* **Realm name** — der Kleinbuchstaben-Slug, der als ``@realm``-Suffix verwendet wird (nur Buchstaben,
  Ziffern und ``-``, z. B. ``acme``).
* **Primary domain** — die E-Mail-Domain, an der das Realm-Admin-Konto verankert wird (z. B.
  ``acme.com``).
* **Additional domains** — weitere E-Mail-Domains, die zu diesem Realm gehören, Komma- oder
  Leerzeichen-getrennt. Kann auch leer bleiben und später über die Verwaltungsseite **Domains**
  ergänzt werden (siehe unten).

.. note::

    Eine Domain kann jeweils nur **einem** Realm gehören. Das Anlegen eines Realms mit einer bereits
    andernorts zugewiesenen Domain wird abgelehnt.

Beim Anlegen provisioniert **IdentityServerNET** automatisch:

* Ein **Realm-Admin-Konto**, ``admin@{PrimaryDomain}`` (z. B. ``admin@acme.com``), mit einem zufällig
  generierten Passwort.
* Fünf **delegierte Admin-Rollen**, gebunden an den neuen Realm (``role@realm``, z. B.
  ``identityserver-user-administrator@acme``), die diesem Admin-Konto zugewiesen werden:

  .. list-table::
     :widths: 35 65
     :header-rows: 1

     * - Delegierte Rolle
       - Berechtigt (nur innerhalb des Realms)
     * - User Administrator
       - Verwaltung von Benutzerkonten, deren E-Mail zu einer der Realm-Domains gehört.
     * - Role Administrator
       - Verwaltung eigener Rollen innerhalb des Realms.
     * - Resource Administrator
       - Verwaltung von Identity-/API-Ressourcen innerhalb des Realms.
     * - Client Administrator
       - Verwaltung von Clients innerhalb des Realms.
     * - Secrets Vault Administrator
       - Verwaltung der eigenen Locker/Secrets im :doc:`Secrets Vault <../accessories/secretsvault>`.

  Die Realm-Verwaltung selbst und das **Payload Signing**-Tool bleiben systemweit und werden **nicht**
  an Realm-Admins delegiert.

.. note::

    Realms, die **vor** Einführung der Secrets-Vault-Delegation angelegt wurden, erhalten die Secrets
    Vault Administrator-Rolle nicht rückwirkend. Sie muss einmalig manuell über die ``User Roles``-Seite
    des Realm-Admins vergeben werden, mit der Rolle
    ``identityserver-secretsvault-administrator@{realm}``.

.. important::

    Das generierte Admin-Passwort wird **genau einmal** angezeigt, direkt nach dem Anlegen, in der
    Statusmeldung auf der Realm-Liste. Es sollte sofort notiert werden — ein erneutes Abrufen ist nicht
    möglich (nur ein Zurücksetzen über die reguläre ``Set Password``-Funktion).

    Es handelt sich außerdem um ein **Einmal-Passwort**: Meldet sich ``admin@{PrimaryDomain}`` das erste
    Mal an, wird sofort ein neues Passwort verlangt, bevor irgendetwas anderes zugänglich ist. Siehe
    :doc:`Einmal-Passwörter <../getting-started/admin-server>` für Details zu diesem Mechanismus.

Realm-Admin — eingeschränkte Verwaltung
------------------------------------------

Nach der Anmeldung als ``admin@acme.com`` sieht der Realm-Admin dieselben Admin-Kacheln wie ein normaler
Administrator (Users, Roles, Resources, Clients, Appearance) — jede Liste und jede Bearbeitungsseite ist
jedoch transparent auf diesen Realm gefiltert:

* **Users** zeigt bzw. erstellt nur Konten, deren E-Mail zu den Domains des Realms gehört.
* **Roles**, **Resources** und **Clients**, die der Realm-Admin anlegt, erhalten automatisch den
  Namespace ``{name}@acme`` — der Admin muss den Suffix nie selbst eingeben, er wird beim Schreiben
  angehängt und für die Anzeige wieder entfernt.
* Der Versuch, ein Objekt eines anderen Realms (oder ein globales Objekt) über eine erratene ID zu laden,
  wird abgelehnt — dieselbe Ownership-Prüfung greift sowohl beim Lesen als auch beim Schreiben.

Einen bestehenden Realm verwalten
------------------------------------

Über die Realm-Liste ("Realms...") gelangt man zu den Verwaltungsseiten des jeweiligen Realms:

**General**
    ``Name`` und ``Primary domain`` des Realms stehen nach dem Anlegen fest. Nur der ``Display name``
    (eine menschenlesbare Bezeichnung) kann hier geändert werden.

**Domains**
    Ein Textfeld mit allen E-Mail-Domains des Realms, eine pro Zeile. Die Primärdomain ist immer
    automatisch enthalten und kann nicht entfernt werden. Weitere Domains hier hinzufügen, um den Kreis
    der Realm-Mitglieder zu erweitern — etwa wenn der Mandant später eine zusätzliche Firmendomain
    erwirbt.

**Users**
    Eine schreibgeschützte Liste aller Benutzerkonten, deren E-Mail-Domain zu einer der Realm-Domains
    passt, mit Verlinkung zur regulären ``Edit User``-Seite.

**Delete**
    Deprovisioniert den Realm dauerhaft. Dabei werden entfernt:

    * Alle Realm-gebundenen **Clients**
    * Alle Realm-gebundenen **Rollen**
    * Alle Realm-gebundenen **Ressourcen**
    * Das Realm-**Admin-Konto**

    .. note::

        Die Endanwender-Konten in den Domains des Realms bleiben **erhalten** — das Löschen eines Realms
        löscht nicht dessen Mitglieder, sondern nur das administrative Gerüst des Mandanten
        (Clients/Rollen/Ressourcen/Admin-Konto).

    Als Sicherheitsabfrage muss der Realm-Name zur Bestätigung erneut eingegeben werden.

Referenz: Namenskonvention
------------------------------

Die Realm-Namespace-Konvention wird konsequent auf jede Realm-gebundene ID angewendet:

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Bezeichner
     - Beispiel
   * - Globaler Client/Rolle/Ressource
     - ``my-app`` (kein Suffix — außerhalb jedes Realms nutzbar)
   * - Realm-gebundener Client/Rolle/Ressource
     - ``my-app@acme``
   * - Realm-Admin-Konto
     - ``admin@acme.com`` (eine E-Mail-Adresse — **nicht** mit Namespace versehen; die
       Benutzer-Zugehörigkeit ergibt sich immer aus der Domain)

.. note::

    Standard-OIDC-Scopes und Identity-Ressourcen (``openid``, ``profile``, ``email``, ``address``,
    ``phone``, ``offline_access``, ``roles``) sind per Definition global und werden nie mit einem
    Namespace versehen, auch nicht innerhalb der Client-Konfiguration eines Realms.
