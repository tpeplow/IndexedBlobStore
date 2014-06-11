using System.IO;
using System.IO.Compression;
using System.Net;
using Microsoft.WindowsAzure.Storage;

namespace IndexedBlobStore
{
    internal class UploadableIndexedBlob : CloudIndexedBlob
    {
        readonly Stream _stream;

        public UploadableIndexedBlob(string fileName, Stream stream, string fileKey, IndexedBlobEntity indexedBlobEntity,
            IndexedBlobStorageOptions options, CloudIndexedBlobStore cloudIndexedBlobStore)
            : base(fileKey, indexedBlobEntity, options, cloudIndexedBlobStore)
        {
            Length = stream.Length;
            _stream = cloudIndexedBlobStore.Cache.Add(fileKey, stream, Length);
            FileName = fileName;
        }

        protected override void PerformUpload()
        {
            try
            {
                ReliableCloudOperations.UploadBlob(() =>
                {
                    _stream.EnsureAtStart();
                    using (var stream = GetBlobStream())
                    {
                        _stream.CopyTo(stream);
                    }
                });
            }
            catch (StorageException storageException)
            {
                if (storageException.RequestInformation.HttpStatusCode != (int) HttpStatusCode.PreconditionFailed)
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