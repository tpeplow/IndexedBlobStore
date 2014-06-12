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
        readonly Lazy<Dictionary<string, string>> _properties; 

        public CloudReadonlyIndexedBlob(IIndexedBlobEntity entity, CloudIndexedBlobStore store)
        {
            _entity = entity;
            _store = store;
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
                options = new IndexedBlobReadOptions();

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
            stream = blob.OpenRead();
            if (_entity.Compressed)
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }

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