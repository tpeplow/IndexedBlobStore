using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace IndexedBlobStore
{
    internal abstract class CloudIndexedBlob : IIndexedBlob
    {
        readonly Dictionary<string, string> _properties;
        readonly int _blobCount;
        readonly int _propertyCount;

        protected CloudIndexedBlob(string fileKey, IndexedBlobEntity entity, IndexedBlobStorageOptions options, CloudIndexedBlobStore cloudIndexedBlobStore, Dictionary<string, string> properties)
        {
            _properties = properties != null ? new Dictionary<string, string>(properties) : new Dictionary<string, string>();
            FileKey = fileKey;
            Exists = entity != null;
            _blobCount = entity != null ? entity.BlobCount : options.AdditionalBlobsForLoadBalancing + 1;
            Length = entity != null ? entity.Length : 0;
            _propertyCount = entity != null ? entity.PropertyCount : _properties.Count;
            Options = options;
            Store = cloudIndexedBlobStore;
        }

        public void Upload()
        {
            if (Exists)
                throw new BlobAlreadyExistsException(FileKey);

            var blobName = string.Format("{0}-0", FileKey);
            Blob = Store.Container.GetBlockBlobReference(blobName);
            Blob.StreamWriteSizeInBytes = Options.StreamWriteSizeInBytes;
            IgnoreConflict(PerformUpload);
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
            var entity = IndexedBlobTagEntity.Create(FileKey, tag, FileName, _blobCount, Compressed, Length, _propertyCount);
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
                Length = Length,
                FileName = FileName,
                PropertyCount = _propertyCount
            };
            try
            {
                var batch = new TableBatchOperation
                {
                    TableOperation.Insert(indexRecord)
                };

                if (_properties.Count > 0)
                {
                    var dynamicTableEntity = new DynamicTableEntity(FileKey, string.Format("prop::{0}", FileKey));
                    foreach (var property in _properties)
                    {
                        dynamicTableEntity.Properties.Add(property.Key, new EntityProperty(property.Value));
                    }
                    batch.Add(TableOperation.Insert(dynamicTableEntity));
                }
                Store.Table.ExecuteBatch(batch);
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
                IgnoreConflict(() => blobCopyManager.Start(newBlob, Blob));
            }

            blobCopyManager.WaitForCompletion();
        }

        void IgnoreConflict(Action action)
        {
            try
            {
                action();
            }
            catch (RetryWriteException ex)
            {
                var exception = ex.InnerException as StorageException;
                if (exception != null)
                    throw HandleStorageException(exception);

                throw;
            }
            catch (StorageException storageException)
            {
                throw HandleStorageException(storageException);
            }
        }

        Exception HandleStorageException(StorageException storageException)
        {
            switch (storageException.RequestInformation.HttpStatusCode)
            {
                case (int) HttpStatusCode.PreconditionFailed:
                    Exists = true;
                    return new BlobAlreadyExistsException(FileKey);
                case (int) HttpStatusCode.Conflict:
                    Exists = true;
                    return new BlobAlreadyExistsException(FileKey);
            }

            return storageException;
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