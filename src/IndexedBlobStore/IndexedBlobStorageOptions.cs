using System;

namespace IndexedBlobStore
{
    public class IndexedBlobStorageOptions
    {
        int _additionalBlobsForLoadBalancing;

        public IndexedBlobStorageOptions()
        {
            Compress = true;
            FileKeyGenerator = new SHA1FileKeyGenerator();
            AdditionalBlobsForLoadBalancing = 0;
        }

        public bool Compress { get; set; }
        public IFileKeyGenerator FileKeyGenerator { get; set; }

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