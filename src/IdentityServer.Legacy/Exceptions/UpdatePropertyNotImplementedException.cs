using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Exceptions
{
    public class UpdatePropertyNotImplementedException : Exception
    {
        public UpdatePropertyNotImplementedException(string propertyName)
            : base($"Update property '{ propertyName }' is not implemented in the datebase context") 
        { 
        }
    }
}
