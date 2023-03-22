using IdentityServer.Legacy.Services.Cryptography;

namespace IdentityServer.Legacy.MongoDb
{
    static class Extensions
    {
        static public string ClientIdToHexId(this string name, ICryptoService cryptoService = null)
        {
            return $"client.{name.NameToHexId(cryptoService)}";
        }

        static public bool IsValidClientHexId(this string id)
        {
            return id.StartsWith("client.");
        }

        static public string ApiNameToHexId(this string name, ICryptoService cryptoService = null)
        {
            return $"api.{name.NameToHexId(cryptoService)}";
        }

        static public bool IsValidApiResourceHexId(this string id)
        {
            return id.StartsWith("api.");
        }

        static public string IdentityNameToHexId(this string name, ICryptoService cryptoService = null)
        {
            return $"identity.{name.NameToHexId(cryptoService)}";
        }

        static public bool IsValidIdentityResourceHexId(this string id)
        {
            return id.StartsWith("identity.");
        }
    }
}
