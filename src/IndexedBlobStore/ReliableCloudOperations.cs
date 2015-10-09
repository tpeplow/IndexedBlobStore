using System;
using System.Net;
using System.Runtime.Serialization;
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

        public static void RetryWrite(Action upload)
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
                        throw new RetryWriteException("Exceeded retries on a write", storageException);
                    }
                    retryCount++;
                }
            }
        }

        public static void RetryRead(Action upload)
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
                    if (storageException.RequestInformation.HttpStatusCode != (int)HttpStatusCode.BadRequest || retryCount == RetryCount)
                    {
                        throw new RetryReadException("Exceeded retries on a read", storageException);
                    }
                    retryCount++;
                }
            }
        }
    }

    [Serializable]
    public class RetryReadException : Exception
    {
        public RetryReadException()
        {
        }

        public RetryReadException(string message) : base(message)
        {
        }

        public RetryReadException(string message, Exception inner) : base(message, inner)
        {
        }

        protected RetryReadException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class RetryWriteException : Exception
    {
        public RetryWriteException()
        {
        }

        public RetryWriteException(string message) : base(message)
        {
        }

        public RetryWriteException(string message, Exception inner) : base(message, inner)
        {
        }

        protected RetryWriteException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}