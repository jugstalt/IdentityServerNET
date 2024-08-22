using IdentityServer.Nova.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Certificates;

public class IndexModel : SecurePageModel
{
    public IndexModel()
    {

    }

    [BindProperty]
    [DisplayName("Certificate (File) Name")]
    public string CertName { get; set; } = "client-secret";

    [BindProperty]
    [DisplayName("Subject (CN)")]
    public string CN { get; set; } = "client-secret";

    [BindProperty]
    [DisplayName("Password (optional)")]
    public string Password { get; set; } = "";

    [BindProperty]
    [DisplayName("Expire Days")]
    public int ExpireDays { get; set; } = 365;

    public void OnGet()
    {
    }

    public Task<IActionResult> OnPostAsync()
    {
        var zipStream = new MemoryStream();

        return SecureHandlerAsync(() =>
        {
            var ecdsa = RSA.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"cn={CN}", ecdsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(ExpireDays));

            var pfxBytes = cert.Export(X509ContentType.Pfx, Password);
            var crtBytes = System.Text.Encoding.UTF8.GetBytes(
                            $"""
                            -----BEGIN CERTIFICATE-----
                            {Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)}
                            -----END CERTIFICATE-----
                            """);
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                var pfxEntry = archive.CreateEntry($"{CertName}.pfx");
                using (var entryStream = pfxEntry.Open())
                {
                    entryStream.Write(pfxBytes, 0, pfxBytes.Length);
                }

                var crtEntry = archive.CreateEntry($"{CertName}.crt");
                using (var entryStream = crtEntry.Open())
                {
                    entryStream.Write(crtBytes, 0, crtBytes.Length);
                }
            }

            return Task.CompletedTask;
        }
        , onFinally: () => File(zipStream.ToArray(), "application/zip", $"{CertName}.zip")
        , successMessage: ""
        , onException: (ex) => Page()
        );
    }
}
