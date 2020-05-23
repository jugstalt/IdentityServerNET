using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Azure.Services.DbContext
{
    public class TableStorageEntityAlreadyExitsException : Exception
    {
        public TableStorageEntityAlreadyExitsException(string tableName, ITableEntity entity, Exception innerException)
            : base("Entity already exists", innerException)
        {

        }
    }
}
