Managing Clients
================

Clients are applications that can log in through the IdentityServer. The following application types can be distinguished:

* **Web Application:** A web application where users must log in with a username and password.
* **API Clients:** Applications that need to access a (web) API, which requires a valid *Bearer Token* issued by the IdentityServer.
* **JavaScript Client:** A *Single Page Application* or *static website* where login with a username and password is required. 

To manage and create clients, you must be logged in as an administrator. In the *Admin area*, the ``Clients`` tile 
leads to the ``Add or modify clients`` view:

.. image:: img/index1.png

From here, you can create new clients or manage existing ones.

.. toctree::
   :maxdepth: 2
   :caption: Contents:

   webapp
   api

   


   