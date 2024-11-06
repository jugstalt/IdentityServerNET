Installation with Docker
========================

The following images are available on *Docker Hub*:

* ``gstalt/identityserver-net-base:{tag}``:

  A base image that includes all necessary assemblies. This can be used 
  to create custom images with specific configurations or plugins.

* ``gstalt/identityserver-net:{tag}``:

  IdentityServer in a *default* configuration. The application is started 
  on port 8080 within the container.

  .. code:: bash

    docker run -d -p 8080:8080 --name is-net identityserver-net:5.24.3601

  The application will be accessible at http://localhost:8080

* ``gstalt/identityserver-net-dev:{tag}``:

  An image designed as a local IdentityServer for application development.
  Since HTTPS is required for application login, *IdentityServerNET* is started 
  in this container on port 8443 with *HTTPS* using a self-signed developer 
  certificate.

  .. code:: bash

    docker run -d -p 8443:8443 --name is-net-dev identityserver-net-dev:5.24.3601

  The application will be accessible at https://localhost:8443

Building Custom Images
----------------------

Custom images can be built with the base image ``gstalt/identityserver-net-base:{tag}``. 
A Dockerfile might look like this:

.. code:: docker

    FROM identityserver-net-base:latest

    WORKDIR /app

    # use an override directory with 
    # custom config, settings and plugin files
    COPY /override .

    ENV ASPNETCORE_URLS=http://*:8080
    EXPOSE 8080

    ENTRYPOINT ["dotnet", "IdentityServer.dll"]
