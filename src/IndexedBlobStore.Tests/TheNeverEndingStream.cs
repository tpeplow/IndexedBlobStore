using System.IO;
using System.Threading.Tasks;

namespace IndexedBlobStore.Tests
{
    public class TheNeverEndingStream : Stream
    {
        private readonly TaskCompletionSource<object> _sync = new TaskCompletionSource<object>();

        public void Complete()
        {
            _sync.SetResult(null);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _sync.Task.Wait();
            return 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
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
            get { return 0; }
        }

        public override long Position
        {
            get { return 0; }
            set { throw new System.NotImplementedException(); }
        }
    }
}