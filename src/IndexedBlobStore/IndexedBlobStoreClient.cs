using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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

            var key = options.FileKeyGenerator.GenerateKey(fileName, stream);

            return CreateIndexedBlob(fileName, key, stream, options, properties);
        }

        public IIndexedBlob CreateIndexedBlob(string fileName, string fileKey, Stream stream, IndexedBlobStorageOptions options = null, Dictionary<string, string> properties = null)
        {
            options = EnsureOptions(options);
            
            var result = LookupIndexedBlob(fileKey);

            return new UploadableIndexedBlob(fileName, stream, fileKey, result.Entity, options, _store, properties);
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
            
            var result = LookupIndexedBlob(fileKey);

            if (options.UseBlobCopyAccrossStorageAccounts || sourceBlob.ServiceClient.Credentials.AccountName == _store.Container.ServiceClient.Credentials.AccountName)
            {
                return new CopyableIndexedBlob(sourceBlob, fileKey, result.Entity, options, _store, properties);
            }

            return new DownloadUploadImportBlob(sourceBlob, fileKey, result.Entity, options, _store, properties);
        }

        public IReadonlyIndexedBlob GetIndexedBlob(string fileKey, bool throwOnNotFound = false)
        {
            var result = LookupIndexedBlob(fileKey);
            if (result.Entity != null && result.StatusCode == 200) return new CloudReadonlyIndexedBlob(result.Entity, _store, _defaultReadOptions);

            if (throwOnNotFound) throw new IndexedBlobNotFoundException($"Indexed blob '{fileKey}' could not be found in the index. Code: {result.StatusCode}.");
            return null;
        }

        IndexedBlobLookupResult LookupIndexedBlob(string key)
        {
            var result = _store.Table.Execute(TableOperation.Retrieve<IndexedBlobEntity>(key, key));
            return new IndexedBlobLookupResult
            {
                Entity = (IndexedBlobEntity) result.Result,
                StatusCode = result.HttpStatusCode
            };
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

    internal class IndexedBlobLookupResult
    {
        public IndexedBlobEntity Entity { get; set; }
        public int StatusCode { get; set; }
    }

    public class IndexedBlobNotFoundException : Exception
    {
        public IndexedBlobNotFoundException()
        {
        }

        public IndexedBlobNotFoundException(string message) : base(message)
        {
        }
    }
}