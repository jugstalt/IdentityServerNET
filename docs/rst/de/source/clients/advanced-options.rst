Client-Referenz (Advanced Options)
==================================

Beim Bearbeiten eines Clients sind dessen Eigenschaften in Menüpunkte gegliedert. Diese Seite ist
eine Referenz für die drei "erweiterten" Bereiche — **Advanced Options**, **Advanced Collections**
und **Advanced Properties**. Die meisten Werte sind bereits durch das Client-Template vorkonfiguriert
und müssen selten geändert werden.

Die übrigen Menüpunkte (``Name``, ``Client Secrets``, ``Allowed Grants``, ``Allowed Scopes``) werden
in :doc:`webapp`, :doc:`api` und :doc:`grants-and-flows` behandelt.

Advanced Options
----------------

Dies sind An/Aus-Schalter. Jede Beschreibung entspricht dem im Admin-UI angezeigten Text.

**Authentifizierung & Sicherheit**

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Option
     - Beschreibung
   * - ``RequireClientSecret``
     - Erfordert ein Client Secret für Anfragen an den Token-Endpoint.
   * - ``RequirePkce``
     - Erzwingt Proof Key for Code Exchange (PKCE) für Authorization-Code-Flows.
   * - ``AllowPlainTextPkce``
     - Erlaubt die weniger sichere Klartext-Code-Challenge-Methode für PKCE.
   * - ``RequirePushedAuthorization``
     - Erfordert, dass Authorization Requests Pushed Authorization Requests (PAR) verwenden.
   * - ``AllowAccessTokensViaBrowser``
     - Erlaubt die Rückgabe von Access Tokens in Browser-URL-Fragmenten (Implicit Flow).
   * - ``EnableLocalLogin``
     - Erlaubt Benutzern die Anmeldung mit lokalem Benutzernamen und Passwort.
   * - ``Enabled``
     - Aktiviert oder deaktiviert diesen Client vollständig.

**Consent**

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Option
     - Beschreibung
   * - ``RequireConsent``
     - Zeigt Benutzern vor der Autorisierung des Clients den Consent-Screen an.
   * - ``AllowRememberConsent``
     - Lässt Benutzer ihre Consent-Entscheidung speichern, um den Consent-Screen künftig zu überspringen.

**Tokens & Claims**

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Option
     - Beschreibung
   * - ``AllowOfflineAccess``
     - Erlaubt Clients, Refresh Tokens für Offline-Zugriff anzufordern.
   * - ``UpdateAccessTokenClaimsOnRefresh``
     - Stellt beim Verwenden eines Refresh Tokens aktualisierte Access-Token-Claims neu aus.
   * - ``IncludeJwtId``
     - Fügt jedem JWT Access Token einen eindeutigen jti-Claim zur Nachverfolgung hinzu.
   * - ``AlwaysSendClientClaims``
     - Nimmt Client-Claims immer in den Token auf, auch ohne Benutzerinteraktion.
   * - ``AlwaysIncludeUserClaimsInIdToken``
     - Nimmt alle angeforderten Benutzer-Claims direkt in den Identity Token auf.

**Logout**

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Option
     - Beschreibung
   * - ``FrontChannelLogoutSessionRequired``
     - Nimmt die Session-ID in Front-Channel-Logout-iframe-Requests auf.
   * - ``BackChannelLogoutSessionRequired``
     - Sendet die Session-ID in Back-Channel-Logout-Benachrichtigungen.

Advanced Collections
--------------------

Listenbasierte Werte — ein Eintrag pro Zeile. Kommt später ein Request mit einem Wert, der nicht in
der jeweiligen Liste steht, weist **IdentityServerNET** ihn ab.

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Collection
     - Zweck
   * - ``RedirectUris``
     - Erlaubte URIs, zu denen nach erfolgreicher Anmeldung weitergeleitet wird (Authorization Code / Implicit). Für interaktive Clients erforderlich.
   * - ``PostLogoutRedirectUris``
     - Erlaubte URIs, zu denen nach dem Logout weitergeleitet wird.
   * - ``AllowedCorsOrigins``
     - Browser-Origins, die IdentityServerNET-Endpoints per CORS aufrufen dürfen. Relevant für SPA-/JavaScript-Clients.
   * - ``IdentityProviderRestrictions``
     - Schränkt ein, welche externen Identity Provider dieser Client verwenden darf. Leer bedeutet: alle konfigurierten Provider sind erlaubt.
   * - ``AllowedUserDomains``
     - Nur für Realm-gebundene Clients. Zusätzliche, über den eigenen Realm des Clients hinausgehende
       E-Mail-Domains, die sich anmelden dürfen; ``*`` erlaubt jeden Benutzer. Siehe
       :doc:`../realms/cross-realm-access`.

.. note::

   Die einzelnen Logout-URIs ``FrontChannelLogoutUri`` und ``BackChannelLogoutUri`` werden zusammen
   mit den Collections bearbeitet. Sie verweisen auf den Client-Endpoint, den IdentityServerNET
   aufruft, um ein Logout zu propagieren.

Advanced Properties
-------------------

Numerische Lebensdauern und Token-Verhalten. Die Defaults sind in Sekunden angegeben und müssen in
der Regel nicht geändert werden.

.. list-table::
   :widths: 35 20 45
   :header-rows: 1

   * - Property
     - Default
     - Bedeutung
   * - ``IdentityTokenLifetime``
     - 300
     - Lebensdauer des Identity Tokens (5 Minuten).
   * - ``AccessTokenLifetime``
     - 3600
     - Lebensdauer des Access Tokens (1 Stunde).
   * - ``AuthorizationCodeLifetime``
     - 300
     - Lebensdauer des Authorization Codes (5 Minuten).
   * - ``AbsoluteRefreshTokenLifetime``
     - 2592000
     - Maximale Lebensdauer eines Refresh Tokens (30 Tage).
   * - ``SlidingRefreshTokenLifetime``
     - 1296000
     - Gleitendes Fenster, um das ein Refresh Token bei Verwendung verlängert wird (15 Tage).
   * - ``ConsentLifetime``
     - null
     - Lebensdauer des gespeicherten Consents. ``null`` bedeutet: läuft nie ab.
   * - ``DeviceCodeLifetime``
     - 300
     - Lebensdauer eines Device Codes im Device Flow (5 Minuten).
   * - ``UserSsoLifetime``
     - null
     - Maximales Alter der Single-Sign-On-Session eines Benutzers für diesen Client. ``null`` bedeutet unbegrenzt.

Token-Verhalten (Enumerationen):

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Property
     - Werte
   * - ``RefreshTokenUsage``
     - ``OneTime`` (Refresh Token bei jeder Verwendung rotieren — empfohlen) oder ``Reuse`` (denselben Token behalten).
   * - ``RefreshTokenExpiration``
     - ``Sliding`` (verlängert sich bei Verwendung, bis zur absoluten Lebensdauer) oder ``Absolute`` (fester Ablauf).
   * - ``AccessTokenType``
     - ``Jwt`` (eigenständiger JSON Web Token) oder ``Reference`` (opaker Token, per Introspection validiert).

Weitere Properties:

* ``ClientClaimsPrefix`` — Präfix für Client-Claims im Token (Standard ``client_``).
* ``PairWiseSubjectSalt`` — Salt zur Erzeugung paarweiser (pairwise) Subject-Identifier.
* ``UserCodeType`` — das User-Code-Format für den Device Flow.
