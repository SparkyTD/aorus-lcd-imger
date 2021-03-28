using System;
using System.IO;

namespace imger
{
    public class RLECompress
    {
        public ushort imageFormat { get; }

        public RLECompress() => imageFormat = 3;

        private static RLESameDiffBlock FindMaxSameData(ushort[] src, int sIndex)
        {
            if (src.Length - sIndex < 4)
                return new RLESameDiffBlock(src, src.Length, 0);
            for (var index1 = sIndex; index1 < src.Length; ++index1)
            {
                if (index1 + 2 == src.Length)
                {
                    var src1 = new ushort[src.Length - sIndex];
                    Array.Copy(src, sIndex, src1, 0, src1.Length);
                    return new RLESameDiffBlock(src1, src1.Length, 0);
                }

                if (src[index1] == src[index1 + 1] && src[index1 + 1] == src[index1 + 2])
                {
                    var index2 = index1 + 2;
                    while (index2 < src.Length - 1 && src[index2] == src[index2 + 1])
                        ++index2;
                    var src1 = new ushort[index2 + 1 - sIndex];
                    Array.Copy(src, sIndex, src1, 0, src1.Length);
                    return new RLESameDiffBlock(src1, index1 - sIndex, index2 + 1 - index1);
                }
            }

            return null;
        }

        private static RLESameDiffBlock FindSameData(ushort[] src, int sIndex)
        {
            const int maxValue = short.MaxValue;
            if (src.Length - sIndex > maxValue)
            {
                var src1 = new ushort[maxValue];
                Array.Copy(src, sIndex, src1, 0, src1.Length);
                return FindMaxSameData(src1, 0);
            }

            var src2 = new ushort[src.Length - sIndex];
            Array.Copy(src, sIndex, src2, 0, src2.Length);
            return FindMaxSameData(src2, 0);
        }

        private static Stream Compress(ushort[] src)
        {
            var memoryStream = new MemoryStream();
            if (src.Length < 3)
            {
                var buffer = new []
                {
                    (byte) src[0],
                    (byte) ((uint) src[0] >> 8),
                    (byte) src[1],
                    (byte) ((uint) src[1] >> 8)
                };
                memoryStream.Write(buffer, 0, buffer.Length);
                return memoryStream;
            }

            int num1;
            for (var sIndex = 0; sIndex < src.Length; sIndex = num1 + 1)
            {
                var sameData = FindSameData(src, sIndex);
                num1 = sIndex + ((int) ((sameData.diffBlock.Length + sameData.sameBlock.Length) / 2L) - 1);
                if (sameData.diffBlock.Length > 0L)
                {
                    var num2 = (ushort) ((ulong) sameData.diffBlock.Length / 2UL);
                    var buffer = new byte[sameData.diffBlock.Length + 2L];
                    buffer[0] = (byte) num2;
                    buffer[1] = (byte) ((uint) num2 >> 8);
                    sameData.diffBlock.Read(buffer, 2, (int) sameData.diffBlock.Length);
                    memoryStream.Write(buffer, 0, buffer.Length);
                }

                if (sameData.sameBlock.Length > 0L)
                {
                    var num2 = (ushort) ((ulong) sameData.sameBlock.Length / 2UL);
                    var buffer = new byte[4];
                    var num3 = (ushort) (num2 | 32768U);
                    buffer[0] = (byte) num3;
                    buffer[1] = (byte) ((uint) num3 >> 8);
                    sameData.sameBlock.Read(buffer, 2, 2);
                    memoryStream.Write(buffer, 0, buffer.Length);
                }
            }

            memoryStream.Seek(0L, SeekOrigin.Begin);
            return memoryStream;
        }

        public static Stream Compress(byte[] src)
        {
            var src1 = new ushort[src.Length / 2];
            for (var index = 0; index < src1.Length; ++index)
                src1[index] = (ushort) (src[index * 2] | (uint) src[index * 2 + 1] << 8);
            return Compress(src1);
        }
    }
}