using System.Collections.Generic;
using System.IO;

namespace IndexedBlobStore
{
    public interface IReadonlyIndexedBlob
    {
        Stream OpenRead(IndexedBlobReadOptions options = null);
        string FileKey { get; }
        long Length { get; }
        string FileName { get; }
        Dictionary<string, string> Properties { get; } 
    }
}