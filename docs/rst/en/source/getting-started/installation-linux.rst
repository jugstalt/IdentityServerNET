Installation on Linux
=====================

.. note::

    ASP.NET Core 8.0 must be installed. Running ``dotnet --info`` in a *shell*
    should display the following framework in the output:

    .. code::

        .NET runtimes installed:
        Microsoft.AspNetCore.App 8.0.x [/usr/lib/dotnet/shared/Microsoft.AspNetCore.App]
        Microsoft.NETCore.App 8.0.x [/usr/lib/dotnet/shared/Microsoft.NETCore.App]

For Linux, ZIP files named ``identityserver.net-linux-x64-{version}.zip`` are available under `Releases <https://github.com/jugstalt/IdentityServerNET/releases>`_.

The ZIP file contains a folder named after the version number. This 
directory can be copied, for example, to ``~/apps/identityserver-net``.
Then, navigate to the directory ``~/apps/identityserver-net/{version}/app``
and run the following command:

.. code:: bash

    dotnet IdentityServer.dll --customAppSettings=dev-https

.. note::

    The server is started with ``--customAppSettings=dev-https``, which loads the additional 
    configuration ``appsettings.dev-https.json`` to specify ports and a developer certificate 
    for the HTTPS connection.

The application will then be accessible at http://localhost:8080 and https://localhost:8443.
