using Microsoft.WindowsAzure.Storage.Table;

namespace IndexedBlobStore
{
    internal interface IIndexedBlobEntity
    {
        int BlobCount { get; }
        string FileKey { get; }
        bool Compressed { get; }
        long Length { get; }
    }

    internal class IndexedBlobEntity : TableEntity, IIndexedBlobEntity
    {
        public string BlobUri { get; set; }
        public int BlobCount { get; set; }
        public string FileKey { get { return RowKey; }}
        public bool Compressed { get; set; }
        public long Length { get; set; }
    }

    internal class IndexedBlobTagEntity : TableEntity, IIndexedBlobEntity
    {
        public string FileName { get; set; }
        public int BlobCount { get; set; }
        public string FileKey { get { return RowKey; } }
        public bool Compressed { get; set; }
        public long Length { get; set; }
    }
}