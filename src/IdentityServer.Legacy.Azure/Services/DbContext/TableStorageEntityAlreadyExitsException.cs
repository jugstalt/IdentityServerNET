using Microsoft.Azure.Cosmos.Table;
using System;

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
