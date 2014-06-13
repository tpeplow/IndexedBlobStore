using System;
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
        }

        public IFileKeyGenerator FileKeyGenerator { get; set; }
        public int StreamWriteSizeInBytes { get; set; }
        public BlobRequestOptions BlobRequestOptions { get; set; }
        
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