using System;
using System.IO;

namespace CFS.Extension
{
    internal static class StreamEx
    {
        public static void CopyTo(this Stream source, long sourceOffset, Stream destination, long offset, long length, int bufferSize = 8192)
        {
            if (sourceOffset + length > source.Length)
                throw new ArgumentOutOfRangeException();

            bufferSize = (int)Math.Min(bufferSize, length);

            source.Seek(sourceOffset, SeekOrigin.Begin);
            destination.Seek(offset, SeekOrigin.Begin);

            byte[] buffer = new byte[bufferSize];
            int read;

            while ((read = source.Read(buffer, 0, bufferSize)) > 0)
            {
                destination.Write(buffer, 0, read);

                length -= bufferSize;
                
                if (bufferSize > length)
                    bufferSize = (int)length;
            }
        }
    }
}