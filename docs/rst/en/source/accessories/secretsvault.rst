Secrets Vault
=============

The **Secrets Vault** is used for centralized storage of **secrets** such as:

* Connection strings
* Passwords
* Client secrets

**Secrets** are assigned to **lockers**. An application, for example, is granted access to a **locker** and can retrieve the 
**secrets** it contains. Multiple **versions** can be created for each secret. If a 
*connection string* changes, for instance, a new version can be created for a **secret**. Once all **clients** have switched to the new version of the *connection string*, 
the old **version** can be deleted.

.. note::

    Ideally, one **locker** should be created per *client*. A **locker** should contain only the **secrets** relevant for the *client*.

.. note::

    When retrieving a **secret**, the **version** can also be omitted. However, in this case, it is important to remember that 
    the client will always access the most recently created version. When a new **version** is created, 
    the *client* will retrieve the new **version** upon the next request!

To manage the **Secrets Vault**, click on the appropriate tile on the *Admin page*.

Creating a Locker
-----------------

A new locker is created using the ``Create new locker`` form:

.. image:: img/secretsvault1.png

Creating a Secret
-----------------

To create a **secret** within a **locker**, open the ``Secrets`` menu in the corresponding locker and use 
the ``Create new secret`` form to add a new **secret**. Only the name and an optional description of the **secret** are required:

.. image:: img/secretsvault2.png

Creating Secret Versions
------------------------

To assign a value to a **secret**, **versions** must be created. To do this, select the **secret** from the list 
and go to the ``Versions`` menu:

.. image:: img/secretsvault3.png

The newly created version of the **secret** appears in the list. If further versions are created, the most recent version is always shown at the top.
Multiple versions are useful if, for example, a connection string changes due to a new database server. A new version can be created here, and clients can be gradually updated to use the new database. Old versions can then be deleted.

Clicking on the link displayed for the **version** opens a *JSON* in the browser:

.. image:: img/secretsvault4.png

.. note::

    The last part of the URL is the ``versionTimeStamp``. This can be omitted when retrieving a **secret version**. 
    This will return the most recently created version. 

    .. image:: img/secretsvault5.png

Retrieving a Secret
-------------------

**Secrets** can be retrieved using the method shown above (clicking the link). However, this link is only accessible to administrators with browser access.
If a non-administrator attempts to open this link in the browser, they will be redirected to the *login* page.

Clients can access secrets by passing a **Bearer Token**. The following steps are necessary to obtain a valid token:

Creating API Resources
++++++++++++++++++++++

*IdentityServerNET* provides an API for retrieving secrets (https://.../api/api/secretsvault?v=1.0&path={secret-version-path}).
To use this API, the necessary **API resources** must first be created. To do this, go to the **Resources (Identities & APIs)** 
section on the *Admin page*, then select **API Resources**. If not yet created, the ``secrets-vault`` API resource must be created here:

.. image:: img/secretsvault6.png

Under ``Scopes``, add a **scope** with the name of the **locker** to the resource:

.. image:: img/secretsvault7.png

Creating a Client
+++++++++++++++++

To allow a client to access the **locker**, go to the **Clients** section on the *Admin page* 
and create an **API client** there:

.. image:: img/secretsvault8.png

Under ``Client Secrets``, assign a **secret** that the client will later use to obtain a token.
Under ``Scopes``, add the **scopes** ``secrets-vault`` and ``secrets-vault.{locker-name}`` from the ``Add existing resource scope`` section:

.. image:: img/secretsvault9.png

.. note::

    ``Add existing resource scope`` only lists scopes the client may actually be granted: the base
    ``secrets-vault`` scope (needed by every Secrets Vault client) plus the locker scopes that belong to
    the client's own realm — a realm-scoped client never sees another realm's or the system's locker
    scopes here, and adding one via a crafted request is rejected server-side too.

Retrieving a Secret via HTTP Request
++++++++++++++++++++++++++++++++++++

.. note::

    The ``secrets-vault`` and ``signing-api`` audiences are validated against
    ``IdentityServer:PublicOrigin`` (see :doc:`../getting-started/configuration`). If this value is not
    configured, every Bearer token is rejected with ``401`` / ``invalid_token`` ("issuer ... is
    invalid"), regardless of how the client and scopes are set up.

First, obtain a valid **Bearer Token**:

.. code::

    POST https://localhost:44300/connect/token
    Content-Type: application/x-www-form-urlencoded

    grant_type=client_credentials&
    client_id=my-api-secrets&
    client_secret=secret&
    scope=secrets-vault secrets-vault.my-api-locker

If an **Access Token** is returned, it can be used to retrieve the **secret**:

.. code::

    GET https://localhost:44300/api/secretsvault?v=1.0&path=my-api-locker/db-connectionstring
    Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IkR...

.. note:: 

    In this example, no version was specified in the path. To retrieve a specific version, add it to the path, e.g., 
    https://localhost:44300/api/secretsvault?v=1.0&path=my-api-locker/db-connectionstring/{version}


Retrieving a Secret via IdentityServerNET.Clients
+++++++++++++++++++++++++++++++++++++++++++++++++

The **NuGet** package ``IdentityServerNET.Clients`` provides the following methods 
to access the **Secrets API**:

.. code:: bash

    dotnet add package IdentityServerNET.Clients

.. code:: csharp

    var secretsVaultClient = new IdentityServerNET.Clients.SecretsVaultClient("my-api-secrets", "secret");
    await secretsVaultClient.OpenLocker("https://localhost:44300", "my-api-locker");
    var secretResponse = await secretsVaultClient.GetSecret("db-connectionstring");

    Console.WriteLine(secretResponse.GetValue())

Realm Admins and the Secrets Vault
-----------------------------------

A :doc:`realm admin <../realms/managing-realms>` has their own, fully self-service Secrets Vault: the
``Secrets Vault`` tile on their admin homepage leads to a locker list scoped to their realm only. Lockers
they create are automatically namespaced ``{name}@{realm}`` (the admin types only the local name, exactly
like creating a client) — the system admin's own Secrets Vault list never shows a realm's lockers, and a
realm admin never sees another realm's or the system admin's lockers, even by guessing the URL.

Everything documented above works identically for a realm admin: creating secrets and versions, and
retrieving a value via the browser link (now scoped — a realm admin can open their own realm's secrets,
but not another realm's or the global ones, and the system admin cannot open a realm's secrets either).

.. note::

    Client-credentials (machine-to-machine) access is provisioned **automatically**: creating a locker
    adds its scope (``secrets-vault.my-locker@acme``) to the shared, global ``secrets-vault`` resource,
    and deleting the locker removes it again — no system admin involvement needed for day-to-day use.
    This only works once the ``secrets-vault`` resource itself exists; if the system admin hasn't
    created it yet (see *Creating API Resources* above), locker creation silently skips this step —
    browser-based retrieval is unaffected either way, and the scope can still be added manually later.

.. warning::

    Do **not** try to create your own ``secrets-vault`` API resource as a realm admin — creating an API
    resource is delegated, but ``secrets-vault`` (like ``signing-api``) is a reserved system resource
    name and this is rejected. Even if it weren't, it would silently not work: the retrieval endpoint
    validates tokens against the fixed audience ``secrets-vault``, and a realm-scoped resource would
    issue tokens audienced ``secrets-vault@{realm}`` instead, which never matches. Always ask the system
    administrator to add your locker's scope to the **existing, global** ``secrets-vault`` resource.








