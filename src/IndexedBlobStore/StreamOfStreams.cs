using System;
using System.IO;

namespace IndexedBlobStore
{
    internal class StreamOfStreams : Stream
    {
        readonly Stream[] _sourceStreams;
        int _currentStream;

        public StreamOfStreams(params Stream[] sourceStreams)
        {
            _sourceStreams = sourceStreams;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readBytes = 0;

            while (_currentStream < _sourceStreams.Length && count > 0)
            {
                var stream = _sourceStreams[_currentStream];
                var lastRead = stream.Read(buffer, offset, count);
                count = count - lastRead;
                offset = offset + lastRead;

                readBytes += lastRead;
                if (lastRead == 0)
                    _currentStream++;
            }

            return readBytes;

        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }
    }
}