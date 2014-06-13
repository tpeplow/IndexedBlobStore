using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
            try
            {
                _stream.EnsureAtStart();
                _stream = Store.Cache.Add(FileKey, _stream, Length);
                ReliableCloudOperations.UploadBlob(() =>
                {
                    using (var stream = GetBlobStream())
                    {
                        _stream.CopyTo(stream);
                    }
                });
            }
            catch (StorageException storageException)
            {
                if (storageException.RequestInformation.HttpStatusCode != (int)HttpStatusCode.PreconditionFailed)
                    throw;
            }
        }

        Stream GetBlobStream()
        {
            Stream blobStream = Blob.OpenWrite();
            if (Options.Compress)
                blobStream = new GZipStream(blobStream, CompressionMode.Compress);
            return blobStream;
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