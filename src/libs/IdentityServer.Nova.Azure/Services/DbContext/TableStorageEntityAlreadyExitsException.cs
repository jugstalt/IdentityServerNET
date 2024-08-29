using Azure.Data.Tables;
using System;

namespace IdentityServer.Nova.Azure.Services.DbContext;

public class TableStorageEntityAlreadyExitsException : Exception
{
    public TableStorageEntityAlreadyExitsException(string tableName, ITableEntity entity, Exception innerException)
        : base("Entity already exists", innerException)
    {

    }
}
