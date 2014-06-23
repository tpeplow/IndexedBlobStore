using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.WindowsAzure.Storage;

namespace IndexedBlobStore
{
    internal class UploadableIndexedBlob : CloudIndexedBlob
    {
        Stream _stream;

        public UploadableIndexedBlob(string fileName, Stream stream, string fileKey, IndexedBlobEntity indexedBlobEntity, IndexedBlobStorageOptions options, CloudIndexedBlobStore cloudIndexedBlobStore, Dictionary<string, string> properties)
            : base(fileKey, indexedBlobEntity, options, cloudIndexedBlobStore, properties)
        {
            Length = stream.Length;
            _stream = stream;
            FileName = fileName;
        }

        protected override void PerformUpload()
        {
            _stream.EnsureAtStart();
            _stream = Store.Cache.Add(FileKey, _stream, Length);
            ReliableCloudOperations.UploadBlob(() =>
            {
                _stream.EnsureAtStart();
                Blob.UploadFromStream(
                    _stream,
                    options: Options.BlobRequestOptions,
                    accessCondition: AccessConditions.CreateIfNotExists());
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}