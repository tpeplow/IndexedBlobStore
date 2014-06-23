using Microsoft.WindowsAzure.Storage;

namespace IndexedBlobStore
{
    internal class AccessConditions
    {
        public static AccessCondition CreateIfNotExists()
        {
            return new AccessCondition { IfNoneMatchETag = "*" };
        }
    }
}