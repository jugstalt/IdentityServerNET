API Client
==========

Ein *API Client* ist eine Anwendung, die auf eine **Web API** zugreifen muss, die einen
gültigen **Bearer Token** erfordert, der vom **IdentityServerNET** ausgestellt wurde.

API Resource
------------

Um einen **Bearer Token** für eine API auszustellen, muss diese API im ersten Schritt als
**API Resource** angelegt werden. Dazu navigiert man über die ``Admin`` Seite zu 
``Resources (Identity & APIs)``/``API Resources``.
Dort kann eine neue **API Resource** angelegt werden:

.. image:: img/api1.png

Im nächsten Schritt müssen für die **API Resource** mögliche **Scopes** angelegt werden:

.. image:: img/api2.png

.. note::

    Die Namenskonvention für API Resource Scopes ist: ``api-resource-name.scope-name``. 
    Gibt man einen Scope ein, wird dieser automatisch in diese Konvention umgewandelt. Eine Ausnahme ist ein Scope, 
    der den gleichen Namen hat, wie die ``api-resource``. Möchte man einen Scope anlegen, der nicht diese 
    Konvention entspricht, muss dieser mit vorangestelltem ``@@`` angegeben werden, zB ``@@scope-name``

Für eine API werden nach dem erstellen automatisch folgende **Scopes** angelegt:

* ``{api-name}``: Allgemeiner Zugriff auf die API
* ``{api-name}.query``: Lesender Zugriff auf die von der API bereitgestellten Daten
* ``{api-name}.command``: Zusätzlich schreibender Zugriff auf die von der API bereitgestellten Daten

.. note::

    Der Scope ``{api-name}`` sollte später bei einem Client unbedingt als Scope hinzugefügt werden. Diese entspricht dann 
    der ``Audience`` (``aud``) des Tokens!

API Client erstellen/bearbeiten
-------------------------------

Um einen neuen *Client* zu erstellen, muss eine eindeutige *Client Id* vergeben werden. 
Optional kann auch ein sprechender Name vergeben werden.

Damit nicht alles manuelle eingegeben werden muss, sollte als Template ``API`` eingetragen 
werden. Außerdem sollte für diesem Template 
die Url zur Web Anwendung eingetragen werden. Die Eingabe der **Scopes** ist optional. Diese können 
auch im nächsten Schritt noch bearbeitet werden:

.. image:: img/api3.png

Wurde der Client erfolgreich erzeugt, kommt man zur Seite ``Modify Client: ...``. Hier sind die 
unterschiedlichen Eigenschaften für den Client in Menüpunkten gegliedert:

``Name``:
+++++++++

.. image:: img/api4.png

Hier kann der sprechende Name für den Client verändert werden. Außerdem kann eine Beschreibung 
für den Client eingetragen werden.

``Client Secrets``
++++++++++++++++++

Hier muss ein Secret angegeben werden, mit dem sich der Client am Identity Server Anmelden muss. Über den
**Random Secret Generator** kann ein sicheres Secret erzeugt werden. Der Einfachheit halber verwenden wir hier 
allerdings als Secret einfach ``secret``:

.. image:: img/api5.png

``Allowed Grants``
++++++++++++++++++

Da beim Erstellen des Clients der Typ ``ApiClient`` gewählt wurde, sollte hier ``ClientCredentials`` ausgewählt sein:

.. image:: img/api6.png

``Allowed Scopes``
++++++++++++++++++

Hier müssen die Scopes hinzugefügt werden, für die ``API Resource`` angelegt worden sind. Die **Scopes** bestimmen in der 
API später spezielle Rechte für den Zugriff auf die API. Beim ``my-api-command`` Client, macht es hier Sinn, den ``my-api`` und 
den ``my-api.command`` Scope aus ``Add existing resource scope`` Bereich zu übernehmen:

.. image:: img/api7.png

``Advanced Properties``
+++++++++++++++++++++++

Hier kann beispielsweise die lebensdauer für einen *AccessToken* definiert werden:

.. image:: img/api8.png

.. note::

    Alle weiteren Menüpunkte sind für *API Clients* weniger relevant und werden nicht im Detail aufgelistete.

Abholen eines AccessTokens
--------------------------

HTTP Request
++++++++++++

Eine Client Anwendung kann über einen **HTTP Post** Request, mit den notwendigen Parametern im Body, einen AccessToken von *IdentityServerNET* abholen.
Die Scopes werden über den Parameter ``scope`` mit leerzeichen als Trennzeichen übergeben:

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

.. note::

    Der ``scope`` Parameter kann auch weggelassen werden. In dem Fall enthält der Token alle für den Client eingestellten Scopes.


IdentityServerNET.Clients
+++++++++++++++++++++++++++

Zum Abholen eines Tokens kann auch das ``IdentityServerNET.Clients`` **nuget** Package verwendet werden:

.. code:: bash

    dotnet add package IdentityServerNET.Clients

.. code:: csharp

    var tokenClient = new IdentityServerNET.Clients.TokenClient("my-api-commands", "secret");
    await tokenClient.GetAccessToken("https://localhost:44300", []);

    var accessToken = tokenClient.AccessToken;


IdentityModel
+++++++++++++

**IdentityModel** bietet ebenfalls ein Möglichkeit einen Token abzuholen:

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


Api Authorization
-----------------

Möchte man eine API über einen (Bearer) Token absichern, ist die Vorgehensweise in etwas folgendermaßen:

``Program.cs``
++++++++++++++

In der ``Programm.cs`` Datei zu erst die notwendigen *Authentication* und *Authorization* Services registriert.

Über die ``AddAuthoriation`` wird angegeben, dass die zur Authentifizierung des Clients eine ``Bearer (JWT) Token`` verwendet wird.
Über die Optionen wird hier gesteuert, wer für die Authentifizierung verantwortlich ist (``Authority``). Ebenfalls kann die ``Audience`` vorgeben 
werden, für die dieser der Token ausgestellt werden werden muss. Über die ``TokenValidationParameters`` wird festgelegt, welche **Claims**
überprüft werden, um einen Token als gültig anzuerkennen. ``ClockSkew = TimeSpan.Zero`` gibt an, der der Token sofort abgelehnt wird, 
wenn die **ExpirationTime** des Tokens überschritten wird.

Mittels ``AddAuthorization`` können **Policies** angeführt werden. Über eine **Policy** wird geregelt, welche Rechte ein Client bei API Aufrufen hat.
Hier wird ``scope`` als Claim vorausgesetzt und zwischen ``command`` und ``query`` Rechten unterschieden.

Damit Authentifizierung und Autorisierung angewendet wird, muss die Application auch die entsprechende Middleware verwenden (``UseAuthentication``, ``UseAuthorization``)-

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

Um einzelnen **Controller** oder **Methoden** abzusichern wird das ``[Authorize]`` verwendet.
Hier wird dem Attribute noch das oben festgelegte ``AuthenticationScheme`` (**Bearer**) und die notwendige ``Policy`` 
(**query**, **command**) übergeben. Die Methoden dieser API Controller können somit nur aufgerufen werden, wenn
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



