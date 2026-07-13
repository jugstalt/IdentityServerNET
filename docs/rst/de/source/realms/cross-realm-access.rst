Realm-fremden und öffentlichen Zugriff auf einen Client erlauben
====================================================================

Standardmäßig kann ein Realm-gebundener Client (``my-app@acme``) nur von Benutzern verwendet werden, die
zu diesem Realm gehören — also deren E-Mail-Domain zu den :doc:`Domains <managing-realms>` des Realms
zählt. Der Versuch, sich mit einem Benutzer eines anderen Realms oder ganz ohne Realm anzumelden, wird
bereits im allerersten Schritt abgelehnt (noch bevor ein Passwort abgefragt wird) — es wird also kein
Benutzername preisgegeben (keine User-Enumeration).

Manchmal ist diese Standardeinstellung zu restriktiv:

* Ein Realm-Admin baut einen Client, der auch von Benutzern eines externen Partners genutzt werden soll.
* Ein Client soll für **jeden** offen sein, unabhängig vom Realm — etwa eine öffentlich zugängliche App,
  die zufällig innerhalb der Client-Liste eines Realms angelegt wurde.

Beide Fälle werden direkt am Client konfiguriert, ohne den Realm selbst zu verändern.

Konfiguration von ``AllowedUserDomains``
--------------------------------------------

Client im Admin-UI öffnen und zu **Advanced Collections** wechseln (siehe
:doc:`../clients/advanced-options`). Die Collection ``AllowedUserDomains`` akzeptiert eine E-Mail-Domain
pro Zeile, zusätzlich zum eigenen Realm des Clients:

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Wert von ``AllowedUserDomains``
     - Wirkung
   * - *(leer, Standard)*
     - Nur Benutzer des eigenen Realms des Clients dürfen sich anmelden.
   * - ``partner.com``
     - Benutzer des Client-Realms **und** Benutzer, deren E-Mail auf ``partner.com`` endet, dürfen sich
       anmelden.
   * - ``partner.com``, ``other.org``
     - Mehrere externe Domains können eingetragen werden, eine pro Zeile.
   * - ``*``
     - Der Client akzeptiert **jeden** Benutzer, unabhängig vom Realm — de facto ein öffentlicher Client.

.. note::

    ``AllowedUserDomains`` wirkt sich nur auf **Realm-gebundene** Clients aus. Globale Clients (ohne
    ``@realm``-Suffix) akzeptieren ohnehin standardmäßig jeden Benutzer und sind von dieser Einstellung
    nicht betroffen.

Wo die Prüfung greift
------------------------

Die Domain-Prüfung läuft konsistent bei jedem Anmeldeweg, sodass eine Öffnung (oder Einschränkung) des
Zugriffs überall gleichzeitig wirkt:

* Der **Identifier-Schritt** des Standard-Logins (lehnt eine nicht erlaubte Domain ab, bevor ein Passwort
  abgefragt wird).
* Die **Passwort**-Anmeldung (zusätzliche Absicherung, falls der Identifier-Schritt umgangen wurde).
* Die **Passkey**-Anmeldung, sowohl als erster Faktor (passwortlos) als auch als zweiter Faktor.
* **Externe/Third-Party-Logins** (z. B. ein OIDC- oder SAML-Identity-Provider) — ein andernfalls gültiger,
  aber für diesen Client nicht zugelassener Benutzer erhält einen ``access_denied``-Fehler, und es wird
  kein Anwendungs-Session-Cookie ausgestellt.
* **Token-Ausstellung und -Refresh** — dieselbe Regel wird bei jeder Token-Validierung bzw. -Erneuerung
  erneut geprüft. Entfernt man eine Domain (oder das Wildcard) aus ``AllowedUserDomains``, wirkt sich das
  spätestens beim nächsten Token-Refresh des Benutzers aus — nicht erst bei dessen nächster
  interaktiver Anmeldung.

.. important::

    Sicherheitsaspekt bei ``*``: Ein Realm-Admin kann dies an seinen **eigenen** Clients konfigurieren,
    ganz ohne Beteiligung des Systemadministrators. Ein Wildcard macht diesen einen Client faktisch zu
    einer offenen, Realm-übergreifenden Login-Fläche — jeder Benutzer der gesamten Installation kann
    dafür Tokens erhalten. Es empfiehlt sich, für einen solchen Client zusätzlich ``RequireConsent``
    (siehe :doc:`../clients/advanced-options`) zu aktivieren, damit Benutzer außerhalb des Realms
    explizit sehen, was sie autorisieren, bevor sie fortfahren.
