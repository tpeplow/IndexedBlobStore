using System;
using System.IO;
using System.Threading;

namespace IndexedBlobStore.Cache
{
    internal class CachedItem
    {
        int _readerCount;
        bool _creating;

        public CachedItem(string fileKey, long uncompressedLength, IndexedBlobLocalCacheSettings cacheSettings)
        {
            _creating = true;
            Length = uncompressedLength;
            FileKey = fileKey;
            LastAccessed = DateTime.UtcNow;
            IsCreated = false;
            FilePath = Path.Combine(cacheSettings.CacheDirectory, Guid.NewGuid().ToString("N"));
        }

        public DateTime LastAccessed { get; private set; }
        public long Length { get; private set; }
        public string FilePath { get; private set; }
        public bool IsLocked { get { return _readerCount > 0; } }
        public bool IsCreated { get; private set; }
        public string FileKey { get; private set; }

        public Stream OpenRead()
        {
            if (!IsCreated)
                throw new InvalidOperationException("Cannot open a cached item that has not been created yet");
            return new ReadStream(this);
        }

        public Stream Create(Stream stream)
        {
            using (var cachedFile = new FileStream(FilePath, FileMode.Create))
            using (stream)
            {
                stream.CopyTo(cachedFile);
            }
            IsCreated = true;
            _creating = false;
            return new ReadStream(this);
        }

        public void Delete()
        {
            if (IsLocked)
                throw new InvalidOperationException("Cannot delete a locked item");
            File.Delete(FilePath);
        }

        private class ReadStream : Stream
        {
            readonly CachedItem _cachedItem;
            readonly Stream _sourceStream;
            
            public ReadStream(CachedItem cachedItem)
            {
                _cachedItem = cachedItem;
                _cachedItem.LastAccessed = DateTime.UtcNow;
                Interlocked.Increment(ref cachedItem._readerCount);
                _sourceStream = File.OpenRead(cachedItem.FilePath);
            }

            protected override void Dispose(bool disposing)
            {
                try
                {
                    _sourceStream.Dispose();
                    base.Dispose(disposing);
                }
                finally
                {
                    Interlocked.Decrement(ref _cachedItem._readerCount);
                }
            }

            public override void Flush()
            {
                _sourceStream.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _sourceStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _sourceStream.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _sourceStream.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _sourceStream.Write(buffer, offset, count);
            }

            public override bool CanRead
            {
                get { return _sourceStream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return _sourceStream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return _sourceStream.CanWrite; }
            }

            public override long Length
            {
                get { return _sourceStream.Length; }
            }

            public override long Position
            {
                get { return _sourceStream.Position; }
                set { _sourceStream.Position = value; }
            }
        }
    }
}