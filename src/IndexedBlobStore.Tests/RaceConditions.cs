using System;
using Machine.Specifications;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore.Tests
{
    public class RaceConditions
    {
        Because of = () => _exception = Catch.Exception(() =>
        {
            _blob.Upload();
            CaptureBlob();
            _blob2.Upload();
        });

        public class when_uploading_duplicate : IndexedBlobStoreTest
        {
            Establish context = () =>
            {
                _blob = Client.CreateIndexedBlob("file", CreateStream(_content));
                _blob2 = Client.CreateIndexedBlob("file", CreateStream(_content));
            };

            Behaves_like<DuplicateBlobBehavior> duplicate;

            static string _content = "when uploading blob that race condition";
        }

        public class when_importing_duplicate : ImportTests
        {
            Establish context = () =>
            {
                _blob = Client.ImportBlob(BlobToImport);
                _blob2 = Client.ImportBlob(BlobToImport);
            };

            Behaves_like<DuplicateBlobBehavior> duplicate;
        }

        [Behaviors]
        public class DuplicateBlobBehavior
        {
            It should_throw_blob_already_exists_exception_if_uploaded = () => _exception.ShouldBeOfExactType<BlobAlreadyExistsException>();
            It should_not_have_replaced_the_underlying_blob = () =>
            {
                _underlyingBlob.FetchAttributes();
                _etagBeforeSecondUploadAttempt.ShouldEqual(_underlyingBlob.Properties.ETag);
            };
        }
        
        Cleanup cleanup = () =>
        {
            _blob.Dispose();
            _blob2.Dispose();
        };

        protected static void CaptureBlob()
        {
            var container =
                TestContext.Current.StorageAccount.CreateCloudBlobClient()
                    .GetContainerReference(TestContext.Current.StoreName + "-blobs");
            _underlyingBlob = container.GetBlockBlobReference(_blob.FileKey + "-0");
            _underlyingBlob.FetchAttributes();
            _etagBeforeSecondUploadAttempt = _underlyingBlob.Properties.ETag;
        }

        static string _etagBeforeSecondUploadAttempt;
        static CloudBlockBlob _underlyingBlob;
        static Exception _exception;
        static IIndexedBlob _blob;
        static IIndexedBlob _blob2;

    }
}