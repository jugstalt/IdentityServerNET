using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityServer.Nova.Distribution.Json.Converts;

namespace IdentityServer.Nova.Distribution.Extensions;

static public class JsonSerializerOptionsExtensions
{
    static public JsonSerializerOptions AddHttpInvokerDefaults(this JsonSerializerOptions options)
    {
        if (options.Converters != null)
        {
            options.Converters.Add(new ClaimConverter());;
        }

        options.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Allow "NaN" with numbers
        options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;

        return options;
    }
}
