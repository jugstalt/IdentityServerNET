using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.Serialize;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Azure.Services.DbContext
{
    public class BlobTableEntity : TableEntity
    {
        public BlobTableEntity()
            : base()
        {
        }

        public BlobTableEntity(string partitionKey, string rowKey, object dataObject, ICryptoService cryptoService, IBlobSerializer blobSerializer)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;

            this.Blob = cryptoService.EncryptText(blobSerializer.SerializeObject(dataObject));
        }

        public string Blob { get; set; }

        #region Overrides

        //private IDictionary<string, EntityProperty> _properties = new Dictionary<string, EntityProperty>();

        //public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        //{
        //    _properties = properties;
        //}

        //public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        //{
        //    return _properties ?? new Dictionary<string, EntityProperty>();
        //}

        #endregion
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

            if(entity?.Blob==null)
            {
                return default(T);
            }

            var bloblBase64 = entity.Blob;

            return blobSerializer.DeserializeObject<T>(cryptoService.DecryptText(bloblBase64));
        }

    }
}
