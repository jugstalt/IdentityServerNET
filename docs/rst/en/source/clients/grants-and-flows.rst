Grant Types and Flows
=====================

Every client declares one or more **grant types** under ``Allowed Grants``. A grant type
determines *how* a client obtains tokens from **IdentityServerNET** — which endpoints are
involved, whether a user is present, and how credentials are exchanged.

This page explains the available grant types and walks through the recommended flows with
sequence diagrams and example HTTP requests. All examples use the authority
``https://localhost:44300`` and the demo client ``is-webclient-test``.

Grant Types Overview
--------------------

The following grant types can be enabled under ``Allowed Grants``. The descriptions match the
ones shown in the Admin UI:

.. list-table::
   :widths: 30 55 15
   :header-rows: 1

   * - Grant
     - Description
     - Status
   * - ``authorization_code``
     - Recommended for web apps — exchanges a short-lived code for tokens after user login.
     - Recommended
   * - ``client_credentials``
     - Machine-to-machine — client authenticates with its own secret, no user involved.
     - Recommended
   * - ``refresh_token``
     - Allows clients to obtain new access tokens without re-authentication using a long-lived refresh token.
     - Recommended
   * - ``urn:ietf:params:oauth:grant-type:device_code``
     - For input-constrained devices (TV, CLI) — user authorizes on a secondary device with a browser.
     - Recommended
   * - ``urn:ietf:params:oauth:grant-type:jwt-bearer``
     - Exchange a signed JWT assertion for an OAuth access token (federation / service accounts).
     - Advanced
   * - ``urn:ietf:params:oauth:grant-type:saml2-bearer``
     - Exchange a SAML 2.0 assertion for an OAuth access token.
     - Advanced
   * - ``urn:ietf:params:oauth:grant-type:token-exchange``
     - Exchange one token for another; supports impersonation and delegation scenarios.
     - Advanced
   * - ``urn:openid:params:grant-type:ciba``
     - Client-Initiated Backchannel Authentication — authenticate the user via an out-of-band channel (e.g. push notification).
     - Advanced
   * - ``implicit``
     - Legacy browser flow — tokens returned directly from the authorization endpoint. Deprecated, prefer Authorization Code + PKCE.
     - Deprecated
   * - ``hybrid``
     - Combination of code and implicit. Prefer Authorization Code + PKCE.
     - Deprecated
   * - ``password``
     - Resource Owner Password — user provides credentials directly to the client. Avoid for public clients.
     - Deprecated

.. note::

   When you create a client from a template (``WebApplication``, ``ApiClient``,
   ``JavascriptClient``), the appropriate grant types are set automatically. You rarely need to
   edit ``Allowed Grants`` by hand.

.. warning::

   The ``implicit``, ``hybrid`` and ``password`` grants are considered legacy. Modern clients
   should use **Authorization Code with PKCE** (for interactive logins) or **Client Credentials**
   (for machine-to-machine). They are documented here only for completeness and receive no
   dedicated flow diagram.

Authorization Code + PKCE
-------------------------

The standard flow for web applications and single-page apps. The user authenticates at
**IdentityServerNET**, the client receives a short-lived ``code``, and exchanges it for tokens on
the back channel. **PKCE** (Proof Key for Code Exchange) binds the code to the client that
started the flow, preventing code interception attacks.

Used by the ``WebApplication`` and ``JavascriptClient`` templates. Enable via the
``authorization_code`` grant and the ``RequirePkce`` option.

.. mermaid::

   sequenceDiagram
       participant B as Browser
       participant C as Client (web app)
       participant IS as IdentityServerNET

       B->>C: Open protected page
       C->>B: Redirect to /connect/authorize (+ code_challenge)
       B->>IS: GET /connect/authorize
       IS->>B: Show login page
       B->>IS: Submit credentials
       IS->>B: Redirect to redirect_uri (+ code)
       B->>C: Deliver code
       C->>IS: POST /connect/token (code + code_verifier + secret)
       IS->>C: access_token, id_token, refresh_token
       C->>B: Set session cookie

Step 1 — Authorization request (browser):

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

Step 2 — Token request (back channel, after the code is returned):

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

   In an ASP.NET Core client the OIDC middleware performs both steps automatically — see
   :doc:`webapp`. PKCE parameters (``code_challenge`` / ``code_verifier``) are generated and
   validated for you.

Pushed Authorization Request (PAR)
----------------------------------

A hardening of the Authorization Code flow (RFC 9126). Instead of putting all parameters into the
browser URL, the client first **POSTs** them to the ``/connect/par`` endpoint on the back channel
and receives a short-lived ``request_uri``. The browser redirect then only carries ``client_id``
and ``request_uri`` — no ``scope``, ``code_challenge`` or ``client_secret`` is ever visible in the
URL or server logs.

.. mermaid::

   sequenceDiagram
       participant B as Browser
       participant C as Client
       participant IS as IdentityServerNET

       C->>IS: POST /connect/par (all params + secret)
       IS->>C: request_uri (expires in 60s)
       C->>B: Redirect to /connect/authorize?client_id=...&request_uri=...
       B->>IS: GET /connect/authorize (request_uri only)
       IS->>B: Show login page
       B->>IS: Submit credentials
       IS->>B: Redirect to redirect_uri (+ code)
       B->>C: Deliver code
       C->>IS: POST /connect/token
       IS->>C: tokens

Step 1 — Push the parameters (back channel):

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

Response:

.. code::

   HTTP/1.1 201 Created
   {
       "request_uri": "urn:ietf:params:oauth:request_uri:8f1a...",
       "expires_in": 60
   }

Step 2 — Browser redirect uses only the ``request_uri``:

.. code::

   GET https://localhost:44300/connect/authorize?
       client_id=is-webclient-test&
       request_uri=urn:ietf:params:oauth:request_uri:8f1a...

PAR can be enforced per client via the ``RequirePushedAuthorization`` option (see
:doc:`advanced-options`). The server also advertises the ``pushed_authorization_request_endpoint``
in its discovery document so the ASP.NET Core OIDC middleware (.NET 9+) uses it automatically. See
the ``Stores`` section in :doc:`../getting-started/configuration` for server-side storage of the
``request_uri``.

Client Credentials
------------------

Machine-to-machine access with no user involved. The client authenticates with its own secret and
receives an access token. Used by the ``ApiClient`` template.

.. mermaid::

   sequenceDiagram
       participant C as Client (service)
       participant IS as IdentityServerNET
       participant API as Web API

       C->>IS: POST /connect/token (client_id + secret)
       IS->>C: access_token
       C->>API: Request + Bearer access_token
       API->>C: Protected resource

.. code::

   POST https://localhost:44300/connect/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=client_credentials&
   client_id=my-api-commands&
   client_secret=secret&
   scope=my-api my-api.command

See :doc:`api` for a full walk-through including how to secure the target Web API.

Device Flow
-----------

For input-constrained devices (smart TVs, CLI tools) that cannot easily show a browser. The device
requests a ``user_code`` and asks the user to enter it on a second device (phone, laptop), while it
polls the token endpoint until the user has authorized. Enable via the
``urn:ietf:params:oauth:grant-type:device_code`` grant.

.. mermaid::

   sequenceDiagram
       participant D as Device (TV/CLI)
       participant IS as IdentityServerNET
       participant U as User's phone

       D->>IS: POST /connect/deviceauthorization
       IS->>D: device_code, user_code, verification_uri
       D->>U: Show "go to URL, enter CODE"
       U->>IS: Open verification_uri, log in, enter user_code
       loop until authorized
           D->>IS: POST /connect/token (device_code)
           IS->>D: authorization_pending
       end
       IS->>D: access_token, refresh_token

Step 1 — Device requests a code:

.. code::

   POST https://localhost:44300/connect/deviceauthorization
   Content-Type: application/x-www-form-urlencoded

   client_id=device-client&scope=openid profile

Response:

.. code::

   {
       "device_code": "GmRh...",
       "user_code": "WDJB-MJHT",
       "verification_uri": "https://localhost:44300/device",
       "expires_in": 300,
       "interval": 5
   }

Step 2 — Device polls the token endpoint:

.. code::

   POST https://localhost:44300/connect/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=urn:ietf:params:oauth:grant-type:device_code&
   client_id=device-client&
   device_code=GmRh...

Refresh Token
-------------

Lets a client obtain a fresh access token when the old one expires, without sending the user
through the login flow again. Requires the ``AllowOfflineAccess`` option **and** the
``offline_access`` scope in the original request. The client receives a ``refresh_token`` alongside
the access token and exchanges it later.

.. mermaid::

   sequenceDiagram
       participant C as Client
       participant IS as IdentityServerNET

       Note over C: access_token expired
       C->>IS: POST /connect/token (grant_type=refresh_token)
       IS->>C: new access_token (+ new refresh_token)

.. code::

   POST https://localhost:44300/connect/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=refresh_token&
   client_id=is-webclient-test&
   client_secret=secret&
   refresh_token=8xLOxBtZp8...

The lifetime and rotation behaviour of refresh tokens is controlled by the ``Advanced Properties``
(``AbsoluteRefreshTokenLifetime``, ``SlidingRefreshTokenLifetime``, ``RefreshTokenUsage``,
``RefreshTokenExpiration``) — see :doc:`advanced-options`.
