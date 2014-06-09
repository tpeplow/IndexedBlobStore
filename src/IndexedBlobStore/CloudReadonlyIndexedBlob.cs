using System;
using System.IO;
using System.IO.Compression;

namespace IndexedBlobStore
{
    internal class CloudReadonlyIndexedBlob : IReadonlyIndexedBlob
    {
        readonly Random _random;
        readonly IIndexedBlobEntity _entity;
        readonly CloudIndexedBlobStore _store;

        public CloudReadonlyIndexedBlob(IIndexedBlobEntity entity, CloudIndexedBlobStore store)
        {
            _entity = entity;
            _store = store;
            _random = new Random();
            Length = entity.Length;
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
    }
}