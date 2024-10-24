using MongoDB.Bson.Serialization.Attributes;

namespace IdentityServerNET.MongoDb.MongoDocuments;

class ClientBlobDocument
{
    [BsonId]
    public string Id { get; set; }

    [BsonElement("BlobData")]
    public string BlobData { get; set; }
}
