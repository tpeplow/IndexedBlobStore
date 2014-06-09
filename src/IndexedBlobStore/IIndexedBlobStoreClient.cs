using System.Collections.Generic;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore
{
    public interface IIndexedBlobStoreClient
    {
        IndexedBlobStorageOptions DefaultStorageOptions { get; set; }
        IIndexedBlob CreateIndexedBlob(string fileName, Stream stream, IndexedBlobStorageOptions options = null);
        IIndexedBlob CreateIndexedBlob(string fileName, string fileKey, Stream stream, IndexedBlobStorageOptions options = null);
        IIndexedBlob ImportBlob(CloudBlockBlob sourceBlob, IndexedBlobStorageOptions options = null);
        IIndexedBlob ImportBlob(string fileKey, CloudBlockBlob sourceBlob, IndexedBlobStorageOptions options = null);
        IReadonlyIndexedBlob GetIndexedBlob(string fileKey);
        IEnumerable<TaggedIndexedBlob> Find(string tag);
        void Delete();
    }
}