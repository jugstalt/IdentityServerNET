using System;
using System.IO;
using System.Text;

namespace IdentityServer.Legacy.Extensions
{
    public static class CertExtensions
    {
        static public string ParseCertBase64String(this string certString)
        {
            StringBuilder sb = new StringBuilder();

            using (var reader = new StringReader(certString))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (String.IsNullOrWhiteSpace(line) || line.StartsWith("--"))
                    {
                        continue;
                    }

                    sb.Append(line);
                }
            }

            var base64String = sb.ToString();

            #region Verify

            var bytes = Convert.FromBase64String(base64String);

            // ToDo: How to check if public key is valid ???

            #endregion

            return base64String;
        }

    }
}
