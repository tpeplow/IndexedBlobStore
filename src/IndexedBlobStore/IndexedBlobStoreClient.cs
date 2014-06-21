using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace IndexedBlobStore
{
    public class IndexedBlobStoreClient : IIndexedBlobStoreClient
    {
        readonly CloudIndexedBlobStore _store;
        IndexedBlobStorageOptions _defaultStorageOptions;
        IndexedBlobReadOptions _defaultReadOptions;

        internal IndexedBlobStoreClient(CloudIndexedBlobStore store)
        {
            _store = store;
            DefaultStorageOptions = new IndexedBlobStorageOptions();
            DefaultReadOptions = new IndexedBlobReadOptions();
        }

        public IndexedBlobStorageOptions DefaultStorageOptions
        {
            get { return _defaultStorageOptions; }
            set { _defaultStorageOptions = value ?? new IndexedBlobStorageOptions(); }
        }

        public IndexedBlobReadOptions DefaultReadOptions
        {
            get { return _defaultReadOptions; }
            set { _defaultReadOptions = value ?? new IndexedBlobReadOptions(); }
        }

        public void Delete()
        {
            _store.Container.Delete();
            _store.Table.Delete();
            _store.Cache.Delete();
        }

        public IIndexedBlob CreateIndexedBlob(string fileName, Stream stream, IndexedBlobStorageOptions options = null, Dictionary<string, string> properties = null)
        {
            options = EnsureOptions(options);

            var key = options.FileKeyGenerator.GenerateKey(stream);
            key = string.Format("{0}-{1}", fileName, key);

            return CreateIndexedBlob(fileName, key, stream, options, properties);
        }

        public IIndexedBlob CreateIndexedBlob(string fileName, string fileKey, Stream stream, IndexedBlobStorageOptions options = null, Dictionary<string, string> properties = null)
        {
            options = EnsureOptions(options);
            
            var indexRecord = LookupIndexedBlob(fileKey);

            return new UploadableIndexedBlob(fileName, stream, fileKey, indexRecord, options, _store, properties);
        }

        public IIndexedBlob ImportBlob(CloudBlockBlob sourceBlob, IndexedBlobStorageOptions options = null, Dictionary<string, string> properties = null)
        {
            options = EnsureOptions(options);
            sourceBlob.FetchAttributes();
            var fileKey = string.Format("{0}-{1}", sourceBlob.Uri.LocalPath.Replace("/", "-"), sourceBlob.Properties.ETag);
            return ImportBlob(fileKey, sourceBlob, options, properties);
        }

        public IIndexedBlob ImportBlob(string fileKey, CloudBlockBlob sourceBlob, IndexedBlobStorageOptions options = null, Dictionary<string, string> properties = null)
        {
            options = EnsureOptions(options);
            
            var indexRecord = LookupIndexedBlob(fileKey);

            if (options.UseBlobCopyAccrossStorageAccounts || sourceBlob.ServiceClient.Credentials.AccountName == _store.Container.ServiceClient.Credentials.AccountName)
            {
                return new CopyableIndexedBlob(sourceBlob, fileKey, indexRecord, options, _store, properties);
            }

            return new DownloadUploadImportBlob(sourceBlob, fileKey, indexRecord, options, _store, properties);
        }

        public IReadonlyIndexedBlob GetIndexedBlob(string fileKey)
        {
            var entity = LookupIndexedBlob(fileKey);
            if (entity == null)
                return null;
            return new CloudReadonlyIndexedBlob(entity, _store, _defaultReadOptions);
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
            return entities.Select(x => new TaggedIndexedBlob(new CloudReadonlyIndexedBlob(x, _store, _defaultReadOptions), x.PartitionKey));
        }
    }
}