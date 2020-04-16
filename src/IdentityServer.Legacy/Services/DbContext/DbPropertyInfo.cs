using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.DbContext
{
    public enum DbPropertyInfoAction
    {
        Editable = 1,
        ReadOnly = 2
    }

    public class DbPropertyInfo
    {
        public DbPropertyInfo() 
        {
            this.Action = DbPropertyInfoAction.Editable;
        }
        public DbPropertyInfo(string name, Type propertyType)
            :this()
        {
            this.Name = name;
            this.DisplayName = name;
            this.PropertyType = propertyType;
            this.Category = "Claims";
        }

        public DbPropertyInfo(string name, string displayName, Type propertyType)
            : this(name, propertyType)
        {
            this.DisplayName = displayName;
        }

        public DbPropertyInfo(string name, string displayName, Type propertyType, string category)
            : this(name, displayName, propertyType)
        {
            this.Category = category;
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public Type PropertyType { get; set; }

        public string Category { get; set; }
        public DbPropertyInfoAction Action { get; set; }

        public string ClaimName { get; set; }

        public string RegexPattern { get; set; }
        
        public bool IsRequired { get; set; }

        public bool IsValid(string value)
        {
            if (IsRequired && String.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if(!value.CheckRegex(RegexPattern))
            {
                return false;
            }

            return true;
        }
    }
}
