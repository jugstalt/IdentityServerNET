Installation mit Kubernetes
===========================

Die Kubernetes-Manifeste befinden sich im Repository unter
`dist/kubernetes/ <https://github.com/jugstalt/identityserver.net/tree/master/dist/kubernetes>`_.

Dateien im Überblick
--------------------

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Datei
     - Inhalt
   * - ``secret.yaml``
     - Passwörter und Zugangsdaten (Zertifikats-Passwort, Admin-Passwort, DB-Connection-Strings, Mail)
   * - ``configmap.yaml``
     - Alle nicht-sensiblen Einstellungen sowie auskommentierte Migrationsbeispiele
   * - ``pvc.yaml``
     - PersistentVolumeClaim für die Datenhaltung (Datenbank, Signing-Keys, Data-Protection-Keys)
   * - ``deployment.yaml``
     - Deployment mit SecurityContext, Liveness-/Readiness-Probes und Resource-Limits
   * - ``service.yaml``
     - ClusterIP-Service, der den Container-Port 8080 intern auf Port 80 exponiert
   * - ``ingress.yaml``
     - Ingress-Vorlage (nginx, optional TLS via cert-manager)

Schnellstart
------------

.. code:: bash

    git clone https://github.com/jugstalt/identityserver.net.git
    cd identityserver.net/dist/kubernetes

    # 1. secret.yaml und configmap.yaml an die eigene Umgebung anpassen
    # 2. Alle Manifeste anwenden
    kubectl apply -f .

Der OIDC-Discovery-Endpunkt ist danach erreichbar unter:
``http://<ingress-host>/.well-known/openid-configuration``

Namespace
---------

Alle Manifeste verwenden standardmäßig ``namespace: default``. Für einen eigenen
Namespace diesen zuerst anlegen und anschließend das Feld ``namespace:`` in jeder
Datei anpassen:

.. code:: bash

    kubectl create namespace identity
    # dann namespace: identity in allen .yaml Files setzen

Alternativ ``kubectl apply -n identity -f .`` verwenden und das Namespace-Feld in den
Dateien weglassen — für Produktionsumgebungen ist die explizite Angabe in den Dateien
aber vorzuziehen.

Secret (``secret.yaml``)
-------------------------

Sensible Werte werden in einem Kubernetes Secret gespeichert. Die Datei verwendet
``stringData``, sodass Werte im Klartext eingetragen werden können — Kubernetes
kodiert sie automatisch in Base64:

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

      # Datenbank — genau eine auskommentieren:
      # IDENTITYSERVER__CONNECTIONSTRINGS__SQLSERVER: "Server=db;..."
      # IDENTITYSERVER__CONNECTIONSTRINGS__POSTGRES: "Host=db;..."
      # IDENTITYSERVER__CONNECTIONSTRINGS__MONGODB: "mongodb://mongo:27017"

      # Mail-Sender — gewünschten auskommentieren:
      # IDENTITYSERVER__MAIL__SMTP__PASSWORD: "smtp-password"
      # IDENTITYSERVER__MAIL__SENDGRID__APIKEY: "SG.xxxxxxxxxxxx"

      # Externer Identity Provider:
      # IDENTITYSERVER__EXTERNAL__MICROSOFTIDENTITYWEB__CLIENTSECRET: "..."

.. warning::

    ``secret.yaml`` mit echten Zugangsdaten niemals in ein öffentliches Repository
    einchecken. In Produktionsumgebungen einen Secrets-Manager verwenden
    (Sealed Secrets, External Secrets Operator, Vault, …).

ConfigMap (``configmap.yaml``)
-------------------------------

Nicht-sensible Einstellungen stehen in der ConfigMap. Die Variablennamen verwenden
``__`` als Trennzeichen für hierarchische ASP.NET Core-Konfigurationswerte — identisch
mit der :doc:`docker-compose-Installation <installation-docker-compose>`.

Die wichtigsten Werte vor dem ersten Deployment anpassen:

.. code:: yaml

    # Öffentliche URL — muss mit der URL übereinstimmen, unter der Clients den Server erreichen
    IDENTITYSERVER__PUBLICORIGIN: "https://auth.example.com"

    IDENTITYSERVER__APPLICATIONTITLE: "IdentityServer.NET"

    # LiteDB (Standard) — Pfad innerhalb des PVC, das bei /home/app eingehängt ist
    IDENTITYSERVER__CONNECTIONSTRINGS__LITEDB: "/home/app/identityserver-net/is_identityserver-net.db"

    # Hinter einem Ingress-Controller, der TLS terminiert:
    IDENTITYSERVER__CONFIGURE__USEHTTPSREDIRECTION: "false"
    IDENTITYSERVER__CONFIGURE__ADDXFORWARDEDPROTOMIDDLEWARE: "true"

Vollständige Liste aller verfügbaren Konfigurationsvariablen: siehe ``configmap.yaml``
im Repository.

PersistentVolumeClaim (``pvc.yaml``)
--------------------------------------

1 Gi ist für die meisten Deployments mit LiteDB ausreichend.
Die Storage-Class an den eigenen Cluster anpassen:

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

Das Deployment hängt das PVC ein, setzt den SecurityContext und konfiguriert
Health-Probes.

Volume und Berechtigungen
~~~~~~~~~~~~~~~~~~~~~~~~~~

Das PVC wird bei ``/home/app`` eingehängt — **nicht** bei
``/home/app/identityserver-net``. Ein Einhängen eine Ebene tiefer würde dazu führen,
dass Kubernetes das Verzeichnis als ``root`` anlegt, was der nicht-privilegierte
``app``-Benutzer (UID 1654) nicht beschreiben kann. Das Einhängen bei ``/home/app``
übernimmt stattdessen die Dateirechte aus dem Image.

``fsGroup: 1654`` stellt sicher, dass das eingehängte Volume dem ``app``-Benutzer
gehört:

.. code:: yaml

    spec:
      securityContext:
        fsGroup: 1654
        runAsNonRoot: true

Health-Probes
~~~~~~~~~~~~~

Beide Probes rufen den OIDC-Discovery-Endpunkt auf, der verfügbar ist, sobald der
Server vollständig initialisiert ist:

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

Ein ClusterIP-Service mappt Port 80 auf den Container-Port 8080:

.. code:: yaml

    spec:
      selector:
        app: identityserver
      ports:
        - name: http
          port: 80
          targetPort: 8080
      type: ClusterIP

``type`` auf ``NodePort`` oder ``LoadBalancer`` ändern, wenn kein Ingress-Controller
verwendet wird.

Ingress / TLS (``ingress.yaml``)
----------------------------------

Die Vorlage richtet sich an den nginx Ingress Controller. ``auth.example.com`` durch
die eigene Domain ersetzen:

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

TLS mit cert-manager (Let's Encrypt)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Den ``tls``-Block und die cert-manager-Annotation einkommentieren:

.. code:: yaml

    metadata:
      annotations:
        cert-manager.io/cluster-issuer: "letsencrypt-prod"
    spec:
      tls:
        - hosts:
            - auth.example.com
          secretName: identityserver-tls

Anschließend ``IDENTITYSERVER__PUBLICORIGIN`` in der ``configmap.yaml`` setzen:

.. code:: yaml

    IDENTITYSERVER__PUBLICORIGIN: "https://auth.example.com"

Und die aktualisierte ConfigMap einpielen sowie einen Rollout-Neustart auslösen:

.. code:: bash

    kubectl apply -f configmap.yaml
    kubectl rollout restart deployment/identityserver

Migrationen (Seeding)
---------------------

.. note::

    Migrationen werden nur ausgeführt, wenn ``ASPNETCORE_ENVIRONMENT=Development``
    gesetzt ist. Sie legen fehlende Einträge beim ersten Start an und überschreiben
    niemals vorhandene Daten. Danach kann die Variable entfernt oder belassen werden —
    nachfolgende Starts verändern nichts Bestehendes.

Das Seeding wird in der ``configmap.yaml`` konfiguriert. Alle Migrations-Werte sind
nicht-sensibel — Ausnahme: Passwörter und Secrets gehören in die ``secret.yaml``.

Vollständiges Beispiel für ein typisches Setup:

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

    # Rollen
    IDENTITYSERVER__MIGRATIONS__ROLES__0__NAME: "admin"
    IDENTITYSERVER__MIGRATIONS__ROLES__1__NAME: "user"

    # Zusätzlicher Benutzer (Passwort in secret.yaml)
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

Passkeys ermöglichen die passwortlose Anmeldung per Biometrie oder Hardware-Schlüssel.
Folgende Werte in der ``configmap.yaml`` setzen:

.. list-table::
   :widths: 60 40
   :header-rows: 1

   * - Variable
     - Beschreibung
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__ALLOWPASSWORDLESS``
     - ``"true"`` – passwortlose Erstfaktor-Anmeldung aktivieren
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__ALLOWSECONDFACTOR``
     - ``"true"`` – Passkey als zweiten Faktor nach Benutzername/Passwort
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__SERVERDOMAIN``
     - WebAuthn Relying Party ID, z. B. ``"example.com"``. Leer lassen = vom Request-Host ableiten.
   * - ``IDENTITYSERVER__LOGIN__PASSKEY__RELYINGPARTYNAME``
     - Anzeigename im Authenticator während der Passkey-Registrierung

Erstes Login
------------

Nach dem ersten Start die Pod-Logs auf die erzeugten Admin-Zugangsdaten prüfen:

.. code:: bash

    kubectl logs deployment/identityserver

Dort erscheint ein Block ähnlich wie:

.. code::

    ################# Setup ##################
    User admin@identityserver.net created
    Password: Admin1234!
    #########################################

Die Standard-Admin-E-Mail-Adresse ist ``admin@identityserver.net``.
Wurde ``IDENTITYSERVER__MIGRATIONS__ADMINPASSWORD`` nicht gesetzt, generiert der
Server ein zufälliges 16-Zeichen-Passwort und gibt es in den Logs aus.

Update
------

Das neueste Image laden und einen Rolling-Restart auslösen:

.. code:: bash

    kubectl rollout restart deployment/identityserver
    kubectl rollout status deployment/identityserver

Um eine bestimmte Version festzuhalten, ``image:`` in der ``deployment.yaml`` anpassen:

.. code:: yaml

    image: gstalt/identityserver-net:5.25.0000

Konfiguration ändern
---------------------

Nach Änderungen an ``configmap.yaml`` oder ``secret.yaml`` anwenden und neu starten:

.. code:: bash

    kubectl apply -f configmap.yaml   # bzw. secret.yaml
    kubectl rollout restart deployment/identityserver

Horizontale Skalierung
-----------------------

.. note::

    Mit LiteDB (Standard) ist nur ``replicas: 1`` möglich, da LiteDB keine
    gleichzeitigen Schreibzugriffe aus mehreren Pods unterstützt.

Für mehrere Replikas auf eine der folgenden Datenbanken wechseln
(Connection-String in ``secret.yaml``):

* SQL Server (``IDENTITYSERVER__CONNECTIONSTRINGS__SQLSERVER``)
* PostgreSQL (``IDENTITYSERVER__CONNECTIONSTRINGS__POSTGRES``)
* MongoDB (``IDENTITYSERVER__CONNECTIONSTRINGS__MONGODB``)

Außerdem muss das PVC ``accessModes: ReadWriteMany`` unterstützen oder der Speicher
für Signing-Keys und Data-Protection-Keys extern abgelegt werden.

Fehlerbehebung
--------------

**Pod bleibt in** ``Pending``

.. code:: bash

    kubectl describe pod -l app=identityserver

Im Abschnitt ``Events`` nachsehen — meistens ein PVC das nicht gebunden werden kann
oder unzureichende Node-Ressourcen.

**Pod stürzt beim Start ab (** ``CrashLoopBackOff`` **)**

.. code:: bash

    kubectl logs deployment/identityserver --previous

Häufige Ursachen:

* ``UnauthorizedAccessException: Access to the path '/home/app/...' is denied`` —
  das Volume ist am falschen Pfad eingehängt oder ``fsGroup`` fehlt. Prüfen, ob
  das Mount bei ``/home/app`` liegt und ``securityContext.fsGroup: 1654`` gesetzt ist.

* ``IDENTITYSERVER__PUBLICORIGIN`` nicht gesetzt — der Server kann ohne einen gültigen
  Public Origin nicht starten.

**Pod ist** ``Running``\ **, aber die Site ist nicht erreichbar**

.. code:: bash

    kubectl get ingress
    kubectl describe ingress identityserver

Sicherstellen, dass der Ingress-Controller installiert ist und der Host mit der
verwendeten Domain übereinstimmt.

**Shell-Zugriff zum Debuggen**

.. code:: bash

    kubectl exec -it deployment/identityserver -- /bin/sh
