Signing-Zertifikate
===================

**IdentityServerNET** signiert jedes ausgestellte Token (ID-Token, Access-Token, ...) mit einem
asymmetrischen X.509-Zertifikat und veröffentlicht die zugehörigen öffentlichen Schlüssel über die
``jwks_uri`` aus dem Discovery-Dokument, damit Clients und APIs Tokens verifizieren können. Diese Seite
beschreibt, wie diese Zertifikate erzeugt, gespeichert, rotiert und aufgeräumt werden.

Speicherung
-----------

Standardmäßig werden Zertifikate als passwortgeschützte ``.pfx``-Dateien unter
``<StorageRootPath>/storage/validation`` gespeichert (siehe :doc:`../getting-started/configuration`).
Das ist der Standard, selbst ohne jede explizite ``SigningCredential``-Konfiguration.

.. code:: javascript

    "SigningCredential": {
      "Storage": "c:\\apps\\identityserver-net\\storage\\validation",  // optionaler Override
      "CertPassword": "...",                                          // optional
      "InMemoryOnly": true                                            // optional, default: false
    }

* **Storage** überschreibt den Standard-Speicherort.
* **InMemoryOnly** hält die Zertifikate nur im Speicher statt auf der Festplatte — bei jedem Neustart
  gehen dann alle Zertifikate verloren. Nur für Tests oder Entwicklung verwenden, niemals in Produktion,
  und niemals mit mehr als einer Instanz (jede Instanz würde sonst ihren eigenen, unterschiedlichen
  Signing-Key bekommen).

Passwortschutz
--------------

Die Zertifikatsdateien werden mit einem Passwort geschützt, damit der private Schlüssel nicht einfach
durch Kopieren der ``.pfx``-Datei von der Festplatte nutzbar wird.

* Ist **CertPassword** explizit gesetzt, wird dieses Passwort verwendet.
* Ist es **nicht** gesetzt, wird beim ersten Start der Anwendung ein zufälliges Passwort erzeugt und
  neben den Zertifikaten als verschlüsselte ``.certpassword.protected``-Datei gespeichert (geschützt
  über die Standard-.NET-**Data Protection API** — denselben Schlüsselkreis, der auch über
  ``DataProtectionKeysPath`` konfiguriert wird). Jeder weitere Start verwendet dasselbe Passwort wieder.
  Jede Installation erhält damit automatisch ihr eigenes, einzigartiges Passwort, ohne manuelle
  Konfiguration.

.. note::

    Werden mehrere Instanzen gegen denselben, geteilten Speicherpfad betrieben, müssen alle Instanzen
    denselben Data-Protection-Schlüsselkreis verwenden (siehe die Methode ``data-protection`` im
    ``Crypto``-Abschnitt unter :doc:`../getting-started/configuration`) — sonst kann es passieren, dass
    eine Instanz ein Passwort erzeugt, das eine andere Instanz nicht entschlüsseln kann.

Lebenszyklus der Zertifikate
-----------------------------

Ein neu erzeugtes Zertifikat ist technisch sehr lange gültig, aber **IdentityServerNET** behandelt nur
Zertifikate, die innerhalb der letzten **60 Tage** erzeugt wurden, als *aktiv* — nur diese werden über
die ``jwks_uri`` veröffentlicht und können neue Tokens signieren. Dieses 60-Tage-Fenster ist es, was
automatische Rotation überhaupt sinnvoll macht: ein Fenster, das sich nie ändert, würde keinerlei
Rotation bedeuten.

**Signing-Key vs. Validation-Keys**

* Das **zuletzt erzeugte** Zertifikat innerhalb des aktiven Fensters wird zum *Signieren* neu
  ausgestellter Tokens verwendet.
* **Alle** Zertifikate innerhalb des aktiven Fensters werden als *Validation-Keys* veröffentlicht — ein
  Token, das kurz vor einer Rotation signiert wurde, bleibt damit danach weiterhin verifizierbar, statt
  plötzlich die Validierung zu verlieren.

**Automatische Rotation, ohne Neustart**

Ein Hintergrunddienst prüft periodisch, ob das neueste Zertifikat einen konfigurierbaren
Schwellenwert überschritten hat, und erzeugt gegebenenfalls ein neues — ganz ohne dass die Anwendung
neu gestartet werden muss. Das lässt sich über denselben ``SigningCredential``-Konfigurationsabschnitt
einstellen:

.. code:: javascript

    "SigningCredential": {
      "CheckInterval": "06:00:00",     // optional, default: 6 Stunden
      "RenewIfOlderThanDays": 60,      // optional, default: 60
      "CacheDuration": "00:15:00"      // optional, default: 15 Minuten
    }

* **CheckInterval:** Wie oft der Hintergrunddienst prüft, ob ein neues Zertifikat benötigt wird.
* **RenewIfOlderThanDays:** Ein neues Zertifikat wird erzeugt, sobald das aktuell neueste älter als
  diese Anzahl Tage ist.
* **CacheDuration:** Die laufende Anwendung liest die Zertifikatsliste nicht bei jeder einzelnen
  Token-Anfrage neu aus dem Speicher — die aktuell aktiven Signing-/Validation-Keys werden für diese
  Dauer zwischengespeichert, bevor erneut nachgesehen wird. Ein neu erzeugtes Zertifikat wird also
  innerhalb dieses Cache-Fensters zum aktiven Signing-Key, nicht sofort.

**Aufräumen alter Zertifikatsdateien**

Zertifikatsdateien (bzw. bei ``InMemoryOnly`` die entsprechenden Einträge im Speicher), die schon eine
Weile außerhalb des aktiven Fensters liegen, werden nicht mehr benötigt und automatisch gelöscht — genauer
gesagt, sobald sie älter sind als das Dreifache der Aktiv-Schwelle (Standard: ``60 * 3 = 180`` Tage).
Diese Karenzzeit über das 60-Tage-Fenster hinaus existiert, damit ein langlebiges Token, das kurz vor
dem Verlassen des aktiven Fensters ausgestellt wurde, noch eine Weile validiert werden kann. Das
Aufräumen läuft als Teil derselben periodischen Hintergrundprüfung, die oben beschrieben ist — altes
Schlüsselmaterial sammelt sich damit nicht unbegrenzt auf der Festplatte an.

.. note::

    Zertifikate werden nie unmittelbar gelöscht, sobald sie nicht mehr aktiv sind — sondern erst, wenn
    sie deutlich über den Punkt hinaus sind, an dem ein realistisch langlebiges Token sie noch
    referenzieren könnte.

Zusammenfassung
----------------

.. list-table::
   :widths: 30 70
   :header-rows: 1

   * - Einstellung
     - Wirkung
   * - ``Storage``
     - Wo die Zertifikatsdateien gespeichert werden. Standard: ``<StorageRootPath>/storage/validation``.
   * - ``InMemoryOnly``
     - Zertifikate nur im Speicher halten (Entwicklung/Test, nur eine einzelne Instanz).
   * - ``CertPassword``
     - Passwort, das die Zertifikatsdateien schützt. Standard: ein einmal pro Installation zufällig
       erzeugtes Passwort.
   * - ``CheckInterval``
     - Wie oft der Hintergrunddienst prüft, ob eine Rotation nötig ist. Standard: 6 Stunden.
   * - ``RenewIfOlderThanDays``
     - Alters-Schwellenwert (in Tagen), der die Erzeugung eines neuen Zertifikats auslöst. Standard: 60.
   * - ``CacheDuration``
     - Wie lange eine laufende Instanz die aktiven Signing-/Validation-Keys zwischenspeichert, bevor
       erneut aus dem Speicher gelesen wird. Standard: 15 Minuten.
