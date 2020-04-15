using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Azure.Services.DbContext
{
    public class AzureTableStorage
    {
        static private object thisLocker = new object();
        private string _connectionString;

        public bool Init(string initalParameter)
        {
            _connectionString = initalParameter;
            return true;
        }

        async public Task<bool> CreateTableAsync(string tableName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();

            return true;
        }

        async public Task<bool> InsertEntityAsync(string tableName, ITableEntity entity)
        {
            try
            {
                return await InsertEntity(tableName, entity);
            }
            catch (StorageException ex)
            {
                if (ex.Message.Contains("(409)"))
                    throw new TableStorageEntityAlreadyExitsException(tableName, entity, ex);

                throw ex;
            }
        }

        async private Task<bool> InsertEntity(string tableName, ITableEntity entity, bool mergeIfExists = false)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference(tableName);

            // Create the TableOperation object that inserts the customer entity.
            TableOperation inserOperation = mergeIfExists ?
                TableOperation.InsertOrMerge(entity) :
                TableOperation.Insert(entity);

            // Execute the insert operation.
            await table.ExecuteAsync(inserOperation);

            return true;
        }

        async public Task<bool> TryInsertEntityAsync(string tableName, ITableEntity entity)
        {
            try
            {
                return await InsertEntityAsync(tableName, entity);
            }
            catch
            {
                return false;
            }
        }

        async public Task<IEnumerable<TableEntity>> AllEntitiesAsync(string tableName)
        {
            return await AllTableEntities(tableName, String.Empty);
        }

        async public Task<IEnumerable<TableEntity>> AllEntitiesAsync(string tableName, string partitionKey)
        {
            return await AllTableEntities(tableName, partitionKey);
        }

        async private Task<IEnumerable<TableEntity>> AllTableEntities(string tableName, string partitionKey)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference(tableName);

            TableQuery<TableEntity> query = String.IsNullOrWhiteSpace(partitionKey) ?
                     new TableQuery<TableEntity>() :
                     new TableQuery<TableEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            List<TableEntity> entities = new List<TableEntity>();
            foreach (var entity in await ExecuteQueryAsync(table, query))
            {
                entities.Add(entity);
            }

            return entities.ToArray();
        }

        async private Task<List<TableEntity>> ExecuteQueryAsync(CloudTable table, TableQuery<TableEntity> query)
        {
            List<TableEntity> results = new List<TableEntity>();
            TableQuerySegment<TableEntity> currentSegment = null;

            if (query.TakeCount > 0)
            {
                // Damit Top Query funktioniert
                while (results.Count < query.TakeCount && (currentSegment == null || currentSegment.ContinuationToken != null))
                {
                    currentSegment = await table.ExecuteQuerySegmentedAsync(query, currentSegment != null ? currentSegment.ContinuationToken : null);
                    results.AddRange(currentSegment.Results);
                }
            }
            else
            {
                TableContinuationToken continuationToken = null;
                do
                {
                    currentSegment = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                    continuationToken = currentSegment.ContinuationToken;
                    results.AddRange(currentSegment.Results);
                }
                while (continuationToken != null);
            }

            return results;
        }

        async public Task<TableEntity> EntityAsync(string tableName, string partitionKey, string rowKey)
        {
            TableEntity tableEntity = await Entity(tableName, partitionKey, rowKey);
            if (tableEntity != null)
                return tableEntity;

            return null;
        }

        async public Task<bool> DeleteEntityAsync(string tableName, ITableEntity tableEntity)
        {
            return await DeleteEntityAsync(tableName, tableEntity.PartitionKey, tableEntity.RowKey);
        }

        async public Task<bool> DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
        {
            var tableEntity = await Entity(tableName, partitionKey, rowKey);
            if (tableEntity == null)
                return false;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference(tableName);

            TableOperation deleteOperation = TableOperation.Delete(tableEntity);

            // Execute the operation.
            await table.ExecuteAsync(deleteOperation);

            return true;
        }

        #region Helper

        private CloudTableClient CreateTableClient()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            return tableClient;
        }

        async private Task<TableEntity> Entity(string tableName, string partitionKey, string rowKey, CloudTableClient tableClient = null)
        {
            if (tableClient == null)
                tableClient = CreateTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference(tableName);

            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntity>(partitionKey, rowKey);

            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            if (retrievedResult.Result is TableEntity)
            {
                return (TableEntity)retrievedResult.Result;
            }
            return null;
        }

        #endregion
    }
}
