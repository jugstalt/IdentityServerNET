namespace IdentityServer.Nova.Abstractions.Services.Serialize;

public interface IBlobSerializer
{
    string SerializeObject(object obj);
    T DeserializeObject<T>(string text);
}
