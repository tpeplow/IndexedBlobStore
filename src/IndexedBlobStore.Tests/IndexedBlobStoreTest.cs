﻿using System;
using System.Collections.Generic;
using System.IO;
using IndexedBlobStore.Cache;

namespace IndexedBlobStore.Tests
{
    public class IndexedBlobStoreTest
    {
        protected static IIndexedBlobStoreClient Client { get { return TestContext.Current.Client; } }
        protected static IIndexedBlobCache Cache { get { return TestContext.Current.Cache; } }

        public static Stream CreateStream(string contents)
        {
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            streamWriter.Write(contents);
            streamWriter.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string ReadStream(Stream stream)
        {
            using (stream)
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        public static IIndexedBlob UploadUniqueBlob(Dictionary<string, string> properties = null)
        {
            using (var blob = Client.CreateIndexedBlob("unique.txt", CreateStream(Guid.NewGuid().ToString()), properties: properties))
            {
                blob.Upload();
                return blob;
            }
        }
    }
}