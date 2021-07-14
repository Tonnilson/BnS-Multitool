using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BnS_Multitool.Extensions
{
    /// <summary>
    /// Pre-prep for handling multi-parted archives
    /// Will be used to concat multiple file streams into one
    /// and plug the stream directly into the lzma decoder
    /// </summary>
    class ConcatStream : Stream
    {
        readonly Queue<Stream> streams;

        public ConcatStream(IEnumerable<Stream> streams)
        {
            this.streams = new Queue<Stream>(streams);
            Length = this.streams.Sum(x => x.Length);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (count > 0 && streams.Count > 0)
            {
                int bytesRead = streams.Peek().Read(buffer, offset, count);
                if (bytesRead == 0)
                {
                    streams.Dequeue().Dispose();
                    continue;
                }

                totalBytesRead += bytesRead;
                offset += bytesRead;
                count -= bytesRead;
            }

            return totalBytesRead;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                while (streams.Count > 0)
                    streams.Dequeue().Dispose();

            base.Dispose(disposing);
        }

        public override void Flush()
        {
        }

        public override long Length { get; }
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
