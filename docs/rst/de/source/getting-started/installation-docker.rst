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

    docker run -d -p 8080:8080 --name is-nova identityserver-net:5.24.3601

  Die Anwendung läuft damit unter http://localhost:8080

* ``gstalt/identityserver-net-dev:{tag}``:

  Ein Image, das als lokaler IdentityServer für die Entwicklung von Anwendung verwendet werden kann.
  Da für die Anmeldung von Anwendungen *HTTPS* vorausgesetzt wird, wir hier der *IdentityServerNET* 
  im Container unter dem Port 8443 mit und *HTTPS* mit denen selbst signierten Developer 
  Zertifikat gestartet.

  .. code:: bash

    docker run -d -p 8443:8443 --name is-nova-dev identityserver-net-dev:5.24.3601

  Die Anwendung läuft damit unter https://localhost:8443

Benutzerdefinierte Images bauen
-------------------------------

Mit dem Basis Image ``gstalt/identityserver-net-base:{tag}`` können Benutzerdefinierte
Images gebaut werden. Ein Docker File sieht dabei in etwa wie folgt aus:

.. code:: docker

    FROM identityserver-net-base:latest

    WORKDIR /app

    # use an override directory with 
    # custom config, settings and plugin files
    COPY /override .

    ENV ASPNETCORE_URLS=http://*:8080
    EXPOSE 8080

    ENTRYPOINT ["dotnet", "IdentityServer.dll"]
