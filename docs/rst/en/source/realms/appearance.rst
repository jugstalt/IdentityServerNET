Realm Appearance (Branding)
============================

Both the system administrator and each realm admin have access to the same **Appearance** admin page —
but they edit different scopes:

* The **system administrator's** ``Appearance`` tile edits the **global** default theme, used whenever
  no realm applies.
* A **realm admin's** ``Appearance`` tile (visible in their own admin area) edits **only their realm's**
  branding. It has no effect on the global theme or on any other realm.

**IdentityServerNET** automatically shows a realm's branding whenever the current login, consent or
account-management page belongs to that realm — resolved either from the client being logged into, or
from the authenticated user's e-mail domain. If a realm has not customized a particular setting, the
global default is used as a fallback for that value.

The Appearance page is organized into the following sections:

Application Title
------------------

A single text field shown in the browser tab title, the navbar brand, and the heading of the login card.
Leave empty to fall back to the configured default (global) or, for a realm, to the global application
title.

Colors
------

Four color pickers, each falling back to a sensible default when unset:

.. list-table::
   :widths: 30 20 50
   :header-rows: 1

   * - Setting
     - Default
     - Used for
   * - Primary Color
     - ``#0094ff``
     - Navbar & sidebar background, button backgrounds, login page gradient.
   * - Navbar & Button Text Color
     - ``#ffffff``
     - Text on the navbar, buttons and active sidebar items.
   * - Heading Color
     - ``#1a1a1a``
     - H1–H4 headings, card titles, form labels; also inactive sidebar text.
   * - Body Text Color
     - ``#1a1a1a``
     - Paragraphs, table cells, labels; fallback for sidebar text if Heading Color is unset.

Each color (other than Primary) can be individually reset to the default with its ``Reset`` button.

Logo
----

Uploads a PNG or JPEG logo, shown in the navbar brand and (for realm clients) alongside the login card.
Uploading a new file replaces the existing logo; ``Remove Logo`` clears it back to the default (no logo).

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Constraint
     - Value
   * - Accepted formats
     - PNG, JPEG, WebP (SVG is rejected — it may contain scripts)
   * - Maximum file size
     - 512 KB
   * - Maximum dimensions
     - 4096 × 4096 px

Background Images
------------------

One or more PNG/JPEG images shown as a randomly selected background on the login page. Multiple files
can be uploaded at once; each is added to the existing set. Individual images can be removed, or
``Remove All`` clears the whole set back to the built-in default background.

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Constraint
     - Value
   * - Accepted formats
     - PNG, JPEG, WebP (SVG is rejected)
   * - Maximum file size (per image)
     - 2 MB
   * - Maximum dimensions
     - 4096 × 4096 px

.. note::

    Background images are larger and more decorative than a logo, so they are allowed a higher upload
    limit (2 MB) than the logo (512 KB). Both limits apply identically at the global and at the
    realm-scoped Appearance page.

.. note::

    Every upload is validated by inspecting the file's magic bytes **and** actually decoding it as an
    image — a renamed non-image file or a corrupted file is rejected with an error message rather than
    silently stored.
