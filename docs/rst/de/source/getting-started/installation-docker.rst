Installation mit Docker
=======================

Für Docker liegen auf *Docker Hub* folgende Images bereit:

* ``gstalt/identityserver-net-base:{tag}``:

  Ein Basis Images, dass alle notwendigen Assemblies beinhaltet. Dieses kann verwendet werden,
  um eingen Images mit eingenen Konfigurationen oder Plugins zu erstellen

* ``gstalt/identityserver-net:{tag}``:

  Der IdentityServer in einer *Default* Konfiguration. Die Applikation wird im Container mit 
  Port 8080 gestartet.

  .. code:: bash

    docker run -d -p 8080:8080 --name is-net identityserver-net:5.24.3601

  Die Anwendung läuft damit unter http://localhost:8080

* ``gstalt/identityserver-net-dev:{tag}``:

  Ein Image, das als lokaler IdentityServer für die Entwicklung von Anwendung verwendet werden kann.
  Da für die Anmeldung von Anwendungen *HTTPS* vorausgesetzt wird, wir hier der *IdentityServerNET* 
  im Container unter dem Port 8443 mit und *HTTPS* mit denen selbst signierten Developer 
  Zertifikat gestartet.

  .. code:: bash

    docker run -d -p 8443:8443 --name is-net-dev identityserver-net-dev:5.24.3601

  Die Anwendung läuft damit unter https://localhost:8443

Benutzerdefinierte Images bauen
-------------------------------

Mit dem Basis Image ``gstalt/identityserver-net-base:{tag}`` können Benutzerdefinierte
Images gebaut werden. Ein Docker File sieht dabei in etwa wie folgt aus:Installation mit Docker
=======================

Folgende Images stehen auf *Docker Hub* bereit:

* ``gstalt/identityserver-net-base:{tag}``:

  Ein Basis-Image, das alle notwendigen Assemblies enthält. Dieses kann verwendet werden,
  um eigene Images mit individuellen Konfigurationen oder Plugins zu erstellen.

* ``gstalt/identityserver-net:{tag}``:

  Der IdentityServer in einer *Default*-Konfiguration. Die Applikation wird im Container 
  auf Port 8080 gestartet.

  .. code:: bash

    docker run -d -p 8080:8080 --name is-net identityserver-net:5.24.3601

  Die Anwendung läuft damit unter http://localhost:8080

* ``gstalt/identityserver-net-dev:{tag}``:

  Ein Image, das als lokaler IdentityServer für die Anwendungsentwicklung genutzt werden kann.
  Da für die Anmeldung von Anwendungen *HTTPS* erforderlich ist, wird der *IdentityServerNET* 
  in diesem Container unter Port 8443 und mit *HTTPS* unter Verwendung eines selbstsignierten 
  Entwicklerzertifikats gestartet.

  .. code:: bash

    docker run -d -p 8443:8443 --name is-net-dev identityserver-net-dev:5.24.3601

  Die Anwendung läuft damit unter https://localhost:8443

Benutzerdefinierte Images erstellen
-----------------------------------

Mit dem Basis-Image ``gstalt/identityserver-net-base:{tag}`` können benutzerdefinierte
Images erstellt werden. Ein Dockerfile könnte wie folgt aussehen:

.. code:: docker

    FROM identityserver-net-base:latest

    WORKDIR /app

    # use an override directory with 
    # custom config, settings and plugin files
    COPY /override .

    ENV ASPNETCORE_URLS=http://*:8080
    EXPOSE 8080

    ENTRYPOINT ["dotnet", "IdentityServer.dll"]
