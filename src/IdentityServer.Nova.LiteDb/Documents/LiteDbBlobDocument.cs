using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Nova.LiteDb.Documents;
internal class LiteDbBlobDocument
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.Empty;

    public string Name { get; set; } = "";
    public string? AltName { get; set; } = null; // eg. Email for Users

    public string BlobData { get; set; } = "";
}
