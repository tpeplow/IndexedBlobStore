using System;
using System.IO;
using IndexedBlobStore.Cache;

namespace IndexedBlobStore.Tests
{
    public class IndexedBlobStoreTest
    {
        protected static IIndexedBlobStoreClient Client { get { return TestContext.Current.Client; } }
        protected static IIndexedBlobCache Cache { get { return TestContext.Current.Cache; } }

        protected static Stream CreateStream(string contents)
        {
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            streamWriter.Write(contents);
            streamWriter.Flush();
            stream.Position = 0;
            return stream;
        }

        protected static string ReadStream(Stream stream)
        {
            using (stream)
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        protected static IIndexedBlob UploadUniqueBlob()
        {
            using (var blob = Client.CreateIndexedBlob(CreateStream(Guid.NewGuid().ToString())))
            {
                blob.Upload();
                return blob;
            }
        }
    }
}