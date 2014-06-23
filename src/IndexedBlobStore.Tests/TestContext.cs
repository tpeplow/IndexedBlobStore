using System;
using IndexedBlobStore.Cache;
using Microsoft.WindowsAzure.Storage;

namespace IndexedBlobStore.Tests
{
    internal class TestContext
    {
        private TestContext(IIndexedBlobStoreClient client, CloudStorageAccount storageAccount, IndexedBlobFileSystemCache cache, IndexedBlobLocalCacheSettings cacheSettings)
        {
            Client = client;
            StorageAccount = storageAccount;
            Cache = cache;
            CacheSettings = cacheSettings;
        }

        public static void Setup(CloudStorageAccount storageAccount = null)
        {
            if (storageAccount == null)
                storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
            var storeName = "test" + Guid.NewGuid().ToString("N");
            
            var factory = new IndexedBlobStoreFactory(storageAccount);
            var cacheSettings = new IndexedBlobLocalCacheSettings();
            var cache = new IndexedBlobFileSystemCache(cacheSettings);

            var client = factory.Create(storeName, cache);

            Current = new TestContext(client, storageAccount, cache, cacheSettings)
            {
                StoreName = storeName
            };
        }

        public static TestContext Current { get; private set; }

        public IIndexedBlobStoreClient Client { get; private set; }

        public CloudStorageAccount StorageAccount { get; private set; }

        public string StoreName { get; private set; }

        public IIndexedBlobCache Cache { get; private set; }

        public IndexedBlobLocalCacheSettings CacheSettings { get; set; }
    }
}