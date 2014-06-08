using System;

namespace IndexedBlobStore
{
    public interface IIndexedBlob : IDisposable
    {
        void Upload();
        string FileKey { get; }
        bool Exists { get; }
        long Length { get; }
        void AddTag(IndexedBlobTag tag);
    }
}