using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IdentityServer.Legacy.UserInteraction
{
    [Flags]
    public enum EditorType
    {
        Editable = 1,
        ReadOnly = 2,
        HiddenInput = 8,
        Required = 16,
        EmailAddress = 32,
        Phone = 64,
        Url = 128,
        Password = 256,
        Date = 512,
        Time = 1024
    }

    public class EditorInfo
    {
        public EditorInfo() 
        {
            this.EditorType = EditorType.Editable;
        }
        public EditorInfo(string name, Type propertyType)
            :this()
        {
            this.Name = name;
            this.DisplayName = name;
            this.PropertyType = propertyType;
            this.Category = "Claims";
        }

        public EditorInfo(string name, string displayName, Type propertyType)
            : this(name, propertyType)
        {
            this.DisplayName = displayName;
        }

        public EditorInfo(string name, string displayName, Type propertyType, string category)
            : this(name, displayName, propertyType)
        {
            this.Category = category;
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Type PropertyType { get; set; }

        public string Category { get; set; }
        public EditorType EditorType { get; set; }

        public string ClaimName { get; set; }

        public string RegexPattern { get; set; }

        public bool IsValid(string value)
        {
            if (EditorType.HasFlag(EditorType.Required) && String.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!String.IsNullOrWhiteSpace(value))
            {
                if (this.EditorType.HasFlag(EditorType.EmailAddress) &&
                    !value.CheckRegex(ValidationExtensions.EmailAddressRegex))
                {
                    return false;
                }

                if (this.EditorType.HasFlag(EditorType.Phone) &&
                    !value.CheckRegex(ValidationExtensions.PhoneNumberRegex))
                {
                    return false;
                }

                if (!value.CheckRegex(RegexPattern))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
