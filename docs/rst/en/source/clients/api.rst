API Client
==========

An *API Client* is an application that needs to access a **Web API** requiring a
valid **Bearer Token** issued by **IdentityServerNET**.

API Resource
------------

To issue a **Bearer Token** for an API, this API must first be created as an
**API Resource**. Navigate to ``Resources (Identity & APIs)``/``API Resources`` 
via the ``Admin`` page to add a new **API Resource**:

.. image:: img/api1.png

In the next step, **Scopes** must be created for the **API Resource**:

.. image:: img/api2.png

.. note::

    The naming convention for API Resource scopes is: ``api-resource-name.scope-name``. 
    When a scope is entered, it is automatically converted to this format. An exception is a scope 
    that has the same name as the ``api-resource``. To create a scope that does not follow this 
    convention, prefix it with ``@@``, e.g., ``@@scope-name``.

For an API, the following **scopes** are created automatically:

* ``{api-name}``: General access to the API
* ``{api-name}.query``: Read-only access to data provided by the API
* ``{api-name}.command``: Additionally, write access to data provided by the API

.. note::

    The ``{api-name}`` scope should later be added to a client as a scope. This scope 
    will then correspond to the ``Audience`` (``aud``) of the token!

Creating/Editing an API Client
------------------------------

To create a new *Client*, a unique *Client Id* must be assigned. 
An optional display name can also be provided.

To simplify input, select the ``API`` template. Additionally, in this template, 
enter the URL of the web application. Entering **Scopes** is optional and can 
also be edited in the next step:

.. image:: img/api3.png

After successfully creating the client, you will be redirected to the ``Modify Client: ...`` page. 
The various client properties are organized into menu items:

``Name``:
+++++++++

.. image:: img/api4.png

Here, you can change the client's display name and add a description.

``Client Secrets``
++++++++++++++++++

Here, a secret must be provided that the client uses to authenticate with the Identity Server. 
A secure secret can be generated via the **Random Secret Generator**. For simplicity, we will use 
the secret ``secret``:

.. image:: img/api5.png

``Allowed Grants``
++++++++++++++++++

Since the client type ``ApiClient`` was selected when creating the client, ``ClientCredentials`` should be selected here:

.. image:: img/api6.png

``Allowed Scopes``
++++++++++++++++++

Here, the scopes created for the ``API Resource`` must be added. The **Scopes** specify specific 
access rights to the API. For the ``my-api-command`` client, it makes sense to select the ``my-api`` 
and ``my-api.command`` scopes from the ``Add existing resource scope`` section:

.. image:: img/api7.png

``Advanced Properties``
+++++++++++++++++++++++

Here, the lifetime of an *AccessToken* can be defined, for example:

.. image:: img/api8.png

.. note::

    All other menu items are less relevant for *API Clients* and are not listed in detail here.

Retrieving an AccessToken
-------------------------

HTTP Request
++++++++++++

A client application can retrieve an AccessToken from *IdentityServerNET* using an **HTTP POST** request 
with the necessary parameters in the body. The scopes are passed via the ``scope`` parameter, using spaces as separators:


.. code:: 
    
    POST https://localhost:44300/connect/token
    Content-Type: application/x-www-form-urlencoded

    grant_type=client_credentials&client_id=my-api-commands&client_secret=secret&scope=my-api my-api.command

or

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

    The ``scope`` parameter can also be omitted. In this case, the token will include all scopes configured for the client.


IdentityServerNET.Clients
++++++++++++++++++++++++++

To retrieve a token, the **NuGet** package ``IdentityServerNET.Clients`` can also be used:

.. code:: bash

    dotnet add package IdentityServerNET.Clients

.. code:: csharp

    var tokenClient = new IdentityServerNET.Clients.TokenClient("my-api-commands", "secret");
    await tokenClient.GetAccessToken("https://localhost:44300", []);

    var accessToken = tokenClient.AccessToken;


IdentityModel
+++++++++++++

**IdentityModel** also provides a way to retrieve a token:

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

To secure an API using a (Bearer) token, the process is generally as follows:

``Program.cs``
++++++++++++++

In the ``Program.cs`` file, register the necessary *authentication* and *authorization* services.

With ``AddAuthentication``, specify that a ``Bearer (JWT) Token`` is used for client authentication.
The options define who is responsible for authentication (``Authority``). Additionally, the ``Audience`` 
that the token must be issued for can be specified. ``TokenValidationParameters`` determines which **claims** 
are checked to validate the token. Setting ``ClockSkew = TimeSpan.Zero`` ensures that the token is immediately rejected 
if its **ExpirationTime** is exceeded.

Using ``AddAuthorization``, **policies** can be defined. A **policy** governs the rights a client has for API calls.
In this example, the ``scope`` claim is required, with distinctions made between ``command`` and ``query`` rights.

To apply authentication and authorization, the application must also use the corresponding middleware 
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

To secure individual **controllers** or **methods**, use the ``[Authorize]`` attribute.
Specify the previously configured ``AuthenticationScheme`` (**Bearer**) and the required ``Policy`` 
(**query**, **command**). This ensures that the methods in these API controllers can only be accessed 
if a **Bearer Token** with the **scope** ``my-api.query`` or ``my-api.command`` is provided.

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



