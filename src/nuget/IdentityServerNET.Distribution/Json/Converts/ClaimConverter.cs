using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdentityServerNET.Distribution.Json.Converts;

public class ClaimConverter : JsonConverter<Claim>
{
    public override Claim Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        string? type = null;
        string? value = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;

                reader.Read();

                switch (propertyName)
                {
                    case nameof(ClaimDTO.Type):
                        type = reader.GetString();
                        break;
                    case nameof(ClaimDTO.Vaule):
                        value = reader.GetString();
                        break;
                    default:
                        throw new JsonException($"Unexpected property: {propertyName}");
                }
            }
        }

        if (type == null || value == null)
        {
            throw new JsonException("Missing required properties for Claim");
        }

        return new Claim(type, value);
    }

    public override void Write(Utf8JsonWriter writer, Claim value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(nameof(ClaimDTO.Type), value.Type);
        writer.WriteString(nameof(ClaimDTO.Vaule), value.Value);
        writer.WriteEndObject();
    }

    #region DTO

    public record ClaimDTO(string Type, string Vaule);

    #endregion
}