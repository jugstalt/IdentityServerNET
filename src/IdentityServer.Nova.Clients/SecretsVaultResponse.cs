using System;

namespace IdentityServer.Nova.Clients;

public class SecretsVaultResponse
{
    public bool Success { get; set; }

    public string ErrorMessage { get; set; }

    public string Path { get; set; }

    public VaultSecretVersion Secret { get; set; }

    public string GetValue()
    {
        if (this.Success == false)
        {
            throw new Exception($"GetSecret causes an error: {this.ErrorMessage}");
        }

        return this.Secret?.Secret;
    }

    #region Classes

    public class VaultSecretVersion
    {
        public long VersionTimeStamp { get; set; }

        public string Secret { get; set; }
    }

    #endregion
}
