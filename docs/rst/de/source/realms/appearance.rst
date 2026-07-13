Realm Appearance (Branding)
============================

Sowohl der Systemadministrator als auch jeder Realm-Admin haben Zugriff auf dieselbe
**Appearance**-Admin-Seite — bearbeiten dabei aber unterschiedliche Bereiche:

* Die ``Appearance``-Kachel des **Systemadministrators** bearbeitet das **globale** Standard-Theme, das
  greift, wenn kein Realm zutrifft.
* Die ``Appearance``-Kachel eines **Realm-Admins** (im eigenen Admin-Bereich sichtbar) bearbeitet
  **ausschließlich das Branding seines Realms**. Sie hat keine Auswirkung auf das globale Theme oder auf
  andere Realms.

**IdentityServerNET** zeigt das Branding eines Realms automatisch, sobald die aktuelle Login-,
Consent- oder Account-Verwaltungsseite zu diesem Realm gehört — ermittelt entweder über den Client, bei
dem sich der Benutzer anmeldet, oder über die E-Mail-Domain des angemeldeten Benutzers. Ist ein
bestimmter Wert für den Realm nicht angepasst, greift als Fallback der globale Standardwert.

Die Appearance-Seite gliedert sich in folgende Bereiche:

Application Title
------------------

Ein einzelnes Textfeld, das im Browser-Tab-Titel, im Navbar-Markennamen und in der Überschrift der
Login-Karte angezeigt wird. Leer lassen, um auf den konfigurierten Standard (global) bzw. für einen
Realm auf den globalen Application Title zurückzufallen.

Colors
------

Vier Farbwähler, jeweils mit sinnvollem Fallback, falls nicht gesetzt:

.. list-table::
   :widths: 30 20 50
   :header-rows: 1

   * - Einstellung
     - Standard
     - Verwendet für
   * - Primary Color
     - ``#0094ff``
     - Navbar- & Sidebar-Hintergrund, Button-Hintergründe, Farbverlauf der Login-Seite.
   * - Navbar & Button Text Color
     - ``#ffffff``
     - Text in Navbar, Buttons und aktiven Sidebar-Einträgen.
   * - Heading Color
     - ``#1a1a1a``
     - H1–H4-Überschriften, Card-Titel, Formular-Labels; zusätzlich inaktiver Sidebar-Text.
   * - Body Text Color
     - ``#1a1a1a``
     - Absätze, Tabellenzellen, Labels; Fallback für Sidebar-Text, falls Heading Color nicht gesetzt ist.

Jede Farbe (außer Primary) kann über den zugehörigen ``Reset``-Button einzeln auf den Standard
zurückgesetzt werden.

Logo
----

Lädt ein PNG- oder JPEG-Logo hoch, das in der Navbar sowie (bei Realm-Clients) neben der Login-Karte
angezeigt wird. Ein neuer Upload ersetzt das bestehende Logo; ``Remove Logo`` setzt es auf den Standard
(kein Logo) zurück.

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Vorgabe
     - Wert
   * - Akzeptierte Formate
     - PNG, JPEG, WebP (SVG wird abgelehnt — kann Skripte enthalten)
   * - Maximale Dateigröße
     - 512 KB
   * - Maximale Abmessungen
     - 4096 × 4096 px

Background Images
------------------

Ein oder mehrere PNG-/JPEG-Bilder, die zufällig ausgewählt als Hintergrund der Login-Seite angezeigt
werden. Es können mehrere Dateien gleichzeitig hochgeladen werden; jede wird der bestehenden Menge
hinzugefügt. Einzelne Bilder lassen sich entfernen, oder ``Remove All`` setzt die gesamte Menge auf den
eingebauten Standard-Hintergrund zurück.

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Vorgabe
     - Wert
   * - Akzeptierte Formate
     - PNG, JPEG, WebP (SVG wird abgelehnt)
   * - Maximale Dateigröße (pro Bild)
     - 2 MB
   * - Maximale Abmessungen
     - 4096 × 4096 px

.. note::

    Hintergrundbilder sind größer und dekorativer als ein Logo, daher ist für sie ein höheres
    Upload-Limit (2 MB) erlaubt als für das Logo (512 KB). Beide Limits gelten identisch auf der
    globalen wie auf der Realm-gebundenen Appearance-Seite.

.. note::

    Jeder Upload wird geprüft, indem sowohl die Magic Bytes der Datei untersucht werden **als auch**
    tatsächlich versucht wird, sie als Bild zu dekodieren — eine umbenannte Nicht-Bild-Datei oder eine
    beschädigte Datei wird mit einer Fehlermeldung abgelehnt statt stillschweigend gespeichert zu werden.
