﻿using IdentityServerNET.LiteDb.Documents;
using LiteDB;

namespace IdentityServerNET.LiteDb.Extensions;
static internal class DocumentExtensions
{
    static public ILiteCollection<LiteDbBlobDocument> GetBlobDocumentCollection(
                this LiteDatabase db,
                string collectionName
            )
    {
        var collection = db.GetCollection<LiteDbBlobDocument>(collectionName);

        collection.EnsureIndex(x => x.Id, true);
        collection.EnsureIndex(x => x.Name, true);

        return collection;
    }
}
