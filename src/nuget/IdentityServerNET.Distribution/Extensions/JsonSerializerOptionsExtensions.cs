using IdentityServerNET.Distribution.Json.Converts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdentityServerNET.Distribution.Extensions;

static public class JsonSerializerOptionsExtensions
{
    static public JsonSerializerOptions AddHttpInvokerDefaults(this JsonSerializerOptions options)
    {
        if (options.Converters != null)
        {
            options.Converters.Add(new ClaimConverter()); ;
        }

        options.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Allow "NaN" with numbers
        options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;

        return options;
    }
}
