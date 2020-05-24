//using Microsoft.Azure.Storage;
//using Microsoft.Azure.Storage.Blob;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using System.Threading.Tasks;

//namespace IdentityServer.Legacy.Azure.Services.DbContext
//{
//    class AzureBloblStorage
//    {
//        private string _connectionString;

//        #region IBlobStorage

//        public bool Init(string initalParameter)
//        {
//            _connectionString = initalParameter;
//            //_connectionString = "DefaultEndpointsProtocol=https;AccountName=[AccountName];AccountKey=[AccountKey]";
//            return true;
//        }

//        async public Task<bool> StoreAsync(IBlob blob)
//        {
//            try
//            {
//                if (blob.HasData)
//                {
//                    CloudBlockBlob blockBlob = await GetBlobReference(blob);

//                    byte[] bytes = blob.ToStorageByteArray();
//                    await blockBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);
//                }
//                else
//                {
//                    return await RemoveAsync(blob);
//                }
//                return true;
//            }
//            catch (StorageException)
//            {
//                return false;
//            }
//        }

//        async public Task<bool> StoreDataAsync(IIdType id, IIdType parentId, string name, byte[] data, BlobTarget blobTarget = BlobTarget.Default)
//        {
//            var blob = new Blob(blobTarget)
//            {
//                Id = id,
//                ParentId = parentId,
//                Name = name,
//                Data = data
//            };

//            return await StoreAsync(blob);
//        }

//        async public Task<bool> StoreStringAsync(IIdType id, IIdType parentId, string name, string data, Core.TextEncoding textEncoding = Core.TextEncoding.Default, BlobTarget blobTarget = BlobTarget.Default)
//        {
//            var blob = new StringBlob(textEncoding, blobTarget)
//            {
//                Id = id,
//                ParentId = parentId,
//                Name = name,
//                DataString = data
//            };

//            return await StoreAsync(blob);
//        }

//        async public Task<bool> LoadAsync(IBlob blob, string ifNoneMatch = null)
//        {
//            string cacheName = String.Empty;

//            try
//            {
//                #region Check ETag Caching

//                //if (!String.IsNullOrWhiteSpace(ifNoneMatch))
//                //{
//                //    cacheName = "etag:" + blob.Id.ToString() + "." + blob.Name;
//                //    if (ifNoneMatch.Equals(Dal.Dal.Cache.Get(cacheName)))
//                //    {
//                //        blob.NotModified = true;
//                //        return true;
//                //    }
//                //}

//                #endregion

//                CloudBlockBlob blockBlob = await GetBlobReference(blob);
//                if (await blockBlob.ExistsAsync())
//                {
//                    if (!String.IsNullOrWhiteSpace(ifNoneMatch) && ifNoneMatch == blockBlob.Properties.ETag)
//                    {
//                        blob.NotModified = true;
//                        return true;
//                    }

//                    blob.ETag = blockBlob.Properties.ETag;
//                    blob.LastModified = blockBlob.Properties.LastModified;

//                    MemoryStream ms = new MemoryStream();
//                    await blockBlob.DownloadToStreamAsync(ms);

//                    byte[] data = new byte[ms.Length];
//                    ms.Position = 0;
//                    ms.Read(data, 0, data.Length);

//                    blob.LoadFromStarageByteArray(data);
//                }
//                else
//                {
//                    blob.Data = null;
//                }

//                return true;
//            }
//            catch (StorageException)
//            {
//                blob.Data = null;
//                return false;
//            }
//        }

//        async public Task<LoadBlobResponse<byte[]>> LoadDataAsync(IIdType uid, IIdType parentId, string name, string ifNoneMatch = null, BlobTarget blobTarget = BlobTarget.Default)
//        {
//            var blob = Blob.QueryBlob(uid, parentId, name, blobTarget);
//            if (await LoadAsync(blob, ifNoneMatch))
//                return new LoadBlobResponse<byte[]>(blob, blob.Data);

//            return null;
//        }

//        async public Task<LoadBlobResponse<string>> LoadStringAsync(IIdType uid, IIdType parentId, string name, string ifNoneMatch = null, BlobTarget blobTarget = BlobTarget.Default)
//        {
//            var blob = Blob.QueryStringBlob(uid, parentId, name, blobTarget);
//            if (await LoadAsync(blob, ifNoneMatch))
//                return new LoadBlobResponse<string>(blob, blob.DataString);

//            return null;
//        }

//        async public Task<bool> RemoveAsync(IBlob blob)
//        {
//            try
//            {
//                CloudBlockBlob blockBlob = await GetBlobReference(blob);
//                if (await blockBlob.ExistsAsync())
//                    await blockBlob.DeleteAsync();

//                return true;
//            }
//            catch (StorageException)
//            {
//                return false;
//            }
//        }

//        async public Task<bool> RemoveDataAsync(IIdType uid, IIdType parentId, string name, BlobTarget blobTarget = BlobTarget.Default)
//        {
//            var blob = Blob.QueryBlob(uid, parentId, name, blobTarget);
//            return await RemoveAsync(blob);
//        }

//        async public Task<bool> RemoveStringAsync(IIdType uid, IIdType parentId, string name)
//        {
//            var blob = Blob.QueryStringBlob(uid, parentId, name);
//            return await RemoveAsync(blob);
//        }

//        async public Task<bool> Exists(IIdType uid, IIdType parentId, string name, BlobTarget blobTarget = BlobTarget.Default)
//        {
//            try
//            {
//                var blob = Blob.QueryStringBlob(uid, parentId, name, blobTarget);

//                CloudBlockBlob blockBlob = await GetBlobReference(blob);
//                return await blockBlob.ExistsAsync();
//            }
//            catch (StorageException)
//            {
//                return false;
//            }
//        }

//        #endregion

//        #region IBlobStorage2

//        async public Task<bool> RemoveContainerAsync(IIdType uid, IIdType parentId)
//        {
//            string storageId = BlobHelper.GetStorageId(uid, parentId);
//            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);

//            // Create the blob client.
//            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

//            // Retrieve a reference to a container. 
//            CloudBlobContainer container = blobClient.GetContainerReference(BucketName(uid, parentId, BlobTarget.Default));

//            CloudBlobDirectory blobDirectory = container.GetDirectoryReference(storageId);
//            IEnumerable<IListBlobItem> items = await ListBlobsAsync(blobDirectory);

//            try
//            {
//                int count = items.Count();
//            }
//            catch (Exception)
//            {
//                items = null;
//            }

//            if (items != null)
//            {
//                foreach (var item in items)
//                {
//                    if (item is CloudBlockBlob)
//                        await ((CloudBlockBlob)item).DeleteIfExistsAsync();
//                }
//            }

//            return true;
//        }

//        async public Task<long> GetContainerSizeAsync(IIdType uid, IIdType parentId)
//        {
//            string storageId = BlobHelper.GetStorageId(uid, parentId);
//            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);

//            // Create the blob client.
//            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

//            // Retrieve a reference to a container. 
//            CloudBlobContainer container = blobClient.GetContainerReference(BucketName(uid, parentId, BlobTarget.Default));

//            CloudBlobDirectory blobDirectory = container.GetDirectoryReference(storageId);
//            IEnumerable<IListBlobItem> items = await ListBlobsAsync(blobDirectory);

//            try
//            {
//                int count = items.Count();
//            }
//            catch (Exception)
//            {
//                return 0;
//            }

//            long length = 0;
//            if (items != null)
//            {
//                foreach (var item in items)
//                {
//                    if (item is CloudBlockBlob)
//                    {
//                        await ((CloudBlockBlob)item).FetchAttributesAsync();
//                        length += ((CloudBlockBlob)item).Properties.Length;
//                    }

//                }
//            }

//            return length;
//        }

//        async public Task<string[]> GetBlobNames(IIdType uid, IIdType parentId, BlobTarget blobTarget = BlobTarget.Default)
//        {
//            List<string> names = new List<string>();

//            try
//            {
//                string storageId = BlobHelper.GetStorageId(uid, parentId);
//                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);

//                // Create the blob client.
//                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

//                // Retrieve a reference to a container. 
//                CloudBlobContainer container = blobClient.GetContainerReference(BucketName(uid, parentId, blobTarget));

//                CloudBlobDirectory blobDirectory = container.GetDirectoryReference(storageId);
//                IEnumerable<IListBlobItem> items = await ListBlobsAsync(blobDirectory);


//                string dir = storageId + "/";

//                if (items != null)
//                {
//                    foreach (var item in items)
//                    {
//                        if (item is CloudBlockBlob)
//                        {
//                            string name = ((CloudBlockBlob)item).Name;
//                            if (name.StartsWith(dir))
//                                name = name.Substring(dir.Length);

//                            names.Add(name);
//                        }
//                    }
//                }
//            }
//            catch { }

//            return names.ToArray();
//        }

//        #endregion

//        #region IPersistantKeyBlobStorage

//        async public Task<CloudBlockBlob> GetPersistKeyBlobUri(string appName)
//        {
//            Blob blob = new Blob(BlobTarget.Settings);
//            blob.Name = $"{ appName }-persistkeys";
//            blob.Id = new PrettyLong("0");

//            var blobReference = await GetBlobReference(blob);
//            return blobReference;
//        }

//        #endregion

//        #region Helper

//        async private Task<List<IListBlobItem>> ListBlobsAsync(CloudBlobDirectory blobDirectory)
//        {
//            BlobContinuationToken continuationToken = null;
//            List<IListBlobItem> results = new List<IListBlobItem>();
//            do
//            {
//                var response = await blobDirectory.ListBlobsSegmentedAsync(continuationToken);
//                continuationToken = response.ContinuationToken;
//                results.AddRange(response.Results);
//            }
//            while (continuationToken != null);
//            return results;
//        }

//        async private Task<CloudBlockBlob> GetBlobReference(IBlob blob)
//        {
//            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionString);

//            // Create the blob client.
//            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

//            // Retrieve a reference to a container. 
//            CloudBlobContainer container = blobClient.GetContainerReference(BucketName(blob.Id, blob.ParentId, blob.BlobTarget));

//            // Create the container if it doesn't already exist.
//            await container.CreateIfNotExistsAsync();
//            // Retrieve reference to a blob named "myblob".
//            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blob.StorageId + "/" + blob.Name);

//            return blockBlob;
//        }

//        protected string BucketName(IIdType id, IIdType parentId, BlobTarget blobTarget)
//        {
//            

//            throw new Exception("Unknown BlobTarget");
//        }

//        #endregion    
//    }
//}
