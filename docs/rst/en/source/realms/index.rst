Realms (Multi-Tenancy)
=======================

A **Realm** turns a single **IdentityServerNET** instance into a multi-tenant server. Each realm is an
isolated tenant with its own administrator, its own clients, roles and resources — while still sharing
one installation, one database and one set of global identity/API resources.

Tenant membership is **domain-based**: a realm owns one or more e-mail domains (e.g. ``acme.com``), and
any user whose e-mail address ends in one of those domains is automatically considered a member of that
realm. There is no separate "assign user to realm" step and no suffix added to the user's own account —
membership is derived purely from the e-mail domain.

Realm-scoped entities — clients, roles and identity/API resources — do carry a namespace suffix,
``{name}@{realm}`` (e.g. a client ``my-app@acme``). This keeps a realm's clients and roles clearly
separated from the global ones and from other realms', while a global entity (no ``@realm`` suffix)
remains usable, by default, only by users who do **not** belong to any realm — see
:doc:`cross-realm-access` for how a client can opt into accepting other realms' users as well.

.. note::

    Realms are an **optional** feature. An installation that never creates a realm behaves exactly like
    a classic single-tenant **IdentityServerNET** server.

.. toctree::
   :maxdepth: 2
   :caption: Contents:

   managing-realms
   appearance
   cross-realm-access
