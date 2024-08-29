using Azure;
using Azure.Data.Tables;
using IdentityServer.Nova.Abstractions.Cryptography;
using IdentityServer.Nova.Abstractions.Serialize;
using System;

namespace IdentityServer.Nova.Azure.Services.DbContext;

public class BlobTableEntity : ITableEntity
{
    // Implement the required properties from ITableEntity
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Custom property for the blob data
    public string Blob { get; set; }

    // Parameterless constructor (required for deserialization)
    public BlobTableEntity()
    {
    }

    // Custom constructor for initializing with data
    public BlobTableEntity(string partitionKey, string rowKey, object dataObject, ICryptoService cryptoService, IBlobSerializer blobSerializer)
    {
        this.PartitionKey = partitionKey;
        this.RowKey = rowKey;

        // Encrypt and serialize the object data
        this.Blob = cryptoService.EncryptText(blobSerializer.SerializeObject(dataObject));
    }
}


static public class BlobTableEntityExtensions
{
    public static T Deserialize<T>(this BlobTableEntity entity, ICryptoService cryptoService, IBlobSerializer blobSerializer)
    {
        //var properties = entity?.WriteEntity(new OperationContext());
        //if (properties == null)
        //{
        //    return null;
        //}

        //var blobBase64 = properties["Blob"]?.StringValue;

        if (entity?.Blob == null)
        {
            return default(T);
        }

        var bloblBase64 = entity.Blob;

        return blobSerializer.DeserializeObject<T>(cryptoService.DecryptText(bloblBase64));
    }

}
