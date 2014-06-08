using System;
using System.Web;
using Machine.Specifications;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore.Tests
{
    public class when_importing_blob_that_does_not_exist : ImportTests
    {
        Establish context = () => _importedBlob = Client.ImportBlob(BlobToImport);
        Because of = () =>
        {
            _importedBlob.Upload();
            _downloadedBlob = Client.GetIndexedBlob(_importedBlob.FileKey);
        };
        It can_be_looked_up_by_key = () => _downloadedBlob.ShouldNotBeNull();
        It should_use_the_MD5_of_the_source_blob_as_filekey = () => _importedBlob.FileKey.ShouldEqual(HttpUtility.UrlEncode(BlobToImport.Properties.ContentMD5));
        It can_download_contents = () => ReadStream(_downloadedBlob.OpenRead()).ShouldEqual("source");
        It should_set_the_size = () => _importedBlob.Length.ShouldEqual(6);

        static IIndexedBlob _importedBlob;
        static IReadonlyIndexedBlob _downloadedBlob;
    }

    public class when_importing_blob_that_does_not_exist_using_given_file_key : ImportTests
    {
        Establish context = () => _importedBlob = Client.ImportBlob("mykey", BlobToImport);
        Because of = () =>
        {
            _importedBlob.Upload();
            _downloadedBlob = Client.GetIndexedBlob(_importedBlob.FileKey);
        };
        It can_be_looked_up_by_key = () => _downloadedBlob.ShouldNotBeNull();
        It should_use_given_key = () => _importedBlob.FileKey.ShouldEqual("mykey");
        It can_download_contents = () => ReadStream(_downloadedBlob.OpenRead()).ShouldEqual("source");
        It should_set_the_size = () => _importedBlob.Length.ShouldEqual(6);

        static IIndexedBlob _importedBlob;
        static IReadonlyIndexedBlob _downloadedBlob;
    }

    public class ImportTests : IndexedBlobStoreTest
    {
        Establish context = () =>
        {
            var container = TestContext.Current.StorageAccount.CreateCloudBlobClient().GetContainerReference("import-source");
            container.CreateIfNotExists();
            BlobToImport = container.GetBlockBlobReference(Guid.NewGuid().ToString("N"));
            BlobToImport.UploadFromStream(CreateStream("source"));
        };

        protected static CloudBlockBlob BlobToImport;
    }
}