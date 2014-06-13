using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;

namespace IndexedBlobStore.Cache
{
    public class IndexedBlobFileSystemCache : IIndexedBlobCache
    {
        long _cacheSize;
        readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        Dictionary<string, CachedItem> _cachedItems = new Dictionary<string, CachedItem>();
        readonly IndexedBlobLocalCacheSettings _cacheSettings;

        public IndexedBlobFileSystemCache(IndexedBlobLocalCacheSettings cacheSettings)
        {
            _cacheSettings = cacheSettings;
        }

        public void CreateIfNotExists()
        {
            Directory.CreateDirectory(_cacheSettings.CacheDirectory);
        }

        public void Delete()
        {
            try
            {
                _lock.EnterWriteLock();

                if (Directory.Exists(_cacheSettings.CacheDirectory))
                    Directory.Delete(_cacheSettings.CacheDirectory, true);

                _cachedItems = new Dictionary<string, CachedItem>();
                _cacheSize = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            try
            {
                _lock.EnterWriteLock();
                _cachedItems = new Dictionary<string, CachedItem>();
                _cacheSize = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryGet(string fileKey, out Stream stream)
        {
            stream = null;
            _lock.EnterReadLock();
            try
            {
                CachedItem item;
                if (_cachedItems.TryGetValue(fileKey, out item))
                {
                    if (!item.IsCreated)
                    {
                        return false;
                    }
                    stream = item.OpenRead();
                    return true;
                }
                return false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Stream Add(string fileKey, Stream stream, long uncompressedLength)
        {
            if (!_cacheSettings.Enabled || uncompressedLength > _cacheSettings.MaxCacheSize)
                return stream;
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_cachedItems.ContainsKey(fileKey))
                {
                    return stream;
                }
                _lock.EnterWriteLock();
                CachedItem cachedItem;
                try
                {
                    if (IsFull(uncompressedLength))
                    {
                        EvictCachedItems(uncompressedLength);
                        if (IsFull(uncompressedLength))
                            return stream;
                    }
                    cachedItem = new CachedItem(fileKey, uncompressedLength, _cacheSettings);
                    _cachedItems.Add(fileKey, cachedItem);
                    _cacheSize += uncompressedLength;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
                // call create outside of the write lock as to not stop other writers whilst we copy the data into the cache.
                return cachedItem.Create(stream);
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        void EvictCachedItems(long uncompressedLength)
        {
            var items = _cachedItems.Values.Where(x => !x.IsLocked && x.IsCreated).OrderBy(x => x.LastAccessed).ToArray();
            foreach (var item in items)
            {
                if (!IsFull(uncompressedLength))
                    return;

                item.Delete();
                _cachedItems.Remove(item.FileKey);
                _cacheSize -= item.Length;
            }
        }

        bool IsFull(long uncompressedLength)
        {
            return uncompressedLength + _cacheSize > _cacheSettings.MaxCacheSize;
        }
    }
}