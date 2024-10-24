namespace IdentityServerNET.Models;

public class VaultSecretVersion
{
    public long VersionTimeStamp { get; set; }

    public string Secret { get; set; } = "";
}
