namespace IdentityServerNET.LiteDb.Extensions;
internal static class StringExtensions
{
    static public string EnsureLiteDbParentDirectoryCreated(this string dbPath)
    {
        if (String.IsNullOrEmpty(dbPath))
        {
            throw new ArgumentException("LiteDbUserDb: no connection string defined");
        }

        FileInfo fileInfo = new FileInfo(dbPath);
        if (!fileInfo.Directory.Exists)
        {
            fileInfo.Directory.Create();
        }

        return dbPath;
    }
}
