using System.Collections.Generic;
using System.Threading;
using Microsoft.WindowsAzure.Storage.Blob;

namespace IndexedBlobStore
{
    public class BlobCopyManager
    {
        readonly List<BlobCopyProgress> _blobsToCopy = new List<BlobCopyProgress>();

        public void Start(CloudBlockBlob target, CloudBlockBlob source)
        {
            var copyId = target.StartCopyFromBlob(source);
            _blobsToCopy.Add(new BlobCopyProgress(target, source, copyId));
        }

        public void WaitForCompletion()
        {
            while (_blobsToCopy.Count > 0)
            {
                foreach (var blobToCopy in _blobsToCopy.ToArray())
                {
                    blobToCopy.Target.FetchAttributes();
                    switch (blobToCopy.Target.CopyState.Status)
                    {
                        case CopyStatus.Success:
                            _blobsToCopy.Remove(blobToCopy);
                            break;
                        case CopyStatus.Aborted:
                        case CopyStatus.Failed:
                            _blobsToCopy.Remove(blobToCopy);
                            Start(blobToCopy.Target, blobToCopy.Source);
                            break;
                    }
                }
                Thread.Sleep(500);
            }
        }

        private class BlobCopyProgress
        {
            public CloudBlockBlob Target { get; private set; }
            public CloudBlockBlob Source { get; private set; }
            public string CopyId { get; private set; }

            public BlobCopyProgress(CloudBlockBlob target, CloudBlockBlob source, string copyId)
            {
                Target = target;
                Source = source;
                CopyId = copyId;
            }
        }
    }
}