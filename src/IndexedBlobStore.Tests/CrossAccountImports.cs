﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IndexedBlobStore.Cache;
using Machine.Specifications;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore.Tests
{
    public class when_importing_a_blob_accross_storage_accounts_without_blob_copy : CrossAccountImportTests
    {
        Establish context = () => _store.DefaultStorageOptions.UseBlobCopyAccrossStorageAccounts = false;
        
        It should_copy = () =>
        {
            using (var stream = _importedBlob.OpenRead())
            using (var streamReader = new StreamReader(stream))
            {
                var contents = streamReader.ReadToEnd();
                contents.ShouldEqual("hello world");
            }
        };

        It should_set_length = () => _importedBlob.Length.ShouldNotEqual(0);
        It should_set_name = () => _importedBlob.FileName.ShouldEqual("source");
    }

    public class when_importing_a_blob_accross_storage_accounts_using_blob_copy : CrossAccountImportTests
    {
        Establish context = () => _store.DefaultStorageOptions.UseBlobCopyAccrossStorageAccounts = true;
        
        It should_copy = () =>
        {
            using (var stream = _importedBlob.OpenRead())
            using (var streamReader = new StreamReader(stream))
            {
                var contents = streamReader.ReadToEnd();
                contents.ShouldEqual("hello world");
            }
        };

        It should_set_length = () => _importedBlob.Length.ShouldNotEqual(0);
        It should_set_name = () => _importedBlob.FileName.ShouldEqual("source");
    }

    public class CrossAccountImportTests
    {
        Establish context = () =>
        {
            _containers = new List<CloudBlobContainer>();
            var config = XDocument.Load(@"c:\projects\StorageConfig.xml");
            _accounts = (from account in config.Root.Elements("account")
                         select new
                         {
                             Id = account.Attribute("id").Value,
                             Account = account.Attribute("name").Value,
                             Key = account.Attribute("key").Value
                         }).ToDictionary(x => x.Id, x => new CloudStorageAccount(new StorageCredentials(x.Account, x.Key), true));

            var blobClient = _accounts["acc1"].CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("a" + Guid.NewGuid().ToString("N"));
            container.Create();
            _sourceBlob = container.GetBlockBlobReference("source");
            _sourceBlob.UploadText("hello world");
            _containers.Add(container);

            var factory = new IndexedBlobStoreFactory(_accounts["acc2"]);
            _store = factory.Create("a" + Guid.NewGuid().ToString("N"),
                new IndexedBlobLocalCacheSettings
                {
                    CacheDirectory = Path.Combine(Path.GetTempPath(), "IndexedBlobStoreCache2")
                });
        };

        Because of = () =>
        {
            using (var blob = _store.ImportBlob("testing", _sourceBlob))
            {
                blob.Upload();
            }
            _importedBlob = _store.GetIndexedBlob("testing");
        };

        Cleanup clean = () =>
        {
            _store.Delete();
            foreach (var container in _containers)
                container.Delete();
        };

        static Dictionary<string, CloudStorageAccount> _accounts;
        static List<CloudBlobContainer> _containers;
        static CloudBlockBlob _sourceBlob;
        protected static IIndexedBlobStoreClient _store;
        protected static IReadonlyIndexedBlob _importedBlob;
    }
}