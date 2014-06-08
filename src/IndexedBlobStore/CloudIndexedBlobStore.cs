using IndexedBlobStore.Cache;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace IndexedBlobStore
{
    internal class CloudIndexedBlobStore
    {
        public CloudIndexedBlobStore(CloudBlobContainer container, CloudTable table, IIndexedBlobCache cache)
        {
            Container = container;
            Table = table;
            Cache = cache;
        }

        public CloudBlobContainer Container { get; private set; }
        public CloudTable Table { get; private set; }
        public IIndexedBlobCache Cache { get; private set; }
    }
}