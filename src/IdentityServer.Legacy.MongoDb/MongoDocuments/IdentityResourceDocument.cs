using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.MongoDb.MongoDocuments
{
    class IdentityResourceDocument
    {
        [BsonId]
        public string Id { get; set; }

        [BsonElement("BlobData")]
        public string BlobData { get; set; }
    }
}
