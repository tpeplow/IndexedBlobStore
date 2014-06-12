using System.Web;
using System.Web.UI.WebControls.WebParts;
using Microsoft.WindowsAzure.Storage.Table;

namespace IndexedBlobStore
{
    internal interface IIndexedBlobEntity
    {
        int BlobCount { get; }
        string FileKey { get; }
        bool Compressed { get; }
        long Length { get; }
        string FileName { get; }
        int PropertyCount { get; }
    }

    internal class IndexedBlobEntity : TableEntity, IIndexedBlobEntity
    {
        public string BlobUri { get; set; }
        public int BlobCount { get; set; }
        public string FileKey { get { return RowKey; }}
        public bool Compressed { get; set; }
        public long Length { get; set; }
        public string FileName { get; set; }
        public int PropertyCount { get; set; }
    }

    internal class IndexedBlobTagEntity : TableEntity, IIndexedBlobEntity
    {
        public string FileName { get; set; }
        public int BlobCount { get; set; }
        public string FileKey { get { return RowKey; } }
        public bool Compressed { get; set; }
        public long Length { get; set; }
        public int PropertyCount { get; set; }

        public static ITableEntity Create(string fileKey, string tag, string fileName, int blobCount, bool compressed, long length, int propertyCount)
        {
            return new IndexedBlobTagEntity
            {
                PartitionKey = tag,
                RowKey = fileKey,
                FileName = fileName,
                BlobCount = blobCount,
                Compressed = compressed,
                Length = length,
                PropertyCount = propertyCount
            };
        }
    }
}