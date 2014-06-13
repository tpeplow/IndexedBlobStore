using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore
{
    public class IndexedBlobReadOptions
    {
        public IndexedBlobReadOptions()
        {
            StreamMinimumReadSizeInBytes = 4194304;
        }

        public int? UseSpecificLoadBalancedBlob { get; set; }
        public BlobRequestOptions BlobRequestOptions { get; set; }
        public int StreamMinimumReadSizeInBytes { get; set; }
    }
}