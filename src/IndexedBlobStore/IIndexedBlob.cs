using System;

namespace IndexedBlobStore
{
    public interface IIndexedBlob : IDisposable
    {
        void Upload();
        string FileKey { get; }
        string FileName { get; }
        bool Exists { get; }
        long Length { get; }
        void AddTag(string tag);
    }
}