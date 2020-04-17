using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IdentityServer.Legacy.UserInteraction
{
    static public class RazorHelpers
    {
        static public object GetPropertyValue(object instance, EditorInfo dbPropertyInfo)
        {
            if (instance == null)
                return null;

            var propertyInfo = instance.GetType().GetProperty(dbPropertyInfo.Name);
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(instance);
            }

            if (!String.IsNullOrWhiteSpace(dbPropertyInfo.ClaimName))
            {
                if (instance is ApplicationUser)
                {
                    var claim = (instance as ApplicationUser).Claims
                                    .Where(c => c.Type == dbPropertyInfo.ClaimName)
                                    .FirstOrDefault();

                    if (claim != null)
                    {
                        // ToDo: Change Type (claim.ValueType)
                        return claim.Value;
                    }
                }
            }

            return null;
        }

        // To lazy to write a TagHelper Class for this -> maybe later
        static public string GetPropertyAttributes(EditorInfo dbPropertyInfo)
        {
            StringBuilder sb = new StringBuilder();

            string type = "text";

            #region Type from EditorType 

            if (dbPropertyInfo.EditorType.HasFlag(EditorType.EmailAddress))
            {
                type = "email";
                sb.Append($@" data-val-email=""The { dbPropertyInfo.DisplayName } field is not a valid e-mail address""");
            }
            else if (dbPropertyInfo.EditorType.HasFlag(EditorType.Phone))
            {
                type = "tel";
                sb.Append($@" data-val-phone=""The { dbPropertyInfo.DisplayName } field is not a valid phone number""");
            }
            else if (dbPropertyInfo.EditorType.HasFlag(EditorType.Password))
            {
                type = "password";
            }
            else if (dbPropertyInfo.EditorType.HasFlag(EditorType.Date))
            {
                type = "date";
            }
            else if (dbPropertyInfo.EditorType.HasFlag(EditorType.Time))
            {
                type = "date";
            }
            else if (dbPropertyInfo.EditorType.HasFlag(EditorType.Url))
            {
                type = "url";
                sb.Append($@" data-val-phone=""The { dbPropertyInfo.DisplayName } field is not a valid fully-qualified http, https, or ftp URL.""");
            }

            #endregion

            #region Type from PropertyType

            if (type == "text")
            {
                if (dbPropertyInfo.PropertyType == typeof(DateTime))
                    type = "date";
                if (dbPropertyInfo.PropertyType == typeof(int))
                    type = "number";
                if (dbPropertyInfo.PropertyType == typeof(double) ||
                    dbPropertyInfo.PropertyType == typeof(float) ||
                    dbPropertyInfo.PropertyType == typeof(decimal))
                {
                    sb.Append(@" step=""0.01""");
                    type = "number";
                }
                if (dbPropertyInfo.PropertyType == typeof(bool))
                    type = "checkbox";
            }

            #endregion

            sb.Append($@" type=""{ type }""");

            if (dbPropertyInfo.EditorType.HasFlag(EditorType.Required))
                sb.Append($@" data-val-required=""The { dbPropertyInfo.DisplayName } field is required.""");

            if (sb.ToString().Contains("data-val-"))
            {
                sb.Append(@" data-val=""true""");
            }

            if (dbPropertyInfo.EditorType.HasFlag(EditorType.ReadOnly))
                sb.Append(" readonly");

            sb.Append($@" id=""{ dbPropertyInfo.Name.Replace(".", "_") }""");

            return sb.ToString();
        }
    }
}
