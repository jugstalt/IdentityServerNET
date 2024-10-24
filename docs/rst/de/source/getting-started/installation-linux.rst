Installation unter Linux
========================

.. note::

    Voraussetzung ist die Installation von ASPNET.Core 8.0. Gibt man in einer *Shell*
    ``dotnet --info`` ein muss folgendes Framework in der Ausgabe vorhanden sein:

    .. code::

        .NET runtimes installed:
        Microsoft.AspNetCore.App 8.0.x [/usr/lib/dotnet/shared/Microsoft.AspNetCore.App]
        Microsoft.NETCore.App 8.0.x [/usr/lib/dotnet/shared/Microsoft.NETCore.App]

Für Linux liegen unter `Releases <https://github.com/jugstalt/identityserver.nova/releases>`_
ZIP Dateien mit dem Namen ``identityserver.nova-linux-x64-{version}.zip``.

Das ZIP File enthält einen Ordner, der der Versionsnummer entspricht. Dieses 
Verzeichnis kann beispielsweise nach ``~/apps/identityserver-net`` kopiert werden.
Danach wechselt man in das Verzeichnis ``~/apps/identityserver-net/{version}/app``
und führt folgendes Kommando aus:

.. code:: bash

    dotnet IdentityServer.dll --customAppSettings=dev-https

.. note::

    Der Server wird mit ``--customAppSettings=dev-https`` aufgerufen. Damit wird zusätzlich 
    die Konfiguration ``appsettings.dev-https.json`` geladen, in der die Ports und eine 
    Entwicklerzertifikat für die HTTPS Verbindung geladen werden.

Die Anwendung läuft danach unter http://localhost:8080 bzw https://localhost:8443.