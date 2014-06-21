using System;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore
{
    public class IndexedBlobStorageOptions
    {
        int _additionalBlobsForLoadBalancing;

        public IndexedBlobStorageOptions()
        {
            FileKeyGenerator = new SHA1FileKeyGenerator();
            AdditionalBlobsForLoadBalancing = 0;
            StreamWriteSizeInBytes = 4194304;
            TemporaryDirectory = Path.Combine(Path.GetTempPath(), "IndexedBlobStoreTemp");
            UseBlobCopyAccrossStorageAccounts = false;
        }

        public IFileKeyGenerator FileKeyGenerator { get; set; }
        public int StreamWriteSizeInBytes { get; set; }
        public BlobRequestOptions BlobRequestOptions { get; set; }
        public string TemporaryDirectory { get; set; }
        public bool UseBlobCopyAccrossStorageAccounts { get; set; }
        
        public int AdditionalBlobsForLoadBalancing
        {
            get { return _additionalBlobsForLoadBalancing; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Cannot have a negative number of additional blobs");
                _additionalBlobsForLoadBalancing = value;
            }
        }
    }
}