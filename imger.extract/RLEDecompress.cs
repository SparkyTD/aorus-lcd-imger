using System;
using System.Collections.Generic;
using System.IO;

namespace imger.extract
{
    public class RLEDecompress
    {
        public static MemoryStream Decompress(IList<ushort> compressData)
        {
            var memoryStream = new MemoryStream();
            int index1 = 0;
            while (index1 < compressData.Count)
            {
                ushort num = (ushort) (compressData[index1] & (uint) short.MaxValue);
                byte[] buffer = new byte[num * 2];
                if ((compressData[index1] & 32768) == 32768)
                {
                    for (int index2 = 0; index2 < num; ++index2)
                    {
                        buffer[index2 * 2] = (byte) compressData[index1 + 1];
                        buffer[index2 * 2 + 1] = (byte) ((uint) compressData[index1 + 1] >> 8);
                    }

                    index1 += 2;
                }
                else
                {
                    for (int index2 = 0; index2 < num; ++index2)
                    {
                        buffer[index2 * 2] = (byte) compressData[index1 + 1 + index2];
                        buffer[index2 * 2 + 1] = (byte) ((uint) compressData[index1 + 1 + index2] >> 8);
                    }

                    index1 += 1 + num;
                }

                memoryStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Seek(0L, SeekOrigin.Begin);
            return memoryStream;
        }

        public static MemoryStream Decompress(string filename)
        {
            var fileStream = File.Open(filename, FileMode.Open);
            byte[] fileBuffer = new byte[fileStream.Length];
            fileStream.Read(fileBuffer, 0, fileBuffer.Length);
            fileStream.Close();
            
            ushort frameCount = BitConverter.ToUInt16(fileBuffer, 0);
            uint[] frameOffsets = new uint[frameCount];
            byte[] headersBuffer = new byte[2 + frameCount * 10];
            headersBuffer[0] = fileBuffer[0];
            headersBuffer[1] = fileBuffer[1];
            for (int index = 0; index < frameCount; ++index)
                frameOffsets[index] = BitConverter.ToUInt32(fileBuffer, 2 + index * 10);
            
            var memoryStream = new MemoryStream();
            memoryStream.Write(fileBuffer, 0, headersBuffer.Length);
            ushort[] compressData = new ushort[(fileBuffer.Length - 2 - frameCount * 10) / 2];
            for (int index = 0; index < compressData.Length; ++index)
                compressData[index] = (ushort) (fileBuffer[2 + frameCount * 10 + index * 2] |
                                                (uint) fileBuffer[2 + frameCount * 10 + index * 2 + 1] << 8);
            var stream = Decompress(compressData);
            byte[] buffer3 = new byte[stream.Length];
            stream.Read(buffer3, 0, (int) stream.Length);
            memoryStream.Write(buffer3, 0, buffer3.Length);
            memoryStream.Seek(0L, SeekOrigin.Begin);
            memoryStream.Write(headersBuffer, 0, headersBuffer.Length);
            memoryStream.Seek(0L, SeekOrigin.Begin);
            return memoryStream;
        }
    }
}