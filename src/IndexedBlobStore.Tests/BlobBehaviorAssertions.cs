using System;
using Machine.Specifications;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore.Tests
{
    public class Md5Tests : IndexedBlobStoreTest
    {
        Because of = () =>
        {
            var container = TestContext.Current.StorageAccount.CreateCloudBlobClient().GetContainerReference("test");
            container.CreateIfNotExists();
            _blob = container.GetBlockBlobReference("dave");
            _blob.UploadFromStream(CreateStream("hello"));
            _blob2 = container.GetBlockBlobReference("dave2");
            _blob2.UploadFromStream(CreateStream("hello"));
            _blob.FetchAttributes();
            _blob2.FetchAttributes();
        };

        It should_have_an_MD5 = () => _blob.Properties.ContentMD5.ShouldNotBeNull();
        It should_match_other_blobs_MD5 = () => _blob.Properties.ContentMD5.ShouldEqual(_blob2.Properties.ContentMD5);

        static CloudBlockBlob _blob2;
        static CloudBlockBlob _blob;
    }

    public class DuplicateBlob : IndexedBlobStoreTest
    {
        Because of = () =>
        {
            var container = TestContext.Current.StorageAccount.CreateCloudBlobClient().GetContainerReference("test");
            container.CreateIfNotExists();
            var blob = container.GetBlockBlobReference(Guid.NewGuid().ToString("N"));
            blob.UploadFromStream(CreateStream("tom should error"), new AccessCondition
            {
                IfNoneMatchETag = "*",
            });
            _exception = Catch.Exception(() => blob.UploadFromStream(CreateStream("tom should error"),  new AccessCondition
            {
                IfNoneMatchETag = "*"
            }));
        };

        It should_throw_an_exception = () => _exception.ShouldNotBeNull();

        static Exception _exception;
    }
}