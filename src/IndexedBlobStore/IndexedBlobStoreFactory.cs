using System.IO;
using IndexedBlobStore.Cache;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace IndexedBlobStore
{
    public class IndexedBlobStoreFactory
    {
        readonly CloudBlobClient _blobClient;
        readonly CloudTableClient _tableClient;

        public IndexedBlobStoreFactory(CloudStorageAccount cloudStorageAccount)
        {
            _blobClient = cloudStorageAccount.CreateCloudBlobClient();
            _tableClient = cloudStorageAccount.CreateCloudTableClient();
        }

        public IIndexedBlobStoreClient Create(string storeName, IndexedBlobLocalCacheSettings cacheSettings = null)
        {
            if (cacheSettings == null)
                cacheSettings = new IndexedBlobLocalCacheSettings();

            return Create(storeName, new IndexedBlobFileSystemCache(cacheSettings));
        }

        public IIndexedBlobStoreClient Create(string storeName, IIndexedBlobCache cache)
        {
            var table = _tableClient.GetTableReference(string.Format("{0}index", storeName));
            table.CreateIfNotExists();
            var container = _blobClient.GetContainerReference(string.Format("{0}-blobs", storeName));
            container.CreateIfNotExists();
            cache.CreateIfNotExists();
            return new IndexedBlobStoreClient(new CloudIndexedBlobStore(container, table, cache));
        }
    }
}
