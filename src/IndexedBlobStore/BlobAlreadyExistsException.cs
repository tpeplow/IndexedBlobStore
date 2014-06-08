using System;

namespace IndexedBlobStore
{
    public class BlobAlreadyExistsException : Exception
    {
        public BlobAlreadyExistsException(string fileKey) : base(string.Format("A blob with key {0} has already been uploaded", fileKey))
        {
        }
    }
}