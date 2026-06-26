Installation with Kubernetes
============================

The Kubernetes manifests are located in the repository under
`dist/kubernetes/ <https://github.com/jugstalt/identityserver.net/tree/master/dist/kubernetes>`_.

Files overview
--------------

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - File
     - Contents
   * - ``secret.yaml``
     - Passwords and credentials (certificate password, admin password, DB connection strings, mail)
   * - ``configmap.yaml``
     - All non-sensitive settings plus commented-out migration examples
   * - ``pvc.yaml``
     - PersistentVolumeClaim for data storage (database, signing keys, data-protection keys)
   * - ``deployment.yaml``
     - Deployment with security context, liveness/readiness probes, and resource limits
   * - ``service.yaml``
     - ClusterIP Service that exposes container port 8080 internally on port 80
   * - ``ingress.yaml``
     - Ingress template (nginx, optional TLS via cert-manager)

Quick start
-----------

.. code:: bash

    git clone https://github.com/jugstalt/identityserver.net.git
    cd identityserver.net/dist/kubernetes

    # 1. Edit secret.yaml and configmap.yaml to match your environment
    # 2. Apply all manifests
    kubectl apply -f .

The OIDC discovery endpoint will be available at:
``http://<ingress-host>/.well-known/openid-configuration``

Namespace
---------

All manifests default to ``namespace: default``. For a dedicated namespace, create it
first and update the ``namespace:`` field in every file:

.. code:: bash

    kubectl create namespace identity
    # then set namespace: identity in all .yaml files

Alternatively, use ``kubectl apply -n identity -f .`` and omit the namespace from the
files — but keeping it explicit in the files is the safer approach for production.

Secrets (``secret.yaml``)
--------------------------

Sensitive values are stored in a Kubernetes Secret. The file uses ``stringData`` so
values can be written as plain text — Kubernetes base64-encodes them automatically:

.. code:: yaml

    apiVersion: v1
    kind: Secret
    metadata:
      name: identityserver-secret
      namespace: default
    type: Opaque
    stringData:
      IDENTITYSERVER__SIGNINGCREDENTIAL__CERTPASSWORD: "Change-This-Password!"
      IDENTITYSERVER__MIGRATIONS__ADMINPASSWORD: "Admin1234!"

      # Database — uncomment exactly one:
      # IDENTITYSERVER__CONNECTIONSTRINGS__SQLSERVER: "Server=db;..."
      # IDENTITYSERVER__CONNECTIONSTRINGS__POSTGRES: "Host=db;..."
      # IDENTITYSERVER__CONNECTIONSTRINGS__MONGODB: "mongodb://mongo:27017"

      # Mail sender — uncomment the one you use:
      # IDENTITYSERVER__MAIL__SMTP__PASSWORD: "smtp-password"
      # IDENTITYSERVER__MAIL__SENDGRID__APIKEY: "SG.xxxxxxxxxxxx"

      # External identity provider:
      # IDENTITYSERVER__EXTERNAL__MICROSOFTIDENTITYWEB__CLIENTSECRET: "..."

.. warning::

    Never commit ``secret.yaml`` with real credentials to a public repository.
    Use a secrets manager (Sealed Secrets, External Secrets Operator, Vault, …)
    in production.

ConfigMap (``configmap.yaml``)
-------------------------------

Non-sensitive settings are stored in a ConfigMap. Variable names use ``__`` as the
separator for hierarchical ASP.NET Core configuration values — identical to the
:doc:`Docker Compose installation <installation-docker-compose>`.

The most important values to adjust before the first deploy:

.. code:: yaml

    # Public-facing URL — must match what clients use to reach the server
    IDENTITYSERVER__PUBLICORIGIN: "https://auth.example.com"

    IDENTITYSERVER__APPLICATIONTITLE: "IdentityServer.NET"

    # LiteDB (default) — path inside the PVC mounted at /home/app
    IDENTITYSERVER__CONNECTIONSTRINGS__LITEDB: "/home/app/identityserver-net/is_identityserver-net.db"

    # Behind an ingress controller that terminates TLS:
    IDENTITYSERVER__CONFIGURE__USEHTTPSREDIRECTION: "false"
    IDENTITYSERVER__CONFIGURE__ADDXFORWARDEDPROTOMIDDLEWARE: "true"

Full list of available configuration variables: see ``configmap.yaml`` in the repository.

PersistentVolumeClaim (``pvc.yaml``)
--------------------------------------

A 1 Gi PVC is sufficient for most deployments with LiteDB.
Adjust the storage class to match your cluster:

.. code:: yaml

    apiVersion: v1
    kind: PersistentVolumeClaim
    metadata:
      name: identityserver-pvc
      namespace: default
    spec:
      accessModes:
        - ReadWriteOnce
      resources:
        requests:
          storage: 1Gi
      # storageClassName: standard

Deployment (``deployment.yaml``)
----------------------------------

The Deployment mounts the PVC, sets the security context, and configures health probes.

Volume mount and permissions
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The PVC is mounted at ``/home/app`` — **not** at ``/home/app/identityserver-net``.
Mounting one level deeper would cause Docker/Kubernetes to create the directory as
``root``, which the non-root ``app`` user (UID 1654) cannot write to. Mounting at
``/home/app`` instead lets Kubernetes copy the correct ownership from the image.

``fsGroup: 1654`` ensures the mounted volume is owned by the ``app`` user:

.. code:: yaml

    spec:
      securityContext:
        fsGroup: 1654
        runAsNonRoot: true

Health probes
~~~~~~~~~~~~~

Both probes hit the OIDC discovery endpoint, which is available as soon as the server
is fully initialized:

.. code:: yaml

    livenessProbe:
      httpGet:
        path: /.well-known/openid-configuration
        port: 8080
      initialDelaySeconds: 15
      periodSeconds: 30

    readinessProbe:
      httpGet:
        path: /.well-known/openid-configuration
        port: 8080
      initialDelaySeconds: 10
      periodSeconds: 10

Service (``service.yaml``)
---------------------------

A ClusterIP Service maps port 80 to the container's port 8080:

.. code:: yaml

    spec:
      selector:
        app: identityserver
      ports:
        - name: http
          port: 80
          targetPort: 8080
      type: ClusterIP

Change ``type`` to ``NodePort`` or ``LoadBalancer`` if you don't use an Ingress
controller.

Ingress / TLS (``ingress.yaml``)
----------------------------------

The template targets the nginx Ingress Controller. Replace ``auth.example.com`` with
your domain:

.. code:: yaml

    spec:
      rules:
        - host: auth.example.com
          http:
            paths:
              - path: /
                pathType: Prefix
                backend:
                  service:
                    name: identityserver
                    port:
                      name: http

TLS with cert-manager (Let's Encrypt)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Uncomment the ``tls`` block and the cert-manager annotation:

.. code:: yaml

    metadata:
      annotations:
        cert-manager.io/cluster-issuer: "letsencrypt-prod"
    spec:
      tls:
        - hosts:
            - auth.example.com
          secretName: identityserver-tls

Then update ``IDENTITYSERVER__PUBLICORIGIN`` in ``configmap.yaml``:

.. code:: yaml

    IDENTITYSERVER__PUBLICORIGIN: "https://auth.example.com"

And apply the updated ConfigMap, followed by a rollout restart:

.. code:: bash

    kubectl apply -f configmap.yaml
    kubectl rollout restart deployment/identityserver

Migrations (Seeding)
--------------------

.. note::

    Migrations only run when ``ASPNETCORE_ENVIRONMENT=Development`` is set.
    They create missing entries on first start and never overwrite existing data.
    The variable can be removed after the first successful start — or left in place,
    since subsequent runs will not modify anything that already exists.

Seeding is configured in ``configmap.yaml``. All migration values are non-sensitive
except passwords and secrets, which belong in ``secret.yaml``.

Example — full seed for a typical setup:

*configmap.yaml*

.. code:: yaml

    ASPNETCORE_ENVIRONMENT: "Development"

    # Identity Resources
    IDENTITYSERVER__MIGRATIONS__IDENTITYRESOURCES__0__NAME: "openid"
    IDENTITYSERVER__MIGRATIONS__IDENTITYRESOURCES__1__NAME: "profile"
    IDENTITYSERVER__MIGRATIONS__IDENTITYRESOURCES__2__NAME: "role"

    # API Resource
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__NAME: "my-api"
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__SCOPES__0__NAME: "read"
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__SCOPES__1__NAME: "write"

    # Roles
    IDENTITYSERVER__MIGRATIONS__ROLES__0__NAME: "admin"
    IDENTITYSERVER__MIGRATIONS__ROLES__1__NAME: "user"

    # Additional user (password in secret.yaml)
    IDENTITYSERVER__MIGRATIONS__USERS__0__NAME: "alice@example.com"
    IDENTITYSERVER__MIGRATIONS__USERS__0__ROLES__0: "admin"

    # Web Application Client
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTTYPE: "WebApplication"
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTID: "my-webapp"
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTURL: "https://myapp.example.com"
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__SCOPES__0: "openid"
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__SCOPES__1: "profile"
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__SCOPES__2: "role"

    # API Client (Client Credentials)
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__CLIENTTYPE: "ApiClient"
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__CLIENTID: "my-service"
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__SCOPES__0: "my-api"
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__SCOPES__1: "my-api.read"

*secret.yaml*

.. code:: yaml

    IDENTITYSERVER__MIGRATIONS__ADMINPASSWORD: "Admin1234!"
    IDENTITYSERVER__MIGRATIONS__APIRESOURCES__0__APISECRET: "apisecret"
    IDENTITYSERVER__MIGRATIONS__USERS__0__PASSWORD: "Alice1234!"
    IDENTITYSERVER__MIGRATIONS__CLIENTS__0__CLIENTSECRET: "secret"
    IDENTITYSERVER__MIGRATIONS__CLIENTS__1__CLIENTSECRET: "secret"

Passkey (WebAuthn / FIDO2)
--------------------------

Passkeys enable passwordless authentication using biometrics or hardware security keys.
Add the following to ``configmap.yaml``:

.. list-table::
   :widths: 60 40
   :header-rows: 1

   * - Variable
     - Description
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__ALLOWPASSWORDLESS``
     - ``"true"`` – enable passwordless first-factor sign-in
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__ALLOWSECONDFACTOR``
     - ``"true"`` – enable passkey as a second factor after username/password
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__SERVERDOMAIN``
     - WebAuthn Relying Party ID, e.g. ``"example.com"``. Leave empty to inherit from the request host.
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__RELYINGPARTYNAME``
     - Human-readable name shown in the authenticator during passkey registration

First login
-----------

After the first start, check the pod logs for the generated admin credentials:

.. code:: bash

    kubectl logs deployment/identityserver

Look for a block similar to:

.. code::

    ################# Setup ##################
    User admin@identityserver.net created
    Password: Admin1234!
    #########################################

The default admin e-mail is ``admin@identityserver.net``.
If ``IDENTITYSERVER__MIGRATIONS__ADMINPASSWORD`` was not set, a random 16-character
password is generated and printed in the logs.

Updating
--------

Pull the latest image and trigger a rolling restart:

.. code:: bash

    kubectl rollout restart deployment/identityserver
    kubectl rollout status deployment/identityserver

To pin a specific version, edit ``image:`` in ``deployment.yaml``:

.. code:: yaml

    image: gstalt/identityserver-net:5.25.0000

Changing configuration
-----------------------

After editing ``configmap.yaml`` or ``secret.yaml``, apply and restart:

.. code:: bash

    kubectl apply -f configmap.yaml   # or secret.yaml
    kubectl rollout restart deployment/identityserver

Horizontal scaling
------------------

.. note::

    With LiteDB (default), only ``replicas: 1`` is possible — LiteDB does not support
    concurrent write access from multiple pods.

To run multiple replicas, switch to one of the following databases (connection string
in ``secret.yaml``):

* SQL Server (``IDENTITYSERVER__CONNECTIONSTRINGS__SQLSERVER``)
* PostgreSQL (``IDENTITYSERVER__CONNECTIONSTRINGS__POSTGRES``)
* MongoDB (``IDENTITYSERVER__CONNECTIONSTRINGS__MONGODB``)

The PVC must also support ``accessModes: ReadWriteMany``, or signing keys and
data-protection keys must be stored externally.

Troubleshooting
---------------

**Pod stays in** ``Pending``

.. code:: bash

    kubectl describe pod -l app=identityserver

Check the ``Events`` section — usually a PVC that cannot be bound or insufficient
node resources.

**Pod crashes on startup (** ``CrashLoopBackOff`` **)**

.. code:: bash

    kubectl logs deployment/identityserver --previous

Common causes:

* ``UnauthorizedAccessException: Access to the path '/home/app/...' is denied`` —
  the volume is mounted at the wrong path or ``fsGroup`` is missing. Verify that
  the mount is at ``/home/app`` and ``securityContext.fsGroup: 1654`` is set.

* ``IDENTITYSERVER__PUBLICORIGIN`` not set — the server cannot start without a
  valid public origin.

**Pod is** ``Running`` **but the site is unreachable**

.. code:: bash

    kubectl get ingress
    kubectl describe ingress identityserver

Verify that the Ingress controller is installed and that the host matches the domain
you are using.

**Quick shell access for debugging**

.. code:: bash

    kubectl exec -it deployment/identityserver -- /bin/sh
