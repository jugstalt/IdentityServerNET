Installation with Docker Compose
=================================

**Docker Compose** is the quickest way to run *IdentityServerNET* in your own environment.
All settings are managed in a single ``.env`` file and can be copied directly into other
projects.

The required files are located in the repository under
`dist/docker-compose/ <https://github.com/jugstalt/identityserver.net/tree/master/dist/docker-compose>`_.

Quick start
-----------

.. code:: bash

    git clone https://github.com/jugstalt/identityserver.net.git
    cd identityserver.net/dist/docker-compose
    docker compose up -d

The application will then be available at http://localhost:8080.

Files
-----

``docker-compose.yml``
~~~~~~~~~~~~~~~~~~~~~~

.. code:: yaml

    services:
      identityserver:
        image: gstalt/identityserver-net:latest
        restart: unless-stopped
        ports:
          - "${HOST_PORT:-8080}:8080"
        env_file:
          - .env
        environment:
          ASPNETCORE_URLS: "http://*:8080"
        volumes:
          - identityserver-data:/home/app

    volumes:
      identityserver-data:

The volume is mounted at ``/home/app`` (not ``/home/app/identityserver-net``) so that
Docker correctly inherits the file ownership from the image (``app`` user).

``.env``
~~~~~~~~

All settings live in the ``.env`` file. Variable names use ``__`` as the separator for
hierarchical ASP.NET Core configuration values.

Essential settings
------------------

.. list-table::
   :widths: 45 55
   :header-rows: 1

   * - Variable
     - Description
   * - ``HOST_PORT``
     - External port on the host (default: ``8080``)
   * - ``IDENTITYSERVER__PUBLICORIGIN``
     - Public URL of the server, e.g. ``https://auth.example.com``.
       Must match the URL clients use to reach the server.
   * - ``IDENTITYSERVER__APPLICATIONTITLE``
     - Title shown in the browser tab and on the login page
   * - ``IDENTITYSERVER__SIGNINGCREDENTIAL__CERTPASSWORD``
     - Password for the auto-generated signing certificate. Change this!

Database / Storage
------------------

**LiteDB** is used by default (file-based, no extra service required).
SQL Server, PostgreSQL, SQLite, and MongoDB are also available:

.. list-table::
   :widths: 45 55
   :header-rows: 1

   * - Variable
     - Example value
   * - ``IDENTITYSERVER__CONNECTIONSTRINGS__LITEDB``
     - ``/home/app/identityserver-net/is_identityserver-net.db``
   * - ``IDENTITYSERVER__CONNECTIONSTRINGS__SQLSERVER``
     - ``Server=db;Database=IdentityServerNET;User Id=sa;Password=...``
   * - ``IDENTITYSERVER__CONNECTIONSTRINGS__POSTGRES``
     - ``Host=db;Database=identityservernet;Username=postgres;Password=...``
   * - ``IDENTITYSERVER__CONNECTIONSTRINGS__SQLITE``
     - ``Data Source=/home/app/identityserver-net/is.sqlite``
   * - ``IDENTITYSERVER__CONNECTIONSTRINGS__MONGODB``
     - ``mongodb://mongo:27017``

Only **one** connection string may be set at a time.

Passkey (WebAuthn / FIDO2)
--------------------------

Passkeys enable passwordless authentication using biometrics or hardware security keys.

.. list-table::
   :widths: 55 45
   :header-rows: 1

   * - Variable
     - Description
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__ALLOWPASSWORDLESS``
     - ``true`` – enable passwordless first-factor sign-in via passkey
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__ALLOWSECONDFACTOR``
     - ``true`` – enable passkey as a second factor after username/password
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__SERVERDOMAIN``
     - WebAuthn Relying Party ID, e.g. ``example.com``. Leave empty to inherit from the request host.
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__RELYINGPARTYNAME``
     - Human-readable name shown in the authenticator during registration

Migrations (Seeding)
--------------------

.. note::

    Migrations only run when ``ASPNETCORE_ENVIRONMENT=Development`` is set.
    They create missing entries on first start and never overwrite existing data.
    After the first successful start, ``ASPNETCORE_ENVIRONMENT`` can be removed — or
    left in place, since subsequent runs will not modify anything that already exists.

Migrations are defined as environment variables in the ``.env`` file:

.. code:: bash

    ASPNETCORE_ENVIRONMENT=Development
    IDENTITYSERVER__MIGRATIONS__ADMINPASSWORD=Admin1234!

    # Identity Resources
    IDENTITYSERVER__MIGRATIONS__IDENTITYRESOURCES__0__NAME=openid
    IDENTITYSERVER__MIGRATIONS__IDENTITYRESOURCES__1__NAME=profile
    IDENTITYSERVER__MIGRATIONS__IDENTITYRESOURCES__2__NAME=role

    # API Resource with scopes
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__NAME=my-api
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__APISECRET=apisecret
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__SCOPES__0__NAME=read
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__SCOPES__1__NAME=write

    # Roles
    IDENTITYSERVER__MIGRATIONS__ROLES__0__NAME=admin
    IDENTITYSERVER__MIGRATIONS__ROLES__1__NAME=user

    # Users
    IDENTITYSERVER__MIGRATIONS__USERS__0__NAME=alice@example.com
    IDENTITYSERVER__MIGRATIONS__USERS__0__PASSWORD=Alice1234!
    IDENTITYSERVER__MIGRATIONS__USERS__0__ROLES__0=admin

    # Web Application Client (Authorization Code + PKCE)
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTTYPE=WebApplication
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTID=my-webapp
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTSECRET=secret
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTURL=https://myapp.example.com
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__SCOPES__0=openid
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__SCOPES__1=profile
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__SCOPES__2=role

    # API Client (Client Credentials)
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__CLIENTTYPE=ApiClient
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__CLIENTID=my-service
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__CLIENTSECRET=secret
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__SCOPES__0=my-api
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__SCOPES__1=my-api.read

    # JavaScript / SPA Client (no secret)
    IDENTITYSERVER__MIGRATIONS__CLIENTS__2__CLIENTTYPE=JavascriptClient
    IDENTITYSERVER__MIGRATIONS__CLIENTS__2__CLIENTID=my-spa
    IDENTITYSERVER__MIGRATIONS__CLIENTS__2__CLIENTURL=https://spa.example.com
    IDENTITYSERVER__MIGRATIONS__CLIENTS__2__SCOPES__0=openid
    IDENTITYSERVER__MIGRATIONS__CLIENTS__2__SCOPES__1=profile

Behind a reverse proxy
----------------------

When *IdentityServerNET* runs behind nginx, Traefik, or another TLS-terminating proxy,
set the following variables:

.. code:: bash

    IDENTITYSERVER__PUBLICORIGIN=https://auth.example.com
    IDENTITYSERVER__CONFIGURE__USEHTTPSREDIRECTION=false
    IDENTITYSERVER__CONFIGURE__ADDXFORWARDEDPROTOMIDDLEWARE=true
