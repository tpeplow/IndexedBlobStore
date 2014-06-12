using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore
{
    internal class CopyableIndexedBlob : CloudIndexedBlob
    {
        readonly CloudBlockBlob _sourceBlob;

        public CopyableIndexedBlob(CloudBlockBlob sourceBlob, string fileKey, IndexedBlobEntity entity, IndexedBlobStorageOptions options, CloudIndexedBlobStore cloudIndexedBlobStore, Dictionary<string, string> properties)
            : base(fileKey, entity, options, cloudIndexedBlobStore, properties)
        {
            _sourceBlob = sourceBlob;
            if (sourceBlob.Properties.Length == 0)
                sourceBlob.FetchAttributes();
            Length = sourceBlob.Properties.Length;
            FileName = sourceBlob.Name;
        }

        protected override void PerformUpload()
        {
            Compressed = false;
            var copyManager = new BlobCopyManager();
            copyManager.Start(Blob, _sourceBlob);
            copyManager.WaitForCompletion();
        }
    }
}