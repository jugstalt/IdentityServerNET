using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServerNET.Models.UserInteraction;

public class EditorInfoCollection
{
    public EditorInfo[]? EditorInfos { get; set; }

    public bool Validate(IEnumerable<KeyValuePair<string, StringValues>> formCollection, out string validationMessage)
    {
        validationMessage = "";

        if (EditorInfos == null)
        {
            return true;
        }

        foreach (var keyValuePair in formCollection)
        {
            var editorInfo = EditorInfos.Where(e => e.Name == keyValuePair.Key).FirstOrDefault();
            if (editorInfo == null || editorInfo.EditorType.HasFlag(EditorType.ReadOnly))
            {
                continue;
            }

            if (!editorInfo.IsValid(keyValuePair.Value.ToString()))
            {
                validationMessage += $"{Environment.NewLine}The {editorInfo.DisplayName} field is not valid.";
            }
        }

        return String.IsNullOrEmpty(validationMessage);
    }

    public string ValidationMessage { get; set; } = "";
}
