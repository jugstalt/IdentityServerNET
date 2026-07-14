Signing Certificates
====================

**IdentityServerNET** signs every issued token (ID token, access token, ...) with an
asymmetric X.509 certificate, and publishes the corresponding public keys at the
``jwks_uri`` from the discovery document so that clients and APIs can verify tokens.
This page describes how those certificates are created, stored, rotated, and cleaned up.

Storage
-------

By default, certificates are stored as password-protected ``.pfx`` files under
``<StorageRootPath>/storage/validation`` (see :doc:`../getting-started/configuration`).
This is the default even without any explicit ``SigningCredential`` configuration.

.. code:: javascript

    "SigningCredential": {
      "Storage": "c:\\apps\\identityserver-net\\storage\\validation",  // optional override
      "CertPassword": "...",                                          // optional
      "InMemoryOnly": true                                            // optional, default: false
    }

* **Storage** overrides the default file location.
* **InMemoryOnly** keeps certificates in memory instead of on disk - all certificates are lost on
  every restart. Only use this for testing or development, never in production, and never with more
  than one instance (each instance would end up with its own, different signing key).

Password protection
--------------------

Certificate files are protected with a password so the private key cannot be used by simply copying
the ``.pfx`` file off disk.

* If **CertPassword** is set explicitly, that password is used.
* If it is **not** set, a random password is generated the first time the application starts, and
  persisted next to the certificates as an encrypted ``.certpassword.protected`` file (protected with
  the standard .NET **Data Protection API** - the same key ring configured via
  ``DataProtectionKeysPath``). Every subsequent start reuses that same password. Each installation
  therefore ends up with its own unique password without any manual configuration being required.

.. note::

    When running multiple instances against the same shared storage path, all instances must share
    the same Data Protection key ring (see the ``data-protection`` method in the
    :doc:`../getting-started/configuration` ``Crypto`` section) - otherwise one instance may generate
    a password that another instance cannot decrypt.

Certificate lifecycle
----------------------

A newly created certificate is technically valid for a very long time, but **IdentityServerNET** only
treats certificates created within the last **60 days** as *active* - only those are published at
``jwks_uri`` and eligible to sign new tokens. This 60-day active window is what makes automatic
rotation meaningful: an active window that never changed would provide no rotation at all.

**Signing key vs. validation keys**

* The **most recently created** certificate within the active window is used to *sign* newly issued
  tokens.
* **All** certificates within the active window are published as *validation* keys - so a token signed
  just before a rotation remains verifiable afterwards, instead of suddenly failing validation.

**Automatic rotation, without a restart**

A background service checks periodically whether the newest certificate has become older than a
configurable threshold, and if so, creates a new one - all without requiring the application to
restart. This is tunable via the same ``SigningCredential`` configuration section:

.. code:: javascript

    "SigningCredential": {
      "CheckInterval": "06:00:00",     // optional, default: 6 hours
      "RenewIfOlderThanDays": 60,      // optional, default: 60
      "CacheDuration": "00:15:00"      // optional, default: 15 minutes
    }

* **CheckInterval:** How often the background service checks whether a new certificate is needed.
* **RenewIfOlderThanDays:** A new certificate is created once the current newest one is older than
  this many days.
* **CacheDuration:** The running application does not re-read the certificate list from storage on
  every single token request - it caches the currently active signing/validation keys for this long
  before checking storage again. A newly created certificate therefore becomes the active signing key
  within this cache window, not instantly.

**Cleanup of old certificate files**

Certificate files (or, for ``InMemoryOnly``, in-memory entries) that have fallen out of the active
window for a while are no longer needed and are deleted automatically - specifically, once they are
older than three times the active retention threshold (by default: ``60 * 3 = 180`` days). This grace
period beyond the 60-day active window exists so that a long-lived token issued just before a key
dropped out of the active window can still be validated for a while longer. Cleanup runs as part of
the same periodic background check described above, so old key material does not accumulate on disk
indefinitely.

.. note::

    Certificates are never deleted immediately after they stop being active - only once they are well
    past the point where any reasonably-lived token could still reference them.

Summary
-------

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Setting
     - Effect
   * - ``Storage``
     - Where certificate files are stored. Default: ``<StorageRootPath>/storage/validation``.
   * - ``InMemoryOnly``
     - Keep certificates in memory only (development/testing, single instance only).
   * - ``CertPassword``
     - Password protecting the certificate files. Default: a random password generated once per
       installation.
   * - ``CheckInterval``
     - How often the background service checks for a needed rotation. Default: 6 hours.
   * - ``RenewIfOlderThanDays``
     - Age threshold (in days) that triggers creating a new certificate. Default: 60.
   * - ``CacheDuration``
     - How long a running instance caches the active signing/validation keys before re-reading
       storage. Default: 15 minutes.
