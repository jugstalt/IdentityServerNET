JavaScript / SPA Client
=======================

A *JavaScript Client* is a **Single Page Application** (SPA) or static website that runs entirely in
the browser. Because such a client cannot keep a secret confidential, it is treated as a
**public client**: it authenticates users with the **Authorization Code flow secured by PKCE**, and
uses no client secret.

Creating a JavaScript Client
----------------------------

To create the client, assign a unique *Client Id* and select the ``JavascriptClient`` template.
Enter the URL of the SPA so the redirect and CORS entries can be pre-filled.

The ``JavascriptClient`` template configures the client as follows:

.. list-table::
   :widths: 40 60
   :header-rows: 1

   * - Setting
     - Value
   * - ``AllowedGrantTypes``
     - ``authorization_code``
   * - ``RequirePkce``
     - ``true``
   * - ``RequireClientSecret``
     - ``false``
   * - ``AllowedScopes``
     - ``openid``, ``profile`` (plus any API scopes you add)
   * - ``RedirectUris``
     - ``{clientUrl}/callback.html``
   * - ``PostLogoutRedirectUris``
     - ``{clientUrl}/index.html``
   * - ``AllowedCorsOrigins``
     - ``{clientUrl}``

.. note::

   ``AllowedCorsOrigins`` is essential for a browser-based client: the SPA calls the token and
   userinfo endpoints directly via ``fetch``/``XMLHttpRequest``, so its origin must be allowed for
   CORS. See :doc:`advanced-options`.

Flow
----

A JavaScript client uses **Authorization Code + PKCE**. Since there is no client secret, PKCE is
what protects the authorization code against interception — it is therefore mandatory
(``RequirePkce = true``). The full sequence diagram and the corresponding HTTP requests are
described in :doc:`grants-and-flows`.

.. warning::

   Do not enable ``AllowAccessTokensViaBrowser`` / the ``implicit`` grant for new SPAs. The implicit
   flow is deprecated; Authorization Code with PKCE is the current recommendation for browser-based
   clients.

Client Library
--------------

Use a standards-compliant OIDC library in the browser (for example ``oidc-client-ts``). Point it at
the authority ``https://localhost:44300``, set ``response_type=code``, and configure the same
``redirect_uri`` (``callback.html``) that is registered on the client. The library performs the
PKCE handshake and token exchange for you.
