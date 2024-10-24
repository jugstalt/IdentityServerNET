using IdentityServerNET.Abstractions.Serialize;
using Newtonsoft.Json;

namespace IdentityServerNET.Services.Serialize;

public class JsonBlobSerializer : IBlobSerializer
{
    public JsonBlobSerializer()
    {
        this.JsonFormatting = Formatting.None;
    }

    public T DeserializeObject<T>(string text)
    {
        return JsonConvert.DeserializeObject<T>(text);
    }

    public string SerializeObject(object obj)
    {
        return JsonConvert.SerializeObject(obj, JsonFormatting);
    }

    public Formatting JsonFormatting { get; set; }
}
