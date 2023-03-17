using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.Message
{
    public class PreAllocByteArrayOutputStream : MemoryStream
    {
        private byte[] _buffer;
        private int _position;

        public PreAllocByteArrayOutputStream(byte[] buffer) : base(buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        public override void WriteByte(byte value)
        {
            if (_position >= _buffer.Length)
            {
                throw new IndexOutOfRangeException("Buffer is full");
            }
            _buffer[_position++] = value;
        }

        public int WrittenDataLength()
        {
            return _position;
        }

        public void Reset()
        {
            _position = 0;
        }

        public int WriteShort(int value)
        {
            int startIndex = _position;
            ReplaceShort(_position, value);
            _position += 2;
            return startIndex;
        }

        public void ReplaceShort(int index, int value)
        {
            _buffer[index++] = (byte)(value >> 8);
            _buffer[index++] = (byte)(value & 0xFF);
        }

        public int GetShort(int index)
        {
            if (index + 1 >= _buffer.Length)
            {
                throw new ArgumentException("Index is out of bounds");
            }

            return (_buffer[index] << 8) | (_buffer[index + 1] & 0xFF);
        }

        public byte[] Array()
        {
            return _buffer;

        }
    }
}
