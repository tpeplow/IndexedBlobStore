﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Machine.Specifications;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore.Tests
{
    [Subject("412 Errors")]
    public class when_blob_changes_between_opening_and_reading : ImportTests
    {
        public class when_failures_less_than_retry_count : when_blob_changes_between_opening_and_reading
        {
            Establish context = () => _errorCount = 1;
            Because of = DownloadingDuringUpdate;
            It should_NOT_throw = () => _exception.ShouldBeNull();
        }

        public class when_failures_exceed_retry_count : when_blob_changes_between_opening_and_reading
        {
            Establish context = () => _errorCount = 5;
            Because of = DownloadingDuringUpdate;
            It should_throw = () => _exception.ShouldNotBeNull();
        }

        static void DownloadingDuringUpdate()
        {
            _exception = Catch.Exception(() =>
            {
                ReliableCloudOperations.Retry(DownloadSource);
            });
        }

        private static void DownloadSource()
        {
            using (var destinationStream = new MemoryStream())
            {
                using (var blobStream = BlobToImport.OpenRead(options: new IndexedBlobStorageOptions().BlobRequestOptions))
                {
                    // Make an independent change to the source blob to cause a 412
                    if (--_errorCount > 0) BlobToImport.SetProperties();
                    blobStream.CopyTo(destinationStream);
                }
            }
        }

        static Exception _exception;
        static int _errorCount;
    }

    public class when_importing_blob_that_does_not_exist : ImportTests
    {
        Establish context = () => _importedBlob = Client.ImportBlob(BlobToImport);
        Because of = () =>
        {
            _importedBlob.Upload();
            _downloadedBlob = Client.GetIndexedBlob(_importedBlob.FileKey);
        };
        It can_be_looked_up_by_key = () => _downloadedBlob.ShouldNotBeNull();
        It can_download_contents = () => ReadStream(_downloadedBlob.OpenRead()).ShouldEqual("source");
        It should_set_the_size = () => _importedBlob.Length.ShouldEqual(6);
        It should_use_the_source_blob_name_as_filename = () => _importedBlob.FileName.ShouldEqual(BlobName);
        It should_add_the_etag_to_the_filekey = () => _downloadedBlob.FileKey.ShouldContain(BlobToImport.Properties.ETag);
        It should_add_the_container_to_the_filekey = () => _downloadedBlob.FileKey.ShouldContain(BlobToImport.Container.Name);
        It should_add_the_blob_name_to_the_filekey = () => _downloadedBlob.FileKey.ShouldContain(BlobToImport.Name);

        static IIndexedBlob _importedBlob;
        static IReadonlyIndexedBlob _downloadedBlob;
    }

    public class when_importing_blob_that_does_not_exist_with_properties : ImportTests
    {
        Establish context = () => _importedBlob = Client.ImportBlob(BlobToImport, properties: new Dictionary<string, string> { { "name", "dave" } });
        Because of = () =>
        {
            _importedBlob.Upload();
            _downloadedBlob = Client.GetIndexedBlob(_importedBlob.FileKey);
        };
        It should_include_properties = () => _downloadedBlob.Properties["name"].ShouldEqual("dave");

        static IIndexedBlob _importedBlob;
        static IReadonlyIndexedBlob _downloadedBlob;
    }

    public class when_importing_the_same_blob_again : ImportTests
    {
        Establish context = () =>
        {
            using (var importedBlob = Client.ImportBlob(BlobToImport))
                importedBlob.Upload();
        };

        Because of = () => _exception = Catch.Exception(() =>
        {
            using (var importedBlob = Client.ImportBlob(BlobToImport))
                importedBlob.Upload();
        });

        It should_throw_blob_already_exists_exception = () => _exception.ShouldBeOfExactType<BlobAlreadyExistsException>();

        static Exception _exception;
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
            BlobName = Guid.NewGuid().ToString("N");
            BlobToImport = container.GetBlockBlobReference(BlobName);
            BlobToImport.UploadFromStream(CreateStream("source"));
        };

        protected static CloudBlockBlob BlobToImport;
        protected static string BlobName;
    }
}