# Installing a Self-Signed SSL Certificate in the Trusted Certificate Store on Windows

To create a self-signed certificate and import it into the trusted certificate store on Windows, follow these steps:

## 1. Copy Certificate

Copy the certificate from the docker image (`app/nova-dev.pfx`) to your windows machine.
You can use the **Docker for Windows** UI for this.

## 2. Open the Certificates Management Console

- Press `Win + R`, type `mmc`, and press Enter.
- Go to `File` > `Add/Remove Snap-in...`.
- Select `Certificates` and add it to the right pane. Choose `Computer account` and then `Local computer`.

## 3. Import the Certificate

- Go to `Trusted Root Certification Authorities` > `Certificates`.
- Right-click on `Certificates` and select `All Tasks` > `Import...`.
- Follow the wizard to import the PFX certificate:
  - Select the `certificate.pfx` file.
  - Enter the password you set when exporting the PFX certificate.
  - Ensure the certificate is imported into the `Trusted Root Certification Authorities`.

## 4. Verify in the Browser

- Open your browser and navigate to your application (e.g., `https://localhost:8443`).
- The browser should now recognize the certificate as trusted and not display any security warnings.

By following these steps, you can ensure that your self-signed SSL certificate is installed in the trusted certificate store on Windows and that your ASP.NET Core application runs securely over HTTPS.
