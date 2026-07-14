Creating and Administering Realms
==================================

Creating a realm — as well as removing one — is a **system-level** capability. It requires the
``identityserver-realm-administrator`` role, which (unlike the other admin roles) is never delegated to
a realm admin: a realm admin manages its own realm, but cannot create further realms.

Creating a Realm
-----------------

When logged in as a system administrator with the realm-administrator role, a ``Realms...`` tile appears
on the admin homepage. It leads to the realm list and a **Create new realm** form:

* **Realm name** — the lowercase slug used as the ``@realm`` suffix (letters, digits and ``-`` only, e.g.
  ``acme``).
* **Primary domain** — the e-mail domain that anchors the realm admin account (e.g. ``acme.com``).
* **Additional domains** — further e-mail domains that belong to this realm, comma- or space-separated.
  Can also be left empty and added later (see the **Domains** management page below).

.. note::

    A domain can belong to at most **one** realm at a time. Creating a realm with a domain that is
    already assigned elsewhere is rejected.

On creation, **IdentityServerNET** automatically provisions:

* A **realm admin account**, ``admin@{PrimaryDomain}`` (e.g. ``admin@acme.com``), with a randomly
  generated password.
* Five **delegated admin roles**, scoped to the new realm (``role@realm``, e.g.
  ``identityserver-user-administrator@acme``), assigned to that admin account:

  .. list-table::
     :widths: 35 65
     :header-rows: 1

     * - Delegated role
       - Grants (within the realm only)
     * - User Administrator
       - Manage user accounts whose e-mail belongs to one of the realm's domains.
     * - Role Administrator
       - Manage custom roles scoped to the realm.
     * - Resource Administrator
       - Manage identity/API resources scoped to the realm.
     * - Client Administrator
       - Manage clients scoped to the realm.
     * - Secrets Vault Administrator
       - Manage the realm's own lockers/secrets in the :doc:`Secrets Vault <../accessories/secretsvault>`.

  Realm administration itself and the **Payload Signing** tool remain system-level and are **not**
  delegated to realm admins.

.. note::

    Realms created **before** Secrets Vault delegation was introduced do not retroactively get the
    Secrets Vault Administrator role. Grant it manually once, via the realm admin's ``User Roles`` page,
    by assigning the role ``identityserver-secretsvault-administrator@{realm}``.

.. important::

    The generated admin password is shown **exactly once**, directly after creation, in the status
    message on the realm list page. Store it immediately — it cannot be retrieved again (only reset via
    the regular ``Set Password`` admin function).

    It is also a **one-time password**: the first time ``admin@{PrimaryDomain}`` signs in, they are
    immediately forced to choose a new password before they can access anything else. See
    :doc:`One-time Passwords <../getting-started/admin-server>` for details on this mechanism.

Realm Admin — Scoped Administration
------------------------------------

Once signed in as ``admin@acme.com``, the realm admin sees the same admin tiles as a regular
administrator (Users, Roles, Resources, Clients, Appearance) — but every list and edit page is
transparently filtered to that realm:

* **Users** only shows/creates accounts whose e-mail belongs to the realm's domains.
* **Roles**, **Resources** and **Clients** the realm admin creates are automatically namespaced
  ``{name}@acme`` — the admin never has to type the suffix; it is applied on write, and stripped again
  for display.
* Attempting to load another realm's (or the global) entity by guessing its id is rejected — the same
  ownership check runs on both the read and the write side.

Managing an Existing Realm
----------------------------

The realm list ("Realms...") leads to per-realm management pages:

**General**
    The realm ``Name`` and ``Primary domain`` are fixed after creation. Only the ``Display name``
    (a human-readable label) can be changed here.

**Domains**
    A textarea listing all e-mail domains that belong to the realm, one per line. The primary domain is
    always included automatically and cannot be removed. Add further domains here to extend which users
    are considered realm members — for example if the tenant later acquires an additional company domain.

**Users**
    A read-only list of all user accounts whose e-mail domain matches one of the realm's domains, linking
    to the standard ``Edit User`` page for each.

**Delete**
    Permanently deprovisions the realm. This removes:

    * All realm-scoped **clients**
    * All realm-scoped **roles**
    * All realm-scoped **resources**
    * The realm **admin account**

    .. note::

        End-user accounts in the realm's domains are **retained** — deleting a realm does not delete its
        members, only the tenant's administrative scaffolding (clients/roles/resources/admin).

    As a safety check, the realm name must be typed again to confirm deletion.

Naming Convention Reference
------------------------------

The realm namespace convention is applied consistently to every realm-scoped identifier:

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Identifier
     - Example
   * - Global client/role/resource
     - ``my-app`` (no suffix — usable outside any realm)
   * - Realm-scoped client/role/resource
     - ``my-app@acme``
   * - Realm admin account
     - ``admin@acme.com`` (an e-mail address — **not** namespaced; user membership is always
       domain-derived)

.. note::

    Standard OIDC scopes and identity resources (``openid``, ``profile``, ``email``, ``address``,
    ``phone``, ``offline_access``, ``roles``) are global by definition and are never namespaced, even
    inside a realm's client configuration.
