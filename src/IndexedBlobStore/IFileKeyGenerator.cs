using System.IO;

namespace IndexedBlobStore
{
    public interface IFileKeyGenerator
    {
        string GenerateKey(Stream stream);
    }
}