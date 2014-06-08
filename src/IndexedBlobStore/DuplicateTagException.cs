using System;

namespace IndexedBlobStore
{
    public class DuplicateTagException : Exception
    {
        public DuplicateTagException(string tagName, string fileKey)
            : base(string.Format("Blob with key {0} already has been tagged with tag {1}", fileKey, tagName))
        {
        }
    }
}