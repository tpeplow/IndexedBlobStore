using System;
using System.IO;
using Machine.Specifications;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore.Tests
{
    public class BlobBehaviorAssertions
    {
        public class Md5Tests
        {
            Because of = () =>
            {
                var container = TestContext.Current.StorageAccount.CreateCloudBlobClient().GetContainerReference("test");
                container.CreateIfNotExists();
                _blob = container.GetBlockBlobReference("dave");
                _blob.UploadFromStream(IndexedBlobStoreTest.CreateStream("hello"));
                _blob2 = container.GetBlockBlobReference("dave2");
                _blob2.UploadFromStream(IndexedBlobStoreTest.CreateStream("hello"));
                _blob.FetchAttributes();
                _blob2.FetchAttributes();
            };

            It should_have_an_MD5 = () => _blob.Properties.ContentMD5.ShouldNotBeNull();
            It should_match_other_blobs_MD5 = () => _blob.Properties.ContentMD5.ShouldEqual(_blob2.Properties.ContentMD5);

            static CloudBlockBlob _blob2;
            static CloudBlockBlob _blob;
        }

        public class DuplicateBlob
        {
            Because of = () =>
            {
                var container = TestContext.Current.StorageAccount.CreateCloudBlobClient().GetContainerReference("test");
                container.CreateIfNotExists();
                var blob = container.GetBlockBlobReference(Guid.NewGuid().ToString("N"));
                blob.UploadFromStream(IndexedBlobStoreTest.CreateStream("tom should error"), new AccessCondition
                {
                    IfNoneMatchETag = "*",
                });
                _exception = Catch.Exception(() => blob.UploadFromStream(IndexedBlobStoreTest.CreateStream("tom should error"), new AccessCondition
                {
                    IfNoneMatchETag = "*"
                }));
            };

            It should_throw_an_exception = () => _exception.ShouldNotBeNull();

            static Exception _exception;
        }

        public class CreateIfNotExistsTest
        {
            Because of = () =>
            {
                var container = TestContext.Current.StorageAccount.CreateCloudBlobClient().GetContainerReference("test");
                container.CreateIfNotExists();
                var blob = container.GetBlockBlobReference(Guid.NewGuid().ToString("N"));

                var stream = blob.OpenWrite(new AccessCondition {IfNoneMatchETag = "*",});
                IndexedBlobStoreTest.CreateStream("fred").CopyTo(stream);
                stream.Flush();

                var stream2 = blob.OpenWrite(new AccessCondition {IfNoneMatchETag = "*",});
                IndexedBlobStoreTest.CreateStream("fred").CopyTo(stream);
                stream.Flush();

                stream2.Dispose();

                _exception = Catch.Exception(stream.Dispose);
            };

            It should_throw_a_conflict_exception = () => _exception.ShouldNotBeNull();
            static Exception _exception;
        }

        [Subject("412 Errors")]
        public class when_blob_changes_between_opening_and_reading
        {
            Establish context = () =>
            {
                var container = TestContext.Current.StorageAccount.CreateCloudBlobClient().GetContainerReference("import-source");
                container.CreateIfNotExists();
                _blobName = Guid.NewGuid().ToString("N");
                _blobToImport = container.GetBlockBlobReference(_blobName);
                _blobToImport.UploadFromStream(IndexedBlobStoreTest.CreateStream("source"));
            };

            public class when_failures_less_than_retry_count
            {
                Establish context = () => _errorCount = 1;
                Because of = DownloadingDuringUpdate;
                It should_NOT_throw = () => _exception.ShouldBeNull();
            }

            public class when_failures_exceed_retry_count
            {
                Establish context = () => _errorCount = 6;
                Because of = DownloadingDuringUpdate;
                It should_throw = () => _exception.ShouldNotBeNull();
            }

            public class when_retrying_after_initial_failure
            {
                Establish context = () =>
                {
                    _errorCount = 5;
                    DownloadingDuringUpdate();
                    _exception = null;
                    _errorCount = 3;
                };

                Because of = DownloadingDuringUpdate;
                It should_not_throw = () => _exception.ShouldBeNull();
            }

            static void DownloadingDuringUpdate()
            {
                _exception = Catch.Exception(() =>
                {
                    ReliableCloudOperations.RetryRead(DownloadSource);
                });
            }

            static void DownloadSource()
            {
                using (var destinationStream = new MemoryStream())
                {
                    var blobRequestOptions = new IndexedBlobStorageOptions().BlobRequestOptions;
                    using (var blobStream = _blobToImport.OpenRead(options: blobRequestOptions))
                    {
                        // Make an independent change to the source blob to cause a 412
                        if (_errorCount-- > 0) _blobToImport.SetProperties();
                        blobStream.CopyTo(destinationStream);
                    }
                }
            }

            static Exception _exception;
            static int _errorCount;
            static CloudBlockBlob _blobToImport;
            static string _blobName;
        }
    }
}