API Client
==========

Ein *API Client* ist eine Anwendung, die auf eine **Web API** zugreifen muss, die einen
gültigen **Bearer Token** erfordert, der vom **IdentityServerNET** ausgestellt wurde.

API Resource
------------

Um einen **Bearer Token** für eine API auszustellen, muss diese API zuerst als
**API Resource** angelegt werden. Dazu navigiert man über die ``Admin``-Seite zu 
``Resources (Identity & APIs)``/``API Resources``.
Dort kann eine neue **API Resource** angelegt werden:

.. image:: img/api1.png

Im nächsten Schritt müssen für die **API Resource** mögliche **Scopes** erstellt werden:

.. image:: img/api2.png

.. note::

    Die Namenskonvention für API Resource Scopes ist: ``api-resource-name.scope-name``. 
    Wenn ein Scope eingegeben wird, wird er automatisch in dieses Format umgewandelt. Eine Ausnahme ist ein Scope, 
    der den gleichen Namen hat wie die ``api-resource``. Möchte man einen Scope anlegen, der von dieser 
    Konvention abweicht, muss dieser mit vorangestelltem ``@@`` angegeben werden, z. B. ``@@scope-name``.

Für eine API werden nach dem Erstellen automatisch folgende **Scopes** angelegt:

* ``{api-name}``: Allgemeiner Zugriff auf die API
* ``{api-name}.query``: Lesezugriff auf die von der API bereitgestellten Daten
* ``{api-name}.command``: Zusätzlich Schreibzugriff auf die von der API bereitgestellten Daten

.. note::

    Der Scope ``{api-name}`` sollte später unbedingt als Scope bei einem Client hinzugefügt werden. Er entspricht 
    der ``Audience`` (``aud``) des Tokens!

API Client erstellen/bearbeiten
-------------------------------

Um einen neuen *Client* zu erstellen, muss eine eindeutige *Client Id* vergeben werden. 
Optional kann auch ein sprechender Name zugewiesen werden.

Damit nicht alles manuell eingegeben werden muss, sollte als Template ``API`` gewählt 
werden. Außerdem sollte in diesem Template die URL zur Webanwendung eingetragen werden. Die Eingabe der **Scopes** ist optional und kann 
auch im nächsten Schritt bearbeitet werden:

.. image:: img/api3.png

Nach erfolgreicher Erstellung des Clients gelangt man zur Seite ``Modify Client: ...``. Hier sind die 
verschiedenen Eigenschaften des Clients in Menüpunkten gegliedert:

``Name``:
+++++++++

.. image:: img/api4.png

Hier kann der Name des Clients geändert und eine Beschreibung hinzugefügt werden.

``Client Secrets``
++++++++++++++++++

Hier muss ein Secret angegeben werden, mit dem sich der Client am Identity Server authentifizieren muss. Über den
**Random Secret Generator** kann ein sicheres Secret erzeugt werden. Für dieses Beispiel verwenden wir das einfache Secret ``secret``:

.. image:: img/api5.png

``Allowed Grants``
++++++++++++++++++

Da beim Erstellen des Clients der Typ ``ApiClient`` gewählt wurde, sollte hier ``ClientCredentials`` ausgewählt sein:

.. image:: img/api6.png

``Allowed Scopes``
++++++++++++++++++

Hier müssen die Scopes hinzugefügt werden, die für die ``API Resource`` erstellt wurden. Die **Scopes** bestimmen in der 
API später spezielle Zugriffsrechte. Beim ``my-api-command`` Client macht es Sinn, den ``my-api`` und 
den ``my-api.command`` Scope aus dem Bereich ``Add existing resource scope`` zu übernehmen:

.. image:: img/api7.png

``Advanced Properties``
+++++++++++++++++++++++

Hier kann beispielsweise die Lebensdauer eines *AccessTokens* definiert werden:

.. image:: img/api8.png

.. note::

    Alle weiteren Menüpunkte sind für *API Clients* weniger relevant und werden hier nicht im Detail aufgelistet.

Abholen eines AccessTokens
--------------------------

HTTP Request
++++++++++++

Eine Client-Anwendung kann über einen **HTTP POST** Request mit den notwendigen Parametern im Body ein AccessToken von *IdentityServerNET* abholen.
Die Scopes werden über den Parameter ``scope`` übergeben, wobei Leerzeichen als Trennzeichen verwendet werden:


.. code:: 
    
    POST https://localhost:44300/connect/token
    Content-Type: application/x-www-form-urlencoded

    grant_type=client_credentials&client_id=my-api-commands&client_secret=secret&scope=my-api my-api.command

bzw.

.. code::

    POST https://localhost:44300/connect/token
    Authorization: Basic bXktYXBpLWNvbW1hbmRzOnNlY3JldA==
    Content-Type: application/x-www-form-urlencoded

    grant_type=client_credentials&scope=my-api.command my-api

.. code::

    {
        "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IkVCM...",
        "expires_in": 3600,
        "token_type": "Bearer",
        "scope": "my-api my-api.command"
    }

... note::

    Der ``scope`` Parameter kann auch weggelassen werden. In diesem Fall enthält der Token alle für den Client eingestellten Scopes.


IdentityServerNET.Clients
++++++++++++++++++++++++++

Zum Abholen eines Tokens kann auch das **NuGet**-Paket ``IdentityServerNET.Clients`` verwendet werden:

.. code:: bash

    dotnet add package IdentityServerNET.Clients

.. code:: csharp

    var tokenClient = new IdentityServerNET.Clients.TokenClient("my-api-commands", "secret");
    await tokenClient.GetAccessToken("https://localhost:44300", []);

    var accessToken = tokenClient.AccessToken;


IdentityModel
+++++++++++++

**IdentityModel** bietet ebenfalls eine Möglichkeit, einen Token abzuholen:

.. code:: bash

    dotnet add package IdentityModel

.. code:: csharp

    var client = new HttpClient();

    // Entdecke den Endpunkt des IdentityServers
    var discovery = await client.GetDiscoveryDocumentAsync("https://localhost:44300");
    if (discovery.IsError)
    {
        Console.WriteLine(discovery.Error);
        return;
    }

    // Get tht Token
    var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
    {
        Address = discovery.TokenEndpoint,

        ClientId = "my-api-commands",
        ClientSecret = "secret",
        Scope = "my-api my-api.command"
    });

    if (tokenResponse.IsError)
    {
        Console.WriteLine(tokenResponse.Error);
        return;
    }

    Console.WriteLine(tokenResponse.AccessToken);


API Authorization
-----------------

Um eine API über einen (Bearer) Token abzusichern, ist die Vorgehensweise in etwa folgendermaßen:

``Program.cs``
++++++++++++++

In der Datei ``Program.cs`` werden zuerst die notwendigen *Authentication*- und *Authorization*-Services registriert.

Mit ``AddAuthentication`` wird festgelegt, dass zur Authentifizierung des Clients ein ``Bearer (JWT) Token`` verwendet wird.
Über die Optionen wird definiert, wer für die Authentifizierung verantwortlich ist (``Authority``). Zudem kann die ``Audience`` 
vorgegeben werden, für die der Token ausgestellt sein muss. Über die ``TokenValidationParameters`` wird festgelegt, welche **Claims** 
überprüft werden, um einen Token als gültig anzuerkennen. ``ClockSkew = TimeSpan.Zero`` gibt an, dass der Token sofort abgelehnt wird, 
wenn die **ExpirationTime** des Tokens überschritten ist.

Mit ``AddAuthorization`` können **Policies** definiert werden. Eine **Policy** regelt, welche Rechte ein Client bei API-Aufrufen hat.
In diesem Beispiel wird der Claim ``scope`` vorausgesetzt und es wird zwischen ``command``- und ``query``-Rechten unterschieden.

Damit Authentifizierung und Autorisierung angewendet werden, muss die Anwendung auch die entsprechende Middleware verwenden 
(``UseAuthentication``, ``UseAuthorization``).

.. code:: csharp

    var builder = WebApplication.CreateBuilder(args);

    // ...

    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.Authority = "https://localhost:44300";
            options.RequireHttpsMetadata = false;

            options.Audience = "my-api";
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
        });

    builder.Services
        .AddAuthorization(options => 
        {
            options.AddPolicy("query",
                policy =>
                policy.RequireClaim("scope", "my-api.query"));
            options.AddPolicy("command",
                policy =>
                policy.RequireClaim("scope", "my-api.command"));
        });

    // ...

    var app = builder.Build();

    // ...

    app.UseAuthentication(); 
    app.UseAuthorization();  

    // ...

    app.Run();

``Controller``
++++++++++++++

Um einzelne **Controller** oder **Methoden** abzusichern, wird das Attribut ``[Authorize]`` verwendet.
Dabei wird dem Attribut das oben festgelegte ``AuthenticationScheme`` (**Bearer**) und die notwendige ``Policy`` 
(**query**, **command**) übergeben. Die Methoden dieser API-Controller können somit nur aufgerufen werden, wenn
ein **Bearer Token** übergeben wird, der den **scope** ``my-api.query`` oder ``my-api.command`` enthält.

.. code:: csharp

    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "query")]
    [ApiController]
    public class MyApiQueryController : ControllerBase
    {
        // ...
    }


    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "command")]
    [ApiController]
    public class MyApiCommandController : ControllerBase
    {
        // ...
    }



