using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore
{
    internal class DownloadUploadImportBlob : CloudIndexedBlob
    {
        readonly CloudBlockBlob _sourceBlob;

        public DownloadUploadImportBlob(CloudBlockBlob sourceBlob, string fileKey, IndexedBlobEntity indexRecord, IndexedBlobStorageOptions options, CloudIndexedBlobStore store, Dictionary<string, string> properties)
            : base(fileKey, indexRecord, options, store, properties)
        {
            _sourceBlob = sourceBlob;

            if (sourceBlob.Properties.Length == 0)
                sourceBlob.FetchAttributes();
            Length = sourceBlob.Properties.Length;
            FileName = sourceBlob.Name;
        }

        protected override void PerformUpload()
        {
            if (!Directory.Exists(Options.TemporaryDirectory))
                Directory.CreateDirectory(Options.TemporaryDirectory);

            var tempFile = Path.Combine(Options.TemporaryDirectory, Guid.NewGuid().ToString());
            try
            {
                ReliableCloudOperations.RetryRead(() => DownloadSource(tempFile));

                ReliableCloudOperations.RetryWrite(() => Blob.UploadFromFile(
                    tempFile,
                    FileMode.Open,
                    options: Options.BlobRequestOptions,
                    accessCondition: AccessConditions.CreateIfNotExists()));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        void DownloadSource(string tempFile)
        {
            using (var destinationStream = File.Create(tempFile))
            using (var blobStream = _sourceBlob.OpenRead(options: Options.BlobRequestOptions))
            {
                blobStream.CopyTo(destinationStream);
            }
        }
    }
}