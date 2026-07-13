Allowing Cross-Realm and Public Access to a Client
=====================================================

By default, a realm-scoped client (``my-app@acme``) can only be used by users who belong to that same
realm — that is, whose e-mail domain is one of the realm's :doc:`domains <managing-realms>`. Attempting
to sign in with a user from a different realm, or with no realm at all, is rejected at the very first
step (before a password is even requested), so no username is leaked (no user enumeration).

Sometimes this default is too strict:

* A realm admin builds a client meant to be used by an external partner's users as well.
* A client should be open to **anyone**, regardless of realm — for example a public-facing app that
  merely happens to have been created inside a realm's client list.

Both cases are configured on the client itself, without changing anything about the realm.

Configuring ``AllowedUserDomains``
------------------------------------

Open the client in the Admin UI and go to **Advanced Collections** (see
:doc:`../clients/advanced-options`). The ``AllowedUserDomains`` collection accepts one e-mail domain per
line, in addition to the client's own realm:

.. list-table::
   :widths: 35 65
   :header-rows: 1

   * - ``AllowedUserDomains`` value
     - Effect
   * - *(empty, default)*
     - Only users of the client's own realm may sign in.
   * - ``partner.com``
     - Users of the client's realm **and** users whose e-mail ends in ``partner.com`` may sign in.
   * - ``partner.com``, ``other.org``
     - Multiple external domains can be listed, one per line.
   * - ``*``
     - The client accepts **any** user, regardless of realm — effectively a public client.

.. note::

    ``AllowedUserDomains`` only has an effect on **realm-scoped** clients. Global clients (no ``@realm``
    suffix) already accept every user by default and are unaffected by this setting.

Where the Check Is Enforced
------------------------------

The domain check runs consistently across every sign-in path, so opening (or restricting) access takes
effect everywhere at once:

* The **identifier step** of the standard login (rejects a disallowed domain before a password is
  requested).
* **Password** sign-in (defense in depth, in case the identifier step was bypassed).
* **Passkey** sign-in, both as a first factor (passwordless) and as a second factor.
* **External / third-party** logins (e.g. an OIDC or SAML identity provider) — a foreign user who is
  otherwise valid but not allowed for this client receives an ``access_denied`` error, and no application
  session cookie is issued.
* **Token issuance and refresh** — the same rule is re-checked whenever a token is validated or renewed,
  so removing a domain (or the wildcard) from ``AllowedUserDomains`` takes effect on the user's *next*
  token refresh, not just on their next interactive login.

.. important::

    Security implication of ``*``: a realm admin can configure this on their **own** clients without
    system-administrator involvement. A wildcard effectively turns that one client into an open,
    cross-tenant login surface — any user of the entire installation can obtain tokens for it. Consider
    also enabling ``RequireConsent`` (see :doc:`../clients/advanced-options`) on such a client, so users
    from outside the realm see explicitly what they are authorizing before continuing.
