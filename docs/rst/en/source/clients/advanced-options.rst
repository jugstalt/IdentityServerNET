Client Reference (Advanced Options)
===================================

When editing a client, its properties are organized into menu items. This page is a reference for
the three "advanced" sections — **Advanced Options**, **Advanced Collections** and
**Advanced Properties**. Most values come pre-configured from the client template and rarely need
to be changed.

The other menu items (``Name``, ``Client Secrets``, ``Allowed Grants``, ``Allowed Scopes``) are
covered in :doc:`webapp`, :doc:`api` and :doc:`grants-and-flows`.

Advanced Options
----------------

These are on/off switches. Each description matches the text shown in the Admin UI.

**Authentication & security**

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Option
     - Description
   * - ``RequireClientSecret``
     - Requires a client secret for token endpoint requests.
   * - ``RequirePkce``
     - Enforces Proof Key for Code Exchange (PKCE) for authorization code flows.
   * - ``AllowPlainTextPkce``
     - Permits the less secure plain text code challenge method for PKCE.
   * - ``RequirePushedAuthorization``
     - Requires authorization requests to use Pushed Authorization Requests (PAR).
   * - ``AllowAccessTokensViaBrowser``
     - Allows access tokens to be returned in browser URL fragments (implicit flow).
   * - ``EnableLocalLogin``
     - Allows users to log in with a local username and password.
   * - ``Enabled``
     - Enables or disables this client entirely.

**Consent**

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Option
     - Description
   * - ``RequireConsent``
     - Displays the consent screen to users before authorizing the client.
   * - ``AllowRememberConsent``
     - Lets users save their consent decision to skip the consent screen next time.

**Tokens & claims**

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Option
     - Description
   * - ``AllowOfflineAccess``
     - Allows clients to request refresh tokens for offline access.
   * - ``UpdateAccessTokenClaimsOnRefresh``
     - Re-issues updated access token claims when a refresh token is used.
   * - ``IncludeJwtId``
     - Adds a unique jti claim to each JWT access token for tracking.
   * - ``AlwaysSendClientClaims``
     - Always includes client claims in the token, even without user interaction.
   * - ``AlwaysIncludeUserClaimsInIdToken``
     - Includes all requested user claims directly in the identity token.

**Logout**

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Option
     - Description
   * - ``FrontChannelLogoutSessionRequired``
     - Includes the session ID in front-channel logout iframe requests.
   * - ``BackChannelLogoutSessionRequired``
     - Sends the session ID in back-channel logout notifications.

Advanced Collections
--------------------

List-based values — enter one entry per line. If a request later arrives with a value not present
in the relevant list, **IdentityServerNET** rejects it.

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Collection
     - Purpose
   * - ``RedirectUris``
     - Allowed URIs to return to after a successful login (authorization code / implicit). Required for interactive clients.
   * - ``PostLogoutRedirectUris``
     - Allowed URIs to return to after logout.
   * - ``AllowedCorsOrigins``
     - Browser origins allowed to call IdentityServerNET endpoints via CORS. Relevant for SPA / JavaScript clients.
   * - ``IdentityProviderRestrictions``
     - Restricts which external identity providers this client may use. Empty means all configured providers are allowed.
   * - ``AllowedUserDomains``
     - Realm-scoped clients only. Additional user e-mail domains (beyond the client's own realm) allowed
       to sign in; ``*`` allows every user. See :doc:`../realms/cross-realm-access`.

.. note::

   The single-value logout URIs ``FrontChannelLogoutUri`` and ``BackChannelLogoutUri`` are edited
   alongside the collections. They point to the client endpoint that IdentityServerNET calls to
   propagate a logout.

Advanced Properties
-------------------

Numeric lifetimes and token behaviour. Defaults are shown in seconds and generally do not need to
be changed.

.. list-table::
   :widths: 35 20 45
   :header-rows: 1

   * - Property
     - Default
     - Meaning
   * - ``IdentityTokenLifetime``
     - 300
     - Lifetime of the identity token (5 minutes).
   * - ``AccessTokenLifetime``
     - 3600
     - Lifetime of the access token (1 hour).
   * - ``AuthorizationCodeLifetime``
     - 300
     - Lifetime of the authorization code (5 minutes).
   * - ``AbsoluteRefreshTokenLifetime``
     - 2592000
     - Maximum lifetime of a refresh token (30 days).
   * - ``SlidingRefreshTokenLifetime``
     - 1296000
     - Sliding window a refresh token is extended by on use (15 days).
   * - ``ConsentLifetime``
     - null
     - Lifetime of stored user consent. ``null`` means it never expires.
   * - ``DeviceCodeLifetime``
     - 300
     - Lifetime of a device code in the device flow (5 minutes).
   * - ``UserSsoLifetime``
     - null
     - Maximum age of a user's single-sign-on session for this client. ``null`` means unlimited.

Token behaviour (enumerations):

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - Property
     - Values
   * - ``RefreshTokenUsage``
     - ``OneTime`` (rotate the refresh token on every use — recommended) or ``Reuse`` (keep the same token).
   * - ``RefreshTokenExpiration``
     - ``Sliding`` (extends on use, up to the absolute lifetime) or ``Absolute`` (fixed expiry).
   * - ``AccessTokenType``
     - ``Jwt`` (self-contained JSON Web Token) or ``Reference`` (opaque token validated via introspection).

Other properties:

* ``ClientClaimsPrefix`` — prefix applied to client claims in the token (default ``client_``).
* ``PairWiseSubjectSalt`` — salt used to generate pairwise subject identifiers.
* ``UserCodeType`` — the user-code format used for the device flow.
