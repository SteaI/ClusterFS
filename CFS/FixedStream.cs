using System;
using System.IO;
using System.Text;

namespace CFS
{
    /// <summary>
    /// 바이트 시퀀스에 대한 제한된 뷰를 제공합니다.
    /// </summary>
    public abstract class FixedStream
    {
        private const int LargeByteBufferSize = 256;

        private byte[] _largeByteBuffer = new byte[LargeByteBufferSize];
        private byte[] _buffer = new byte[4];
        private Encoder _encoder = Encoding.UTF8.GetEncoder();

        /// <summary>
        /// 내부 스트림을 보유합니다.
        /// </summary>
        protected Stream Stream { get; }

        /// <summary>
        /// 내부 스트림에대한 BinaryWriter를 제공합니다.
        /// </summary>
        protected BinaryWriter Writer { get; }

        /// <summary>
        /// 내부 스트림에대한 BinaryWriter를 제공합니다.
        /// </summary>
        protected BinaryReader Reader { get; }

        internal long Position { get; set; }
        internal long Size { get; set; }

        /// <summary>
        /// 고정된 스트림의 크기입니다.
        /// </summary>
        public long Length
        {
            get
            {
                return Size;
            }
        }

        /// <summary>
        /// 사용 가능한 스트림길이를 가져옵니다.
        /// </summary>
        public int Remain
        {
            get
            {
                return (int)(Size - (Stream.Position - this.Position));
            }
        }

        internal FixedStream(CFSStream stream)
        {
            this.Stream = stream.BaseStream;
            this.Writer = stream.writer;
            this.Reader = stream.reader;

            Size = stream.BaseStream.Length;
        }

        internal FixedStream(Stream stream)
        {
            this.Stream = stream;
            this.Writer = new BinaryWriter(stream);
            this.Reader = new BinaryReader(stream);

            this.Size = stream.Length;
        }

        internal void SetRange(long position, long size, bool seek = true)
        {
            this.Position = position;
            this.Size = size;

            if (seek)
                Stream.Seek(position, SeekOrigin.Begin);
        }

        /// <summary>
        /// 현재 스트림 내의 위치를 설정합니다.
        /// </summary>
        /// <param name="position">스트림 내의 새 위치입니다.</param>
        public void Seek(long position)
        {
            Stream.Seek(Position + position, SeekOrigin.Begin);
        }

        /// <summary>
        /// 할당된 스트림 영역을 0으로 채웁니다.
        /// </summary>
        public void Clear()
        {
            Seek(0);
            Write(new byte[Size]);
        }
        
        #region Reader
        private T CheckReadError<T>(T value)
        {
            if (this.Remain > Size)
                throw new EndOfStreamException();

            return value;
        }

        /// <summary>
        /// 사용할 수 있는 다음 문자를 반환하고 바이트 또는 문자 위치를 앞으로 이동하지 않습니다.
        /// </summary>
        /// <returns>사용할 수 있는 다음 문자를 반환하고 사용할 수 있는 문자가 더 이상 없거나 스트림에서 검색을 지원하지 않을 경우 -1을 반환합니다.</returns>
        public int PeekChar()
        {
            return CheckReadError(Reader.PeekChar());
        }

        /// <summary>
        /// 원본 스트림에서 문자를 읽고 사용된 Encoding과 스트림에서 읽어오는 특정 문자의 길이만큼 스트림의 현재 위치를 앞으로  이동합니다.
        /// </summary>
        /// <returns>현재 사용할 수 있는 문자가 없으면 입력 스트림의 다음 문자 또는 -1입니다.</returns>
        public int Read()
        {
            return CheckReadError(Reader.Read());
        }

        /// <summary>
        /// 바이트 배열의 지정된 지점부터 스트림에서 지정된 바이트 수만큼 읽습니다.
        /// </summary>
        /// <param name="buffer">데이터를 읽어올 버퍼입니다.</param>
        /// <param name="index">버퍼로 읽어오기를 시작할 버퍼의 시작 위치입니다.</param>
        /// <param name="count">읽을 바이트의 수입니다.</param>
        /// <returns>buffer로 읽어 온 바이트 수입니다. 이 문자 수는 바이트가 충분하지 않은 경우 요청된 바이트 수보다 작을 수 있 으며 스트림의 끝에 도달하면 0이 됩니다.</returns>
        public int Read(byte[] buffer, int index, int count)
        {
            return CheckReadError(Reader.Read());
        }

        /// <summary>
        /// 문자 배열의 지정된 지점부터 스트림에서 지정된 문자 수만큼 읽습니다.
        /// </summary>
        /// <param name="buffer">데이터를 읽어올 버퍼입니다.</param>
        /// <param name="index">버퍼로 읽어오기를 시작할 버퍼의 시작 위치입니다.</param>
        /// <param name="count">읽을 문자 수입니다.</param>
        /// <returns>버퍼로 읽어온 총 문자 수입니다. 이 문자 수는 문자가 현재 충분하지 않은 경우 요청된 문자 수보다 작을 수 있으며 스트림의 끝에 도달하면 0이 됩니다.</returns>
        public int Read(char[] buffer, int index, int count)
        {
            return CheckReadError(Reader.Read());
        }

        /// <summary>
        /// 현재 스트림에서 Boolean 값을 읽고 스트림의 현재 위치를 1바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>바이트가 0이 아니면 true이고, 그렇지 않으면 false입니다.</returns>
        public bool ReadBoolean()
        {
            return CheckReadError(Reader.ReadBoolean());
        }

        /// <summary>
        /// 현재 스트림에서 다음 바이트를 읽고 스트림의 현재 위치를 1바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 다음 바이트입니다.</returns>
        public byte ReadByte()
        {
            return CheckReadError(Reader.ReadByte());
        }

        /// <summary>
        /// 지정된 바이트 수만큼 현재 스트림에서 바이트 배열로 읽어 오고 현재 위치를 해당 바이트 수만큼 앞으로 이동합니다.
        /// </summary>
        /// <param name="count">읽을 바이트의 수입니다.</param>
        /// <returns>원본 스트림에서 읽은 데이터를 포함하는 바이트 배열입니다. 이 바이트 배열은 스트림의 끝에 도달할 경우 요청된 바이트 수보다 작을 수 있습니다.</returns>
        public byte[] ReadBytes(int count)
        {
            return CheckReadError(Reader.ReadBytes(count));
        }

        /// <summary>
        /// 현재 스트림에서 다음 문자를 읽고 사용된 Encoding과 스트림에서 읽어오는 특정 문자의 길이만큼 스트림의 현재 위치를 앞 으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 문자입니다.</returns>
        public char ReadChar()
        {
            return CheckReadError(Reader.ReadChar());
        }

        /// <summary>
        /// 현재 스트림에서 지정된 문자 수만큼 읽어 문자 배열로 데이터를 반환하고, 사용된 Encoding과 스트림에서 읽어 오는 특정  문자의 길이만큼
        /// </summary>
        /// <param name="count">읽을 문자 수입니다.</param>
        /// <returns>원본 스트림에서 읽어온 데이터를 포함하는 문자 배열입니다. 스트림의 끝에 도달할 경우 이 문자 배열은 요청된  바이트 수보다 작을 수 있습니다.</returns>
        public char[] ReadChars(int count)
        {
            return CheckReadError(Reader.ReadChars(count));
        }

        /// <summary>
        /// 현재 스트림에서 10진 값을 읽고 스트림의 현재 위치를 16바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 10진 값입니다.</returns>
        public decimal ReadDecimal()
        {
            return CheckReadError(Reader.ReadDecimal());
        }

        /// <summary>
        /// 현재 스트림에서 8바이트 부동 소수점 값을 읽고 스트림의 현재 위치를 8바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 8바이트 부동 소수점 값입니다.</returns>
        public double ReadDouble()
        {
            return CheckReadError(Reader.ReadDouble());
        }

        /// <summary>
        /// 현재 스트림에서 부호 있는 2바이트 정수를 읽고 스트림의 현재 위치를 2바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 부호 있는 2바이트 정수입니다.</returns>
        public short ReadInt16()
        {
            return CheckReadError(Reader.ReadInt16());
        }

        /// <summary>
        /// 현재 스트림에서 부호 있는 4바이트 정수를 읽고 스트림의 현재 위치를 4바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 부호 있는 4바이트 정수입니다.</returns>
        public int ReadInt32()
        {
            return CheckReadError(Reader.ReadInt32());
        }

        /// <summary>
        /// 현재 스트림에서 부호 있는 8바이트 정수를 읽고 스트림의 현재 위치를 8바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 부호 있는 8바이트 정수입니다.</returns>
        public long ReadInt64()
        {
            return CheckReadError(Reader.ReadInt64());
        }

        /// <summary>
        /// 현재 스트림에서 부호 있는 바이트를 읽고 스트림의 현재 위치를 1바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 부호 있는 바이트입니다.</returns>
        public sbyte ReadSByte()
        {
            return CheckReadError(Reader.ReadSByte());
        }

        /// <summary>
        /// 현재 스트림에서 4바이트 부동 소수점 값을 읽고 스트림의 현재 위치를 4바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 4바이트 부동 소수점 값입니다.</returns>
        public float ReadSingle()
        {
            return CheckReadError(Reader.ReadSingle());
        }

        /// <summary>
        /// 현재 스트림에서 문자열을 읽습니다. 한 번에 7비트 정수로 인코딩된 문자열 길이는 해당 문자열 앞에 옵니다.
        /// </summary>
        /// <returns>읽는 중인 문자열입니다.</returns>
        public string ReadString()
        {
            return CheckReadError(Reader.ReadString());
        }

        /// <summary>
        /// little-endian 인코딩을 사용하여 현재 스트림에서 부호 없는 2바이트 정수를 읽고 스트림의 위치를 2바이트씩 앞으로 이동 합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 부호 없는 2바이트 정수입니다.</returns>
        public ushort ReadUInt16()
        {
            return CheckReadError(Reader.ReadUInt16());
        }

        /// <summary>
        /// 현재 스트림에서 부호 없는 4바이트 정수를 읽고 스트림의 위치를 4바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 부호 없는 4바이트 정수입니다.</returns>
        public uint ReadUInt32()
        {
            return CheckReadError(Reader.ReadUInt32());
        }

        /// <summary>
        /// 현재 스트림에서 부호 없는 8바이트 정수를 읽고 스트림의 위치를 8바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <returns>현재 스트림에서 읽은 부호 없는 8바이트 정수입니다.</returns>
        public ulong ReadUInt64()
        {
            return CheckReadError(Reader.ReadUInt64());
        }
        #endregion

        #region Writer
        /// <summary>
        /// 데이터를 쓰기전 고정된 스트림에 대한 남은 바이트수를 계산하여 예외를 검출합니다.
        /// </summary>
        /// <param name="size">데이터의 바이트수 입니다.</param>
        protected virtual void CheckWriteError(int size)
        {
            if (size > Remain)
                throw new EndOfStreamException();
        }
        
        /// <summary>
        /// 문자 배열을 현재 스트림에 쓴 다음 사용된 Encoding과 스트림에 쓰여지는 특정 문자의 길이만큼 스트림의 현재 위치를 앞으로 이동합니다.
        /// </summary>
        /// <param name="chars">쓸 데이터를 포함하는 문자 배열입니다.</param>
        /// <returns></returns>
        public void Write(char[] chars)
        {
            CheckWriteError(Encoding.UTF8.GetByteCount(chars));
            Writer.Write(chars);
        }

        /// <summary>
        /// 8바이트 부호 있는 정수를 현재 스트림에 쓰고 스트림 위치를 8바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 8바이트 부호 있는 정수입니다.</param>
        /// <returns></returns>
        public void Write(long value)
        {
            CheckWriteError(8);
            Writer.Write(value);
        }

        /// <summary>
        /// 4바이트 부호 없는 정수를 현재 스트림에 쓰고 스트림 위치를 4바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 4바이트 부호 없는 정수입니다.</param>
        /// <returns></returns>
        public void Write(uint value)
        {
            CheckWriteError(4);
            Writer.Write(value);
        }

        /// <summary>
        /// 4바이트 부호 있는 정수를 현재 스트림에 쓰고 스트림 위치를 4바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 4바이트 부호 있는 정수입니다.</param>
        /// <returns></returns>
        public void Write(int value)
        {
            CheckWriteError(4);
            Writer.Write(value);
        }

        /// <summary>
        /// 2바이트 부호 없는 정수를 현재 스트림에 쓰고 스트림 위치를 2바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 2바이트 부호 없는 정수입니다.</param>
        /// <returns></returns>
        public void Write(ushort value)
        {
            CheckWriteError(2);
            Writer.Write(value);
        }

        /// <summary>
        /// 2바이트 부호 있는 정수를 현재 스트림에 쓰고 스트림 위치를 2바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 2바이트 부호 있는 정수입니다.</param>
        /// <returns></returns>
        public void Write(short value)
        {
            CheckWriteError(2);
            Writer.Write(value);
        }

        /// <summary>
        /// 10진 값을 현재 스트림에 쓰고 스트림 위치를 16바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">출력할 10진수 값입니다.</param>
        /// <returns></returns>
        public void Write(decimal value)
        {
            CheckWriteError(16);
            Writer.Write(value);
        }

        /// <summary>
        /// 8바이트 부동 소수점 값을 현재 스트림에 쓰고 스트림 위치를 8바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 8바이트 부동 소수점 값입니다.</param>
        /// <returns></returns>
        public void Write(double value)
        {
            CheckWriteError(8);
            Writer.Write(value);
        }

        /// <summary>
        /// 4바이트 부동 소수점 값을 현재 스트림에 쓰고 스트림 위치를 4바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 4바이트 부동 소수점 값입니다.</param>
        /// <returns></returns>
        public void Write(float value)
        {
            CheckWriteError(4);
            Writer.Write(value);
        }

        /// <summary>
        /// 유니코드 문자를 현재 스트림에 쓴 다음 사용된 Encoding과 스트림에 쓰여지는 특정 문자의 길이만큼 스트림의 현재 위치를 앞으로 이동합니다.
        /// </summary>
        /// <param name="ch">쓰려고 하는 서로게이트가 아닌 유니코드 문자입니다.</param>
        /// <returns></returns>
        public unsafe void Write(char ch)
        {
            int numBytes = 0;
            fixed (byte* pBytes = _buffer)
            {
                numBytes = _encoder.GetBytes(&ch, 1, pBytes, _buffer.Length, true);
            }

            CheckWriteError(numBytes);
            Writer.Write(ch);
        }

        /// <summary>
        /// 이 스트림에 문자열의 길이가 맨 앞에 나오는 문자열을 쓴 다음 사용된 인코 딩과
        /// 스트림에 쓰여지는 특정 문자의 길이만큼 스트림의 현재 위치를 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 값입니다.</param>
        /// <returns></returns>
        public unsafe void Write(string value)
        {
            CheckWriteError(GetMSStringByteCount(value));
            Writer.Write(value);
        }

        /// <summary>
        /// 내부 스트림에 바이트 배열을 씁니다.
        /// </summary>
        /// <param name="buffer">쓸 데이터를 포함하는 바이트 배열입니다.</param>
        /// <returns></returns>
        public void Write(byte[] buffer)
        {
            CheckWriteError(buffer.Length);
            Writer.Write(buffer);
        }

        /// <summary>
        /// 부호 있는 바이트를 현재 스트림에 쓰고 스트림 위치를 1바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 부호 있는 바이트입니다.</param>
        /// <returns></returns>
        public void Write(sbyte value)
        {
            CheckWriteError(1);
            Writer.Write(value);
        }

        /// <summary>
        /// 부호 없는 바이트를 현재 스트림에 쓰고 스트림 위치를 1바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 부호 없는 바이트입니다.</param>
        /// <returns></returns>
        public void Write(byte value)
        {
            CheckWriteError(1);
            Writer.Write(value);
        }

        /// <summary>
        /// false를 나타내는 0과 true를 나타내는 1을 사용하여 1바이트 Boolean 값을 현재 스트림에 씁니다.
        /// </summary>
        /// <param name="value">쓸 Boolean 값(0 또는 1)입니다.</param>
        /// <returns></returns>
        public void Write(bool value)
        {
            CheckWriteError(1);
            Writer.Write(value);
        }

        /// <summary>
        /// 8바이트 부호 없는 정수를 현재 스트림에 쓰고 스트림 위치를 8바이트씩 앞으로 이동합니다.
        /// </summary>
        /// <param name="value">쓸 8바이트 부호 없는 정수입니다.</param>
        /// <returns></returns>
        public void Write(ulong value)
        {
            CheckWriteError(8);
            Writer.Write(value);
        }

        /// <summary>
        /// 문자 배열 섹션을 현재 스트림에 쓴 다음 사용된 Encoding과 스트림에 쓰여지는 특정 문자의 길이만큼 스트림의 현재 위치를 앞으로 이동합니다.
        /// </summary>
        /// <param name="chars">쓸 데이터를 포함하는 문자 배열입니다.</param>
        /// <param name="index">쓰기를 시작할 chars의 시작점입니다.</param>
        /// <param name="count">쓸 문자 수입니다.</param>
        /// <returns></returns>
        public void Write(char[] chars, int index, int count)
        {
            CheckWriteError(count);
            Writer.Write(chars, index, count);
        }

        /// <summary>
        /// 현재 스트림에 바이트 배열 영역을 씁니다.
        /// </summary>
        /// <param name="buffer">쓸 데이터를 포함하는 바이트 배열입니다.</param>
        /// <param name="index">쓰기를 시작할 buffer의 시작점입니다.</param>
        /// <param name="count">쓸 바이트 수입니다.</param>
        /// <returns></returns>
        public void Write(byte[] buffer, int index, int count)
        {
            CheckWriteError(count);
            Writer.Write(buffer, index, count);
        }
        #endregion

        /// <summary>
        /// 7Bit로 인코딩된 바이트수를 계산합니다.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>인코딩된 바이트수를 계산한 정수입니다.</returns>
        protected static int Get7BitEncodedSize(int value)
        {
            int size = 0;
            uint v = (uint)value;

            while (v >= 0x80)
            {
                size += 1;
                v >>= 7;
            }

            return size + 1;
        }

        /// <summary>
        /// 문자열 바이너리 바이트수를 계산합니다.
        /// </summary>
        /// <param name="value">대상 문자열입니다.</param>
        /// <returns>문자열 바이너리 바이트수를 계산한 정수입니다.</returns>
        protected unsafe int GetMSStringByteCount(string value)
        {
            int len = Encoding.UTF8.GetByteCount(value);
            int size = Get7BitEncodedSize(len);

            if (len <= LargeByteBufferSize)
            {
                size += len;
            }
            else
            {
                int charStart = 0;
                int numLeft = value.Length;
                int maxChars = LargeByteBufferSize / Encoding.UTF8.GetMaxByteCount(1);

                while (numLeft > 0)
                {
                    int charCount = (numLeft > maxChars) ? maxChars : numLeft;
                    int byteLen;

                    checked
                    {
                        if (charStart < 0 || charCount < 0 || charStart + charCount > value.Length)
                        {
                            throw new ArgumentOutOfRangeException("charCount");
                        }

                        fixed (char* pChars = value)
                        {
                            fixed (byte* pBytes = _largeByteBuffer)
                            {
                                byteLen = _encoder.GetBytes(pChars + charStart, charCount, pBytes, _largeByteBuffer.Length, charCount == numLeft);
                            }
                        }
                    }

                    size += byteLen;
                    charStart += charCount;
                    numLeft -= charCount;
                }
            }

            return size;
        }
    }
}