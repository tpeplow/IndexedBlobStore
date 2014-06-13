using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace IndexedBlobStore
{
    internal class CloudReadonlyIndexedBlob : IReadonlyIndexedBlob
    {
        readonly Random _random;
        readonly IIndexedBlobEntity _entity;
        readonly CloudIndexedBlobStore _store;
        readonly IndexedBlobReadOptions _defaultReadOptions;
        readonly Lazy<Dictionary<string, string>> _properties; 

        public CloudReadonlyIndexedBlob(IIndexedBlobEntity entity, CloudIndexedBlobStore store, IndexedBlobReadOptions defaultReadOptions)
        {
            _entity = entity;
            _store = store;
            _defaultReadOptions = defaultReadOptions;
            _random = new Random();
            Length = entity.Length;
            _properties = new Lazy<Dictionary<string, string>>(LoadProperties);
        }
        
        public Stream OpenRead(IndexedBlobReadOptions options = null)
        {
            Stream stream;
            if (_store.Cache.TryGet(FileKey, out stream))
            {
                return stream;
            }

            if (options == null)
                options = _defaultReadOptions;

            var index = 0;
            if (options.UseSpecificLoadBalancedBlob.HasValue)
            {
                index = Math.Min(options.UseSpecificLoadBalancedBlob.Value, _entity.BlobCount -1);
            }
            else if (_entity.BlobCount > 1)
            {
                index = _random.Next(_entity.BlobCount);
            }
            var blob = _store.Container.GetBlockBlobReference(string.Format("{0}-{1}", FileKey, index));
            blob.StreamMinimumReadSizeInBytes = options.StreamMinimumReadSizeInBytes;

            stream = blob.OpenRead(options: options.BlobRequestOptions);
            
            return _store.Cache.Add(FileKey, stream, _entity.Length);
        }

        public string FileKey
        {
            get { return _entity.FileKey; }
        }

        public long Length { get; private set; }

        public string FileName
        {
            get { return _entity.FileName; }
        }

        public Dictionary<string, string> Properties
        {
            get { return _properties.Value; }
        }
        
        Dictionary<string, string> LoadProperties()
        {
            if (_entity.PropertyCount == 0)
                return new Dictionary<string, string>();

            var partitionKey = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, FileKey);
            var rowkey = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, "prop::");
            var query = new TableQuery<IndexedBlobProperty>()
                .Where(TableQuery.CombineFilters(partitionKey, TableOperators.And, rowkey))
                .Take(_entity.PropertyCount);

            var result = _store.Table.ExecuteQuery(query);
            return result.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}