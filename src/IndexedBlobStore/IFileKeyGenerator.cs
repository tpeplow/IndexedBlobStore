using System.IO;

namespace IndexedBlobStore
{
    public interface IFileKeyGenerator
    {
        string GenerateKey(string fileName, Stream fileStream);
    }
}