using System;
using System.IO;

namespace IndexedBlobStore
{
    public static class StreamHelpers
    {
        public static void EnsureAtStart(this Stream stream)
        {
            if (stream.Position > 0)
            {
                if (!stream.CanSeek)
                    throw new NotSupportedException("Cannot upload from a non-seekable stream that is not at stream position 0");
                stream.Position = 0;
            }
        }
    }
}