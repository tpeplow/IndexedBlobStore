using System.IO;

namespace IndexedBlobStore.Cache
{
    public class IndexedBlobLocalCacheSettings
    {
        public IndexedBlobLocalCacheSettings()
        {
            MaxCacheSize = 1024*1024*1024;
            MaxCacheSize = MaxCacheSize*10;
            Enabled = true;
            CacheDirectory = Path.Combine(Path.GetTempPath(), "IndexedBlobStoreCache");
        }

        public long MaxCacheSize { get; set; }
        public bool Enabled { get; set; }
        public string CacheDirectory { get; set; }
    }
}