using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Azure.Services.DbContext;

public class AzureTableStorage<T> where T : class, ITableEntity, new()
{
    private string _connectionString;

    public bool Init(string initalParameter)
    {
        _connectionString = initalParameter;
        return true;
    }

    async public Task<bool> CreateTableAsync(string tableName)
    {
        TableServiceClient tableServiceClient = new TableServiceClient(_connectionString);

        try
        {
            TableClient tableClient = tableServiceClient.GetTableClient(tableName);
            await tableClient.CreateIfNotExistsAsync();

            return true;
        }
        catch (RequestFailedException ex)
        {
            Console.WriteLine($"Error on Create Table: {ex.Message}");
            return false;
        }
    }

    async public Task<bool> InsertEntityAsync(string tableName, T entity)
    {
        try
        {
            // Create a TableServiceClient using the connection string
            TableServiceClient tableServiceClient = new TableServiceClient(_connectionString);

            // Get a reference to the table
            TableClient tableClient = tableServiceClient.GetTableClient(tableName);

            // Insert the entity into the table
            await tableClient.AddEntityAsync(entity);

            return true;
        }
        catch (RequestFailedException ex)
        {
            // Check if the entity already exists (conflict error)
            if (ex.Status == 409) // 409 is the HTTP status code for Conflict
            {
                throw new TableStorageEntityAlreadyExitsException(tableName, entity, ex);
            }

            // Rethrow the original exception for other error cases
            throw;
        }
    }

    async private Task<bool> InsertEntity(string tableName, T entity, bool mergeIfExists = false)
    {
        // Create the TableServiceClient using the connection string
        TableServiceClient tableServiceClient = new TableServiceClient(_connectionString);

        // Get a reference to the table
        TableClient tableClient = tableServiceClient.GetTableClient(tableName);

        try
        {
            // Choose the appropriate operation based on mergeIfExists parameter
            if (mergeIfExists)
            {
                // If the entity exists, merge it; otherwise, insert it
                await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Merge);
            }
            else
            {
                // Insert the entity; if the entity already exists, this will throw an exception
                await tableClient.AddEntityAsync(entity);
            }

            return true;
        }
        catch (RequestFailedException ex)
        {
            // Handle the error if the entity already exists or other errors occur
            Console.WriteLine($"An error occurred while inserting the entity: {ex.Message}");
            return false;
        }
    }


    async public Task<bool> TryInsertEntityAsync(string tableName, T entity)
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

    async public Task<bool> MergeEntityAsync(string tableName, T entity)
    {
        // Create the TableServiceClient using the connection string
        TableServiceClient tableServiceClient = new TableServiceClient(_connectionString);

        // Get a reference to the table
        TableClient tableClient = tableServiceClient.GetTableClient(tableName);

        try
        {
            // Set the ETag to "*" to indicate a merge operation
            entity.ETag = ETag.All;

            // Perform the merge operation using UpsertEntityAsync with Merge mode
            await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Merge);

            return true;
        }
        catch (RequestFailedException ex)
        {
            // Handle any errors that occur during the merge operation
            Console.WriteLine($"An error occurred while merging the entity: {ex.Message}");
            return false;
        }
    }


    async public Task<IEnumerable<T>> AllEntitiesAsync(string tableName)
    {
        return await AllTableEntitiesAsync(tableName, String.Empty);
    }

    async public Task<IEnumerable<T>> AllEntitiesAsync(string tableName, string partitionKey)
    {
        return await AllTableEntitiesAsync(tableName, partitionKey);
    }

    async private Task<IEnumerable<T>> AllTableEntitiesAsync(string tableName, string partitionKey)
    {
        // Create the TableServiceClient using the connection string
        TableServiceClient tableServiceClient = new TableServiceClient(_connectionString);

        // Get a reference to the table
        TableClient tableClient = tableServiceClient.GetTableClient(tableName);

        List<T> entities = new List<T>();

        try
        {
            // Define the query filter for partition key if provided
            string filter = string.IsNullOrWhiteSpace(partitionKey)
                ? null
                : TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey}");

            // Execute the query and iterate through the result pages
            await foreach (Page<T> page in tableClient.QueryAsync<T>(filter: filter).AsPages())
            {
                entities.AddRange(page.Values);
            }
        }
        catch (RequestFailedException ex)
        {
            // Handle any errors that occur during the query operation
            Console.WriteLine($"An error occurred while querying the entities: {ex.Message}");
        }

        return entities;
    }

    async private Task<List<T>> ExecuteQueryAsync(TableClient tableClient, string filter = null, int? takeCount = null)
    {
        List<T> results = new List<T>();

        try
        {
            // Query the entities using the TableClient with specified filter and take count
            await foreach (var page in tableClient.QueryAsync<T>(filter: filter, maxPerPage: takeCount).AsPages())
            {
                results.AddRange(page.Values);
            }
        }
        catch (RequestFailedException ex)
        {
            // Handle any errors that occur during the query operation
            Console.WriteLine($"An error occurred while executing the query: {ex.Message}");
        }

        return results;
    }


    async public Task<T> EntityAsync(string tableName, string partitionKey, string rowKey)
    {
        T tableEntity = await EntityAsync(tableName, partitionKey, rowKey);
        if (tableEntity != null)
        {
            return tableEntity;
        }

        return default(T);
    }

    async public Task<bool> DeleteEntityAsync(string tableName, T tableEntity)
    {
        return await DeleteEntityAsync(tableName, tableEntity.PartitionKey, tableEntity.RowKey);
    }

    async public Task<bool> DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
    {
        // Fetch the entity to be deleted
        var tableEntity = await EntityAsync(tableName, partitionKey, rowKey);
        if (tableEntity == null)
        {
            return false;
        }

        // Create the TableServiceClient using the connection string
        TableServiceClient tableServiceClient = new TableServiceClient(_connectionString);

        // Get a reference to the table
        TableClient tableClient = tableServiceClient.GetTableClient(tableName);

        try
        {
            // Perform the delete operation
            await tableClient.DeleteEntityAsync(partitionKey, rowKey, tableEntity.ETag);

            return true;
        }
        catch (RequestFailedException ex)
        {
            // Handle any errors that occur during the delete operation
            Console.WriteLine($"An error occurred while deleting the entity: {ex.Message}");
            return false;
        }
    }


    #region Helper

    private TableServiceClient CreateTableServiceClient()
    {
        // Create the TableServiceClient using the connection string
        TableServiceClient tableServiceClient = new TableServiceClient(_connectionString);

        return tableServiceClient;
    }

    async private Task<T> EntityAsync(string tableName, string partitionKey, string rowKey, TableServiceClient tableServiceClient = null)
    {
        if (tableServiceClient == null)
        {
            tableServiceClient = CreateTableServiceClient();
        }

        // Get a reference to the table client
        TableClient tableClient = tableServiceClient.GetTableClient(tableName);

        try
        {
            // Retrieve the entity
            var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);

            // Check if the response contains an entity
            if (response.HasValue)
            {
                return response.Value;
            }
        }
        catch (RequestFailedException ex)
        {
            // Handle the exception (e.g., entity not found or other errors)
            Console.WriteLine($"An error occurred while retrieving the entity: {ex.Message}");
        }

        return default(T);
    }


    #endregion
}
