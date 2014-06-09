using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace IndexedBlobStore
{
    public class IndexedBlobStoreClient : IIndexedBlobStoreClient
    {
        readonly CloudIndexedBlobStore _store;
        IndexedBlobStorageOptions _defaultStorageOptions;

        internal IndexedBlobStoreClient(CloudIndexedBlobStore store)
        {
            _store = store;
            DefaultStorageOptions = new IndexedBlobStorageOptions();
        }

        public IndexedBlobStorageOptions DefaultStorageOptions
        {
            get { return _defaultStorageOptions; }
            set 
            {
                _defaultStorageOptions = value ?? new IndexedBlobStorageOptions();
            }
        }

        public void Delete()
        {
            _store.Container.Delete();
            _store.Table.Delete();
            _store.Cache.Delete();
        }

        public IIndexedBlob CreateIndexedBlob(string fileName, Stream stream, IndexedBlobStorageOptions options = null)
        {
            options = EnsureOptions(options);

            var key = options.FileKeyGenerator.GenerateKey(stream);
            key = string.Format("{0}-{1}", fileName, key);

            return CreateIndexedBlob(fileName, key, stream, options);
        }

        public IIndexedBlob CreateIndexedBlob(string fileName, string fileKey, Stream stream, IndexedBlobStorageOptions options = null)
        {
            options = EnsureOptions(options);
            
            var indexRecord = LookupIndexedBlob(fileKey);

            return new UploadableIndexedBlob(fileName, stream, fileKey, indexRecord, options, _store);
        }

        public IIndexedBlob ImportBlob(CloudBlockBlob sourceBlob, IndexedBlobStorageOptions options = null)
        {
            options = EnsureOptions(options);
            sourceBlob.FetchAttributes();
            var fileKey = HttpUtility.UrlEncode(sourceBlob.Properties.ContentMD5);
            return ImportBlob(fileKey, sourceBlob, options);
        }

        public IIndexedBlob ImportBlob(string fileKey, CloudBlockBlob sourceBlob, IndexedBlobStorageOptions options = null)
        {
            options = EnsureOptions(options);
            
            var indexRecord = LookupIndexedBlob(fileKey);
            
            return new CopyableIndexedBlob(sourceBlob, fileKey, indexRecord, options, _store);
        }

        public IReadonlyIndexedBlob GetIndexedBlob(string fileKey)
        {
            var entity = LookupIndexedBlob(fileKey);
            if (entity == null)
                return null;
            return new CloudReadonlyIndexedBlob(entity, _store);
        }

        IndexedBlobEntity LookupIndexedBlob(string key)
        {
            var result = _store.Table.Execute(TableOperation.Retrieve<IndexedBlobEntity>(key, key));
            return (IndexedBlobEntity)result.Result;
        }

        IndexedBlobStorageOptions EnsureOptions(IndexedBlobStorageOptions options)
        {
            return options ?? (DefaultStorageOptions);
        }

        public IEnumerable<TaggedIndexedBlob> Find(string tag)
        {
            var query = new TableQuery<IndexedBlobTagEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, tag));
            var entities = _store.Table.ExecuteQuery(query);
            return entities.Select(x => new TaggedIndexedBlob(new CloudReadonlyIndexedBlob(x, _store), x.PartitionKey));
        }
    }
}