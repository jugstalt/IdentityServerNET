using System;
using System.Linq;
using System.Text;

namespace IdentityServerNET.Models.UserInteraction;

static public class RazorHelpers
{
    static public object? GetPropertyValue(object instance, EditorInfo editorInfo)
    {
        if (instance == null)
        {
            return null;
        }

        object? propertyValue = null;
        var propertyInfo = instance.GetType().GetProperty(editorInfo.Name);
        if (propertyInfo != null)
        {
            propertyValue = propertyInfo.GetValue(instance);
        }
        else if (!String.IsNullOrWhiteSpace(editorInfo.ClaimName))
        {
            if (instance is ApplicationUser)
            {
                var claim = (instance as ApplicationUser)?.Claims
                                .Where(c => c.Type == editorInfo.ClaimName)
                                .FirstOrDefault();

                if (claim != null)
                {
                    // ToDo: Change Type (claim.ValueType)
                    propertyValue = claim.Value;
                }
            }
        }

        if (editorInfo.PropertyType?.IsArray == true && propertyValue != null)
        {
            if (propertyValue is string)
            {
                propertyValue = ((string)propertyValue).Split('\n');
            }
            else
            {
                propertyValue = ((object[])propertyValue).Select(s => s?.ToString()).ToArray();
            }

            propertyValue = String.Join(Environment.NewLine, (string[])propertyValue);
        }

        return propertyValue;
    }

    // To lazy to write a TagHelper Class for this -> maybe later
    static public string GetPropertyAttributes(EditorInfo editorInfo)
    {
        StringBuilder sb = new StringBuilder();

        string type = "text";

        #region Type from EditorType 

        if (editorInfo.EditorType.HasFlag(EditorType.EmailAddress))
        {
            type = "email";
            sb.Append($@" data-val-email=""The {editorInfo.DisplayName} field is not a valid e-mail address""");
        }
        else if (editorInfo.EditorType.HasFlag(EditorType.Phone))
        {
            type = "tel";
            sb.Append($@" data-val-phone=""The {editorInfo.DisplayName} field is not a valid phone number""");
        }
        else if (editorInfo.EditorType.HasFlag(EditorType.Password))
        {
            type = "password";
        }
        else if (editorInfo.EditorType.HasFlag(EditorType.Date))
        {
            type = "date";
        }
        else if (editorInfo.EditorType.HasFlag(EditorType.Time))
        {
            type = "date";
        }
        else if (editorInfo.EditorType.HasFlag(EditorType.Url))
        {
            type = "url";
            sb.Append($@" data-val-phone=""The {editorInfo.DisplayName} field is not a valid fully-qualified http, https, or ftp URL.""");
        }

        #endregion

        #region Type from PropertyType

        if (type == "text")
        {
            if (editorInfo.PropertyType == typeof(DateTime))
            {
                type = "date";
            }

            if (editorInfo.PropertyType == typeof(int))
            {
                type = "number";
            }

            if (editorInfo.PropertyType == typeof(double) ||
                editorInfo.PropertyType == typeof(float) ||
                editorInfo.PropertyType == typeof(decimal))
            {
                sb.Append(@" step=""0.01""");
                type = "number";
            }
            if (editorInfo.PropertyType == typeof(bool))
            {
                type = "checkbox";
            }
        }

        #endregion

        sb.Append($@" type=""{type}""");

        if (editorInfo.EditorType.HasFlag(EditorType.Required))
        {
            sb.Append($@" data-val-required=""The {editorInfo.DisplayName} field is required.""");
        }

        if (sb.ToString().Contains("data-val-"))
        {
            sb.Append(@" data-val=""true""");
        }

        if (editorInfo.EditorType.HasFlag(EditorType.ReadOnly))
        {
            sb.Append(" readonly");
        }

        sb.Append($@" id=""{editorInfo.Name.Replace(".", "_")}""");

        if (editorInfo.PropertyType?.IsArray == true)
        {
            sb.Append($" rows='5'");
        }

        return sb.ToString();
    }
}
