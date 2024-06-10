using MongoDB.Bson.Serialization.Attributes;

namespace IdentityServer.Nova.MongoDb.MongoDocuments
{
    class ApiResourceDocument
    {
        [BsonId]
        public string Id { get; set; }

        [BsonElement("BlobData")]
        public string BlobData { get; set; }
    }
}
