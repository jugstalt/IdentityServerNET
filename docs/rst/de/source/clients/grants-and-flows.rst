Grant Types und Flows
=====================

Jeder Client legt unter ``Allowed Grants`` einen oder mehrere **Grant Types** fest. Ein Grant Type
bestimmt, *wie* ein Client Tokens von **IdentityServerNET** erhält — welche Endpoints beteiligt
sind, ob ein Benutzer anwesend ist und wie Credentials ausgetauscht werden.

Diese Seite erklärt die verfügbaren Grant Types und führt durch die empfohlenen Flows mit
Sequenzdiagrammen und Beispiel-HTTP-Requests. Alle Beispiele verwenden die Authority
``https://localhost:44300`` und den Demo-Client ``is-webclient-test``.

Übersicht der Grant Types
-------------------------

Folgende Grant Types können unter ``Allowed Grants`` aktiviert werden. Die Beschreibungen
entsprechen denen im Admin-UI:

.. list-table::
   :widths: 30 55 15
   :header-rows: 1

   * - Grant
     - Beschreibung
     - Status
   * - ``authorization_code``
     - Empfohlen für Web-Apps — tauscht einen kurzlebigen Code nach dem Login gegen Tokens.
     - Empfohlen
   * - ``client_credentials``
     - Machine-to-Machine — der Client authentifiziert sich mit seinem eigenen Secret, ohne Benutzer.
     - Empfohlen
   * - ``refresh_token``
     - Ermöglicht neue Access Tokens ohne erneute Anmeldung mittels eines langlebigen Refresh Tokens.
     - Empfohlen
   * - ``urn:ietf:params:oauth:grant-type:device_code``
     - Für eingabebeschränkte Geräte (TV, CLI) — der Benutzer autorisiert auf einem zweiten Gerät mit Browser.
     - Empfohlen
   * - ``urn:ietf:params:oauth:grant-type:jwt-bearer``
     - Tauscht ein signiertes JWT-Assertion gegen einen OAuth Access Token (Föderation / Service-Accounts).
     - Erweitert
   * - ``urn:ietf:params:oauth:grant-type:saml2-bearer``
     - Tauscht ein SAML-2.0-Assertion gegen einen OAuth Access Token.
     - Erweitert
   * - ``urn:ietf:params:oauth:grant-type:token-exchange``
     - Tauscht einen Token gegen einen anderen; unterstützt Impersonation und Delegation.
     - Erweitert
   * - ``urn:openid:params:grant-type:ciba``
     - Client-Initiated Backchannel Authentication — Authentifizierung über einen Out-of-Band-Kanal (z. B. Push-Benachrichtigung).
     - Erweitert
   * - ``implicit``
     - Veralteter Browser-Flow — Tokens werden direkt vom Authorization-Endpoint zurückgegeben. Deprecated, stattdessen Authorization Code + PKCE.
     - Deprecated
   * - ``hybrid``
     - Kombination aus Code und Implicit. Stattdessen Authorization Code + PKCE bevorzugen.
     - Deprecated
   * - ``password``
     - Resource Owner Password — der Benutzer gibt seine Credentials direkt an den Client. Für öffentliche Clients vermeiden.
     - Deprecated

.. note::

   Beim Erstellen eines Clients aus einem Template (``WebApplication``, ``ApiClient``,
   ``JavascriptClient``) werden die passenden Grant Types automatisch gesetzt. ``Allowed Grants``
   muss nur selten von Hand bearbeitet werden.

.. warning::

   Die Grants ``implicit``, ``hybrid`` und ``password`` gelten als veraltet. Moderne Clients
   sollten **Authorization Code mit PKCE** (für interaktive Anmeldungen) oder **Client Credentials**
   (für Machine-to-Machine) verwenden. Sie sind hier nur der Vollständigkeit halber aufgeführt und
   erhalten kein eigenes Flow-Diagramm.

Authorization Code + PKCE
-------------------------

Der Standard-Flow für Web-Anwendungen und Single-Page-Apps. Der Benutzer authentifiziert sich bei
**IdentityServerNET**, der Client erhält einen kurzlebigen ``code`` und tauscht ihn auf dem
Back-Channel gegen Tokens. **PKCE** (Proof Key for Code Exchange) bindet den Code an den Client, der
den Flow gestartet hat, und verhindert so das Abfangen des Codes.

Verwendet von den Templates ``WebApplication`` und ``JavascriptClient``. Aktivierung über den
Grant ``authorization_code`` und die Option ``RequirePkce``.

.. mermaid::

   sequenceDiagram
       participant B as Browser
       participant C as Client (Web-App)
       participant IS as IdentityServerNET

       B->>C: Geschützte Seite öffnen
       C->>B: Redirect zu /connect/authorize (+ code_challenge)
       B->>IS: GET /connect/authorize
       IS->>B: Login-Seite anzeigen
       B->>IS: Credentials senden
       IS->>B: Redirect zu redirect_uri (+ code)
       B->>C: code übermitteln
       C->>IS: POST /connect/token (code + code_verifier + secret)
       IS->>C: access_token, id_token, refresh_token
       C->>B: Session-Cookie setzen

Schritt 1 — Authorization Request (Browser):

.. code::

   GET https://localhost:44300/connect/authorize?
       client_id=is-webclient-test&
       response_type=code&
       scope=openid profile offline_access&
       redirect_uri=https://localhost:44360/signin-oidc&
       code_challenge=K2-ltc0...&
       code_challenge_method=S256&
       state=xyz&
       nonce=abc

Schritt 2 — Token Request (Back-Channel, nachdem der Code zurückkam):

.. code::

   POST https://localhost:44300/connect/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=authorization_code&
   client_id=is-webclient-test&
   client_secret=secret&
   code=8f1a...&
   code_verifier=dBjftJeZ...&
   redirect_uri=https://localhost:44360/signin-oidc

.. note::

   In einem ASP.NET-Core-Client führt die OIDC-Middleware beide Schritte automatisch aus — siehe
   :doc:`webapp`. Die PKCE-Parameter (``code_challenge`` / ``code_verifier``) werden für Sie
   erzeugt und validiert.

Pushed Authorization Request (PAR)
----------------------------------

Eine Härtung des Authorization-Code-Flows (RFC 9126). Statt alle Parameter in die Browser-URL zu
schreiben, schickt der Client sie zuerst per **POST** an den ``/connect/par``-Endpoint auf dem
Back-Channel und erhält eine kurzlebige ``request_uri``. Der Browser-Redirect trägt dann nur noch
``client_id`` und ``request_uri`` — weder ``scope``, ``code_challenge`` noch ``client_secret`` sind
jemals in der URL oder in Server-Logs sichtbar.

.. mermaid::

   sequenceDiagram
       participant B as Browser
       participant C as Client
       participant IS as IdentityServerNET

       C->>IS: POST /connect/par (alle Parameter + Secret)
       IS->>C: request_uri (läuft in 60s ab)
       C->>B: Redirect zu /connect/authorize?client_id=...&request_uri=...
       B->>IS: GET /connect/authorize (nur request_uri)
       IS->>B: Login-Seite anzeigen
       B->>IS: Credentials senden
       IS->>B: Redirect zu redirect_uri (+ code)
       B->>C: code übermitteln
       C->>IS: POST /connect/token
       IS->>C: Tokens

Schritt 1 — Parameter pushen (Back-Channel):

.. code::

   POST https://localhost:44300/connect/par
   Content-Type: application/x-www-form-urlencoded

   client_id=is-webclient-test&
   client_secret=secret&
   response_type=code&
   scope=openid profile&
   redirect_uri=https://localhost:44360/signin-oidc&
   code_challenge=K2-ltc0...&
   code_challenge_method=S256

Antwort:

.. code::

   HTTP/1.1 201 Created
   {
       "request_uri": "urn:ietf:params:oauth:request_uri:8f1a...",
       "expires_in": 60
   }

Schritt 2 — Der Browser-Redirect verwendet nur die ``request_uri``:

.. code::

   GET https://localhost:44300/connect/authorize?
       client_id=is-webclient-test&
       request_uri=urn:ietf:params:oauth:request_uri:8f1a...

PAR kann pro Client über die Option ``RequirePushedAuthorization`` erzwungen werden (siehe
:doc:`advanced-options`). Der Server veröffentlicht außerdem den
``pushed_authorization_request_endpoint`` in seinem Discovery-Dokument, sodass die ASP.NET-Core-
OIDC-Middleware (ab .NET 9) ihn automatisch verwendet. Zur serverseitigen Speicherung der
``request_uri`` siehe den Abschnitt ``Stores`` in :doc:`../getting-started/configuration`.

Client Credentials
------------------

Machine-to-Machine-Zugriff ohne Benutzer. Der Client authentifiziert sich mit seinem eigenen Secret
und erhält einen Access Token. Verwendet vom Template ``ApiClient``.

.. mermaid::

   sequenceDiagram
       participant C as Client (Service)
       participant IS as IdentityServerNET
       participant API as Web API

       C->>IS: POST /connect/token (client_id + secret)
       IS->>C: access_token
       C->>API: Request + Bearer access_token
       API->>C: Geschützte Ressource

.. code::

   POST https://localhost:44300/connect/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=client_credentials&
   client_id=my-api-commands&
   client_secret=secret&
   scope=my-api my-api.command

Siehe :doc:`api` für eine vollständige Anleitung inklusive der Absicherung der Ziel-Web-API.

Device Flow
-----------

Für eingabebeschränkte Geräte (Smart-TVs, CLI-Tools), die nicht einfach einen Browser anzeigen
können. Das Gerät fordert einen ``user_code`` an und bittet den Benutzer, ihn auf einem zweiten
Gerät (Smartphone, Laptop) einzugeben, während es den Token-Endpoint pollt, bis der Benutzer
autorisiert hat. Aktivierung über den Grant ``urn:ietf:params:oauth:grant-type:device_code``.

.. mermaid::

   sequenceDiagram
       participant D as Gerät (TV/CLI)
       participant IS as IdentityServerNET
       participant U as Smartphone des Benutzers

       D->>IS: POST /connect/deviceauthorization
       IS->>D: device_code, user_code, verification_uri
       D->>U: "Öffne URL, gib CODE ein" anzeigen
       U->>IS: verification_uri öffnen, anmelden, user_code eingeben
       loop bis autorisiert
           D->>IS: POST /connect/token (device_code)
           IS->>D: authorization_pending
       end
       IS->>D: access_token, refresh_token

Schritt 1 — Gerät fordert einen Code an:

.. code::

   POST https://localhost:44300/connect/deviceauthorization
   Content-Type: application/x-www-form-urlencoded

   client_id=device-client&scope=openid profile

Antwort:

.. code::

   {
       "device_code": "GmRh...",
       "user_code": "WDJB-MJHT",
       "verification_uri": "https://localhost:44300/device",
       "expires_in": 300,
       "interval": 5
   }

Schritt 2 — Gerät pollt den Token-Endpoint:

.. code::

   POST https://localhost:44300/connect/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=urn:ietf:params:oauth:grant-type:device_code&
   client_id=device-client&
   device_code=GmRh...

Refresh Token
-------------

Ermöglicht einem Client, einen neuen Access Token zu erhalten, wenn der alte abgelaufen ist, ohne
den Benutzer erneut durch den Login-Flow zu schicken. Erfordert die Option ``AllowOfflineAccess``
**und** den Scope ``offline_access`` im ursprünglichen Request. Der Client erhält neben dem Access
Token einen ``refresh_token`` und tauscht ihn später ein.

.. mermaid::

   sequenceDiagram
       participant C as Client
       participant IS as IdentityServerNET

       Note over C: access_token abgelaufen
       C->>IS: POST /connect/token (grant_type=refresh_token)
       IS->>C: neuer access_token (+ neuer refresh_token)

.. code::

   POST https://localhost:44300/connect/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=refresh_token&
   client_id=is-webclient-test&
   client_secret=secret&
   refresh_token=8xLOxBtZp8...

Lebensdauer und Rotationsverhalten von Refresh Tokens werden über die ``Advanced Properties``
gesteuert (``AbsoluteRefreshTokenLifetime``, ``SlidingRefreshTokenLifetime``, ``RefreshTokenUsage``,
``RefreshTokenExpiration``) — siehe :doc:`advanced-options`.
