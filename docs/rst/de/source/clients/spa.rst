JavaScript- / SPA-Client
========================

Ein *JavaScript-Client* ist eine **Single Page Application** (SPA) oder statische Webseite, die
vollständig im Browser läuft. Da ein solcher Client ein Secret nicht geheim halten kann, wird er als
**Public Client** behandelt: Er authentifiziert Benutzer mit dem **Authorization-Code-Flow, gesichert
durch PKCE**, und verwendet kein Client Secret.

Einen JavaScript-Client erstellen
----------------------------------

Zum Erstellen wird eine eindeutige *Client Id* vergeben und das Template ``JavascriptClient``
gewählt. Die URL der SPA sollte eingetragen werden, damit die Redirect- und CORS-Einträge vorbelegt
werden können.

Das Template ``JavascriptClient`` konfiguriert den Client wie folgt:

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Einstellung
     - Wert
   * - ``AllowedGrantTypes``
     - ``authorization_code``
   * - ``RequirePkce``
     - ``true``
   * - ``RequireClientSecret``
     - ``false``
   * - ``AllowedScopes``
     - ``openid``, ``profile`` (plus optionale API-Scopes)
   * - ``RedirectUris``
     - ``{clientUrl}/callback.html``
   * - ``PostLogoutRedirectUris``
     - ``{clientUrl}/index.html``
   * - ``AllowedCorsOrigins``
     - ``{clientUrl}``

.. note::

   ``AllowedCorsOrigins`` ist für einen browserbasierten Client essenziell: Die SPA ruft den Token-
   und den Userinfo-Endpoint direkt per ``fetch``/``XMLHttpRequest`` auf, daher muss ihr Origin für
   CORS erlaubt sein. Siehe :doc:`advanced-options`.

Flow
----

Ein JavaScript-Client verwendet **Authorization Code + PKCE**. Da es kein Client Secret gibt, schützt
PKCE den Authorization Code gegen Abfangen — es ist daher zwingend erforderlich
(``RequirePkce = true``). Das vollständige Sequenzdiagramm und die zugehörigen HTTP-Requests sind in
:doc:`grants-and-flows` beschrieben.

.. warning::

   Aktivieren Sie für neue SPAs nicht ``AllowAccessTokensViaBrowser`` bzw. den ``implicit``-Grant.
   Der Implicit Flow ist veraltet; Authorization Code mit PKCE ist die aktuelle Empfehlung für
   browserbasierte Clients.

Client-Bibliothek
-----------------

Verwenden Sie im Browser eine standardkonforme OIDC-Bibliothek (z. B. ``oidc-client-ts``). Richten
Sie sie auf die Authority ``https://localhost:44300`` aus, setzen Sie ``response_type=code`` und
konfigurieren Sie dieselbe ``redirect_uri`` (``callback.html``), die am Client registriert ist. Die
Bibliothek führt den PKCE-Handshake und den Token-Austausch für Sie durch.
