using System.IO;

namespace IndexedBlobStore.Cache
{
    public interface IIndexedBlobCache
    {
        void Delete();
        bool TryGet(string fileKey, out Stream stream);
        Stream Add(string fileKey, Stream stream, long uncompressedLength);
        void CreateIfNotExists();
    }
}