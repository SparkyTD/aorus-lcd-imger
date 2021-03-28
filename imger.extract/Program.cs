using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace imger.extract
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var binFilePath = args[0];

            var reader = new BinaryReader(File.OpenRead(binFilePath));

            uint frameCount = reader.ReadUInt16();
            var frameInfoList = new FrameInfo[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                frameInfoList[i] = new FrameInfo
                {
                    EndPosition = reader.ReadUInt32() + 1,
                    Width = reader.ReadUInt16(),
                    Height = reader.ReadUInt16(),
                    ImageFormat = reader.ReadUInt16(),
                    StartPosition = i == 0 ? 2 + frameCount * 10 : frameInfoList[i - 1].EndPosition
                };

                // Console.Out.WriteLine($"{frameInfoList[i].StartPosition} -> {frameInfoList[i].EndPosition - frameInfoList[i].StartPosition}");
            }

            // Extract the first frame from the GIF and save it as frame0.png
            var frame = frameInfoList[0];
            reader.BaseStream.Seek(frame.StartPosition, SeekOrigin.Begin);

            var data = reader.ReadBytes((int) (frame.EndPosition - frame.StartPosition));
            var shortData = Enumerable.Range(0, data.Length / 2)
                .Select(i => (ushort) (data[i * 2] | data[i * 2 + 1] << 8));

            var decompressed = RLEDecompress.Decompress(shortData.ToList()).ToArray();
            var unmanagedPointer = Marshal.AllocHGlobal(decompressed.Length);
            Marshal.Copy(decompressed, 0, unmanagedPointer, decompressed.Length);

            var image = new Bitmap(frame.Width, frame.Height, frame.Width * 2, PixelFormat.Format16bppRgb565,
                unmanagedPointer);
            image.Save("frame0.png");
        }
    }
}