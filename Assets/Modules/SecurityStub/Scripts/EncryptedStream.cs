using System.IO;

namespace Security
{
    public class EncryptedReadStream : Stream
    {
        private readonly Stream _stream;

        private byte _checksum = 0;
        private byte _checksumExpected = 0;
        private uint _w;
        private uint _z;
        private readonly int _length;
        private byte _lastByte = 0;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _length;

        public EncryptedReadStream(Stream stream, int length)
        {
            _stream = stream;
            _w = 0x12345678 ^ (uint)(length - 1);
            _z = 0x87654321 ^ (uint)(length - 1);
            _length = length;
        }

        public override int ReadByte()
        {
            int byteValue = _stream.ReadByte();
            if (byteValue == -1)
            {
                // TODO: Implement  correct checksum algorithm
                _checksum = (byte)(_checksum ^ (byte)random(ref _w, ref _z));
                if (_checksum != _checksumExpected)
                {
                    //throw new System.InvalidOperationException("Checksum mismatch");
                }
                return byteValue;
            }

            _checksumExpected = (byte)byteValue;

            byte decryptedByte = (byte)(byteValue ^ (byte)random(ref _w, ref _z));
            _checksum += decryptedByte;

            return decryptedByte;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var advanced =  _stream.Read(buffer, offset, count);

            if (advanced > 0)
            {
                _checksumExpected = buffer[offset + advanced - 1];
            }

            for (int i = 0; i < advanced; i++)
            {
                buffer[offset + i] = (byte)(buffer[offset + i] ^ (byte)random(ref _w, ref _z));
                _checksum += buffer[offset + i];
            }

            if (advanced < count || advanced == 0)
            {
                // TODO: Implement  correct checksum algorithm
                _checksum = (byte)(_checksum ^ (byte)random(ref _w, ref _z));
                if (_checksum != _checksumExpected)
                {
                    //throw new System.InvalidOperationException("Checksum mismatch");
                }
            }

            return advanced;
        }
        private static uint random(ref uint w, ref uint z)
        {
            z = 36969 * (z & 65535) + (z >> 16);
            w = 18000 * (w & 65535) + (w >> 16);
            return (z << 16) + w; /* 32-bit result */
        }

        public override long Position
        {
            get => _stream.Position;
            set => throw new System.InvalidOperationException();
        }

        public override void Flush() => throw new System.InvalidOperationException();
        public override long Seek(long offset, SeekOrigin origin) => throw new System.InvalidOperationException();
        public override void SetLength(long value) => throw new System.InvalidOperationException();
        public override void Write(byte[] buffer, int offset, int count) => throw new System.InvalidOperationException();
    }
}
