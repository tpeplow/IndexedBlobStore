using System;
using System.Net;
using Microsoft.WindowsAzure.Storage;

namespace IndexedBlobStore
{
    public static class ReliableCloudOperations
    {
        static ReliableCloudOperations()
        {
            RetryCount = 5;
        }

        public static int RetryCount { get; set; }

        public static void Retry(Action upload)
        {
            var retryCount = 0;
            while (true)
            {
                try
                {
                    upload();
                    return;
                }
                catch (StorageException storageException)
                {
                    if (storageException.RequestInformation.HttpStatusCode != (int) HttpStatusCode.BadRequest || retryCount == RetryCount)
                    {
                        throw;
                    }
                    retryCount++;
                }
            }
        }
    }
}