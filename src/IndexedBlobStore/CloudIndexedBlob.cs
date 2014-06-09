using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace IndexedBlobStore
{
    internal abstract class CloudIndexedBlob : IIndexedBlob
    {
        readonly int _blobCount;

        protected CloudIndexedBlob(string fileKey, IndexedBlobEntity entity, IndexedBlobStorageOptions options, CloudIndexedBlobStore cloudIndexedBlobStore)
        {
            FileKey = fileKey;
            Exists = entity != null;
            _blobCount = entity != null ? entity.BlobCount : options.AdditionalBlobsForLoadBalancing + 1;
            Compressed = entity != null ? entity.Compressed : options.Compress;
            Length = entity != null ? entity.Length : 0;
            Options = options;
            Store = cloudIndexedBlobStore;
        }

        public void Upload()
        {
            if (Exists)
                throw new BlobAlreadyExistsException(FileKey);

            var blobName = string.Format("{0}-0", FileKey);
            Blob = Store.Container.GetBlockBlobReference(blobName);
            PerformUpload();
            DuplicateForLoadBalancing();
            InsertIndex();
        }

        protected abstract void PerformUpload();

        protected CloudBlockBlob Blob { get; private set; }
        protected IndexedBlobStorageOptions Options { get; private set; }        
        public string FileKey { get; private set; }
        public bool Exists { get; private set; }
        public long Length { get; protected set; }
        protected bool Compressed { get; set; }
        protected CloudIndexedBlobStore Store { get; private set; }
        public string FileName { get; protected set; }

        public void AddTag(string tag)
        {
            var entity = IndexedBlobTagEntity.Create(FileKey, tag, FileName, _blobCount, Compressed, Length);
            try
            {
                Store.Table.Execute(TableOperation.Insert(entity));
            }
            catch (StorageException storageException)
            {
                if (storageException.RequestInformation.HttpStatusCode == (int) HttpStatusCode.Conflict)
                {
                    throw new DuplicateTagException(tag, FileKey);
                }
                throw;
            }
        }
        
        void InsertIndex()
        {
            var indexRecord = new IndexedBlobEntity
            {
                PartitionKey = FileKey, 
                RowKey = FileKey, 
                BlobUri = Blob.Uri.ToString(),
                BlobCount = _blobCount,
                Compressed = Compressed,
                Length = Length,
                FileName = FileName
            };
            try
            {
                Store.Table.Execute(TableOperation.Insert(indexRecord));
            }
            catch (StorageException storageException)
            {
                if (storageException.RequestInformation.HttpStatusCode == (int) HttpStatusCode.Conflict)
                {
                    throw new BlobAlreadyExistsException(FileKey);
                }
                throw;
            }
        }

        void DuplicateForLoadBalancing()
        {
            var blobCopyManager = new BlobCopyManager();

            for (var i = 0; i < Options.AdditionalBlobsForLoadBalancing; i++)
            {
                var newBlob = Store.Container.GetBlockBlobReference(string.Format("{0}-{1}", FileKey, i + 1));
                blobCopyManager.Start(newBlob, Blob);
            }

            blobCopyManager.WaitForCompletion();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~CloudIndexedBlob()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}