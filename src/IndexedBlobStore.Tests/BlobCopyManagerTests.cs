using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using IndexedBlobStore.Cache;
using Machine.Specifications;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore.Tests
{
    [Subject("BlobCopyManager")]
    public class BlobCopyManagerTests
    {
        public class when_copying_within_same_storage_account
        {
            Establish context = () =>
            {
                _destBlob = _sourceContainer.GetBlockBlobReference(Guid.NewGuid().ToString("N"));
            };

            Because of = () =>
            {
                _manager.Start(_destBlob, _sourceBlob);
                _manager.WaitForCompletion();
            };

            It should_copy_blob = () =>
            {
                _destBlob.Exists().ShouldBeTrue();
                _destBlob.Properties.ETag.ShouldNotBeNull();
            };

            static CloudBlockBlob _destBlob;
        }

        // Note: this could take up to 2 weeks to complete!
        public class when_copying_between_storage_accounts
        {
            Establish context = () =>
            {
                _destBlob = _destContainer.GetBlockBlobReference(Guid.NewGuid().ToString("N"));
            };

            Because of = () =>
            {
                _manager.Start(_destBlob, _sourceBlob);
                _manager.WaitForCompletion();
            };

            It should_copy_blob = () =>
            {
                _destBlob.Exists().ShouldBeTrue();
                _destBlob.Properties.ETag.ShouldNotBeNull();
            };

            static CloudBlockBlob _destBlob;
        }

        public class when_copying_same_blob_twice
        {
            Establish context = () =>
            {
                _destBlob = _destContainer.GetBlockBlobReference(Guid.NewGuid().ToString("N"));
            };

            Because of = () =>
            {
                _manager.Start(_destBlob, _sourceBlob);
                _exception = Catch.Exception(() => _manager.Start(_destBlob, _sourceBlob));
                _manager.WaitForCompletion();
            };

            It should_copy_blob = () =>
            {
                _destBlob.Exists().ShouldBeTrue();
                _destBlob.Properties.ETag.ShouldNotBeNull();
            };

            It should_report_conflict_on_second_copy = () =>
            {
                _exception.ShouldBeOfExactType<StorageException>();
                var storageException = _exception as StorageException;
                storageException.RequestInformation.HttpStatusCode.ShouldEqual((int)HttpStatusCode.Conflict);
            };

            static CloudBlockBlob _destBlob;
            static Exception _exception;
        }

        public class when_source_blob_not_exist
        {
            Establish context = () =>
            {
                _notExistSourceBlob = _sourceContainer.GetBlockBlobReference(Guid.NewGuid().ToString("N"));
                _destBlob = _destContainer.GetBlockBlobReference(Guid.NewGuid().ToString("N"));
            };

            Because of = () =>
            {
                _exception = Catch.Only<StorageException>(() => _manager.Start(_destBlob, _notExistSourceBlob));
            };

            It should_throw = () =>
            {
                _exception.RequestInformation.HttpStatusCode.ShouldEqual((int) HttpStatusCode.NotFound);
            };

            static CloudBlockBlob _destBlob;
            static StorageException _exception;
            static CloudBlockBlob _notExistSourceBlob;
        }

        public class when_destination_blob_already_exists
        {
            Establish context = () =>
            {
                _destBlob = _destContainer.GetBlockBlobReference(Guid.NewGuid().ToString("N"));
                _destBlob.UploadText("Original Text");
                _etag = _destBlob.Properties.ETag;
            };

            Because of = () =>
            {
                _exception = Catch.Only<StorageException>(() =>_manager.Start(_destBlob, _sourceBlob));
            };

            It should_NOT_overwrite_blob = () =>
            {
                _destBlob.FetchAttributes();
                _destBlob.Properties.ETag.ShouldEqual(_etag);
                _destBlob.DownloadText().ShouldEqual("Original Text");
            };

            It should_throw_409_Conflict_exception = () => _exception.RequestInformation.HttpStatusCode.ShouldEqual((int) HttpStatusCode.Conflict);

            static CloudBlockBlob _destBlob;
            static string _etag;
            static StorageException _exception;
        }

        Establish context = () =>
        {
            var config = XDocument.Load(@"c:\projects\StorageConfig.xml");
            _accounts = (from account in config.Root.Elements("account")
                select new
                {
                    Id = account.Attribute("id").Value,
                    Account = account.Attribute("name").Value,
                    Key = account.Attribute("key").Value
                }).ToDictionary(x => x.Id, x => new CloudStorageAccount(new StorageCredentials(x.Account, x.Key), true));

            var sourceClient = _accounts["acc1"].CreateCloudBlobClient();
            _sourceContainer = sourceClient.GetContainerReference("a" + Guid.NewGuid().ToString("N"));
            _sourceContainer.Create();
            _sourceBlob = _sourceContainer.GetBlockBlobReference("source");
            _sourceBlob.UploadText("hello world");

            var destClient = _accounts["acc2"].CreateCloudBlobClient();
            _destContainer = destClient.GetContainerReference("b" + Guid.NewGuid().ToString("N"));
            _destContainer.Create();

            _manager = new BlobCopyManager();
        };

        Cleanup clean = () =>
        {
            _sourceContainer.Delete();
            _destContainer.Delete();
        };

            static BlobCopyManager _manager;
        static Dictionary<string, CloudStorageAccount> _accounts;
        static CloudBlobContainer _sourceContainer;
        static CloudBlobContainer _destContainer;
        static CloudBlockBlob _sourceBlob;
    }

}
