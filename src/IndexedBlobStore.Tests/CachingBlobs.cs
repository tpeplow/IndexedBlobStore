using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Machine.Specifications;

namespace IndexedBlobStore.Tests
{
    public class CachingTests
    {
        public class when_uploading_a_blob : IndexedBlobStoreTest
        {
            Establish context = () => _blob = UploadUniqueBlob();
            
            It should_add_it_to_the_cache = () => Cache.TryGet(_blob.FileKey, out _stream).ShouldBeTrue();
            
            static IIndexedBlob _blob;
        }

        public class when_reading_a_blob_not_in_cache : IndexedBlobStoreTest
        {
            Establish context = () =>
            {
                _blob = UploadUniqueBlob();
                Cache.Delete();
                Cache.CreateIfNotExists();
            };

            Because of = () => ReadStream(Client.GetIndexedBlob(_blob.FileKey).OpenRead());

            It should_add_it_to_the_cache = () => Cache.TryGet(_blob.FileKey, out _stream).ShouldBeTrue();
            
            static IIndexedBlob _blob;
        }

        public class when_reading_blob_being_written : IndexedBlobStoreTest
        {
            Establish context = () =>
            {
                _theNeverEndingStream = new TheNeverEndingStream();
                _blob = Client.CreateIndexedBlob("file", "key so I don't get stuck generating the key", _theNeverEndingStream);
                _task = Task.Factory.StartNew(() => _blob.Upload());
            };

            It should_not_be_available_within_cache = () => Cache.TryGet(_blob.FileKey, out _stream).ShouldBeFalse();

            Cleanup cleanup = () =>
            {
                _theNeverEndingStream.Complete();
                _task.Wait();
                _blob.Dispose();
            };

            static IIndexedBlob _blob;
            static TheNeverEndingStream _theNeverEndingStream;
            static Task _task;
        }

        public class when_cache_is_full : IndexedBlobStoreTest
        {
            Establish context = () =>
            {
                Cache.Delete();
                Cache.CreateIfNotExists();

                using (var blob = Client.CreateIndexedBlob("file", CreateStream("content that will be deleted")))
                {
                    blob.Upload();
                    _oldestKey = blob.FileKey;
                };
                using (var blob = Client.CreateIndexedBlob("file", CreateStream("content that should be kept")))
                {
                    blob.Upload();
                    _shouldBeKeptKey = blob.FileKey;
                }
            };

            Because of = () =>
            {
                var content = Enumerable.Range(0, 994).Select(i => "a").Aggregate((x, x1) => x + x1);
                using (var blob = Client.CreateIndexedBlob("file", CreateStream(content)))
                {
                    blob.Upload();
                    _bigItem = blob.FileKey;
                }
            };

            It should_remove_the_oldest_item = () => Cache.TryGet(_oldestKey, out _stream).ShouldBeFalse();
            It should_keep_the_newer_item = () =>
            {
                Cache.TryGet(_shouldBeKeptKey, out _stream).ShouldBeTrue();
                _stream.Dispose();
            };
            It should_store_the_new_item = () =>
            {
                Cache.TryGet(_bigItem, out _stream).ShouldBeTrue();
                _stream.Dispose();
            };

            static string _oldestKey;
            static string _shouldBeKeptKey;
            static string _bigItem;
        }

        public class when_item_too_big_for_cache : IndexedBlobStoreTest
        {
            Because of = () =>
            {
                var content = Enumerable.Range(0, 1025).Select(i => "a").Aggregate((x, x1) => x + x1);
                using (var blob = Client.CreateIndexedBlob("file", CreateStream(content)))
                {
                    blob.Upload();
                    _key = blob.FileKey;
                }
            };

            It should_not_store_it_in_the_cache = () => Cache.TryGet(_key, out _stream).ShouldBeFalse();

            static string _key;
        }

        public class when_reader_reads_a_chunk_of_the_source : IndexedBlobStoreTest
        {
            Establish context = () =>
            {
                _expectedBuffer = new byte[] { 0xFF, 0xFA, 0xFF };
                using (var blob = Client.CreateIndexedBlob("file", new MemoryStream(_expectedBuffer)))
                {
                    _key = blob.FileKey;
                    blob.Upload();
                }
                Cache.Clear();
            };

            Because of = () =>
            {
                var blob = Client.GetIndexedBlob(_key);
                using (var stream = blob.OpenRead())
                {
                    stream.Position = 1;
                    stream.Read(new byte[1], 0, 1);
                }
                blob = Client.GetIndexedBlob(_key);
                using (var stream = blob.OpenRead())
                {
                    _buffer = new byte[3];
                    stream.Read(_buffer, 0, 3);
                }
            };

            It should_store_the_correct_data_in_the_cache = () => _buffer.ShouldEqual(_expectedBuffer);

            static string _key;
            static byte[] _buffer;
            static byte[] _expectedBuffer;
        }

        public class when_cache_is_full_but_all_items_are_in_use : IndexedBlobStoreTest
        {
            Establish context = () =>
            {
                using (var blob = Client.CreateIndexedBlob("file", CreateStream("content that will be deleted 2")))
                {
                    blob.Upload();
                    _blob1 = blob.FileKey;
                };
                using (var blob = Client.CreateIndexedBlob("file", CreateStream("content that should be kept 2")))
                {
                    blob.Upload();
                    _blob2 = blob.FileKey;
                }
            };

            Because of = () =>
            {
                _blobs = new List<Stream>
                {
                    Client.GetIndexedBlob(_blob1).OpenRead(),
                    Client.GetIndexedBlob(_blob2).OpenRead()
                };
                var content = Enumerable.Range(0, 994).Select(i => "b").Aggregate((x, x1) => x + x1);
                using (var blob = Client.CreateIndexedBlob("file", CreateStream(content)))
                {
                    blob.Upload();
                    _bigItem = blob.FileKey;
                }
            };

            It should_keep_them_in_the_cache = () =>
            {
                Cache.TryGet(_blob1, out _stream).ShouldBeTrue();
                _stream.Dispose();
                Cache.TryGet(_blob2, out _stream).ShouldBeTrue();
                _stream.Dispose();
            };
            It should_keep_the_newer_item = () =>
            {
                Cache.TryGet(_blob2, out _stream).ShouldBeTrue();
                _stream.Dispose();
            };
            It should_not_store_new_item = () => Cache.TryGet(_bigItem, out _stream).ShouldBeFalse();

            Cleanup cleanBlobs = () =>
            {
                foreach (var blob in _blobs)
                {
                    blob.Dispose();
                }
            };

            static string _blob1;
            static string _blob2;
            static string _bigItem;
            static List<Stream> _blobs;
        }

        Establish smallCache = () =>
        {
            _oldCacheSize = TestContext.Current.CacheSettings.MaxCacheSize;
            TestContext.Current.CacheSettings.MaxCacheSize = 1024;
        };

        Cleanup cleanup = () =>
        {
            TestContext.Current.CacheSettings.MaxCacheSize = _oldCacheSize;

            if (_stream == null)
                return;
            _stream.Dispose();
        };

        static Stream _stream;
        static long _oldCacheSize;
    }
}