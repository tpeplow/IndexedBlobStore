using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore
{
    internal class DownloadUploadImportBlob : CloudIndexedBlob
    {
        readonly CloudBlockBlob _sourceBlob;

        public DownloadUploadImportBlob(CloudBlockBlob sourceBlob, string fileKey, IndexedBlobEntity indexRecord, IndexedBlobStorageOptions options, CloudIndexedBlobStore store, Dictionary<string, string> properties)
            : base (fileKey, indexRecord, options, store, properties)
        {
            _sourceBlob = sourceBlob;
        }

        protected override void PerformUpload()
        {
            try
            {
                if (!Directory.Exists(Options.TemporaryDirectory))
                    Directory.CreateDirectory(Options.TemporaryDirectory);

                var tempFile = Path.Combine(Options.TemporaryDirectory, Guid.NewGuid().ToString());
                try
                {
                    _sourceBlob.DownloadToFile(tempFile, FileMode.Create, options: Options.BlobRequestOptions);
                    Blob.UploadFromFile(tempFile, FileMode.Open, options: Options.BlobRequestOptions);
                }
                finally
                {
                    File.Delete(tempFile);
                }
            }
            catch (StorageException storageException)
            {
                if (storageException.RequestInformation.HttpStatusCode != (int)HttpStatusCode.PreconditionFailed)
                    throw;
            }
        }
    }
}