using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace imger
{
    internal static class Program
    {
        private static PixelFormat _sDpp = PixelFormat.Format24bppRgb;
        private static string _sSourcePath = "";
        private static int _depth;

        private static Bitmap LightenMargin(Bitmap bitmap, int depth)
        {
            if (bitmap == null)
                return null;
            if (depth <= 0)
                return bitmap;
            var bitmap1 = new Bitmap(bitmap.Width, bitmap.Height, _sDpp);
            for (var x = 0; x < bitmap.Width; ++x)
            {
                for (var y = 0; y < bitmap.Height; ++y)
                {
                    var num1 = x < bitmap.Width - x ? x : bitmap.Width - x;
                    var num2 = y < bitmap.Height - y ? y : bitmap.Height - y;
                    if (num1 <= depth && num2 <= depth)
                    {
                        var num3 = (int) Math.Sqrt((depth - num1) * (depth - num1) +
                                                   (depth - num2) * (depth - num2));
                        if (num3 <= depth)
                        {
                            var coefficient = 1f * num3 / depth;
                            bitmap1.SetPixel(x, y, AdjustBrightness(bitmap.GetPixel(x, y), coefficient));
                        }
                        else
                            bitmap1.SetPixel(x, y, Color.FromArgb(bitmap.GetPixel(x, y).A, 0, 0, 0));
                    }
                    else if (num1 <= depth || num2 <= depth)
                    {
                        var coefficient =
                            (float) (1.0 - 1.0 * (num1 < num2 ? num1 : (double) num2) / depth);
                        bitmap1.SetPixel(x, y, AdjustBrightness(bitmap.GetPixel(x, y), coefficient));
                    }
                    else
                        bitmap1.SetPixel(x, y, bitmap.GetPixel(x, y));
                }
            }

            return bitmap1;
        }

        private static Color AdjustBrightness(Color color, float coefficient)
        {
            if (coefficient < -1.0)
                coefficient = -1f;
            if (coefficient > 1.0)
                coefficient = 1f;
            var r = (float) color.R;
            var g = (float) color.G;
            var b = (float) color.B;
            float num1;
            float num2;
            float num3;
            if (coefficient < 0.0)
            {
                coefficient = 1f + coefficient;
                num1 = r * coefficient;
                num2 = g * coefficient;
                num3 = b * coefficient;
            }
            else
            {
                num1 = (0.0f - r) * coefficient + r;
                num2 = (0.0f - g) * coefficient + g;
                num3 = (0.0f - b) * coefficient + b;
            }

            var num4 = num1 > 0.0 ? num1 : 0.0f;
            var num5 = num4 < (double) byte.MaxValue ? num4 : byte.MaxValue;
            var num6 = num2 > 0.0 ? num2 : 0.0f;
            var num7 = num6 < (double) byte.MaxValue ? num6 : byte.MaxValue;
            var num8 = num3 > 0.0 ? num3 : 0.0f;
            var num9 = num8 < (double) byte.MaxValue ? num8 : byte.MaxValue;
            return Color.FromArgb(color.A, (int) num5, (int) num7, (int) num9);
        }

        private static void Main(string[] args)
        {
            // File.WriteAllText($"C:\\NvAPISpy\\imger_{DateTime.Now.Ticks}.log", string.Join("\n", args));
            if (args.Length > 1 && args[1] == "--frame")
            {
                Console.Write(GetFrameCount(Image.FromFile(args[0])));
            }
            else
            {
                if (args.Length < 8)
                    return;
                var targetWidth = Convert.ToInt32(args[1]);
                var targetHeight = Convert.ToInt32(args[2]);
                var sourceX = Convert.ToInt32(args[3]);
                var sourceY = Convert.ToInt32(args[4]);
                var sourceWidth = Convert.ToInt32(args[5]);
                var sourceHeight = Convert.ToInt32(args[6]);
                var savePath = args[7];
                _depth = args.Length > 8 ? Convert.ToInt32(args[8]) : 0;
                var bitsPerPixel = args.Length > 9 ? Convert.ToInt32(args[9]) : 24;
                var compressionName = args.Length > 10 ? args[10] : "";
                switch (bitsPerPixel)
                {
                    case 16:
                        _sDpp = PixelFormat.Format16bppRgb565;
                        break;
                    case 24:
                        _sDpp = PixelFormat.Format24bppRgb;
                        break;
                    case 32:
                        _sDpp = PixelFormat.Format32bppRgb;
                        break;
                }

                _sSourcePath = args[0];
                var image1 = Image.FromFile(args[0]);
                if (!ImageFormat.Gif.Equals(image1.RawFormat))
                {
                    var pathByExt1 = GetPathByExt(savePath, "bmp");
                    var pathByExt2 = GetPathByExt(pathByExt1, "bak.bmp");
                    var image2 = (Image) new Bitmap(targetWidth, targetHeight, _sDpp);
                    var graphics = Graphics.FromImage(image2);
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(image1, new Rectangle(0, 0, targetWidth, targetHeight),
                        new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight), GraphicsUnit.Pixel);
                    graphics.Dispose();
                    image1.Dispose();
                    image2.Save(pathByExt2, ImageFormat.Bmp);
                    if (_depth > 0)
                        image2 = LightenMargin((Bitmap) image2, _depth);
                    image2.Save(pathByExt1, ImageFormat.Bmp);
                    SaveZipData(pathByExt1, compressionName, new List<Bitmap>()
                    {
                        (Bitmap) image2
                    });
                    SaveInfo(pathByExt1, compressionName);
                }
                else
                    SaveGifInfo(
                        ChangeGif(image1, targetWidth, targetHeight, sourceX, sourceY, sourceWidth, sourceHeight,
                            savePath,
                            _depth, compressionName),
                        savePath, compressionName);
            }
        }

        private static int GetFrameCount(Image img)
        {
            var frameDimensionsList = img.FrameDimensionsList;
            var index = 0;
            if (index >= frameDimensionsList.Length)
                return 0;
            var dimension = new FrameDimension(frameDimensionsList[index]);
            return img.GetFrameCount(dimension);
        }

        private static void SaveZipData(string sFile, string compress, IList<Bitmap> bmps)
        {
            if (string.IsNullOrEmpty(compress))
                return;
            var streamList = new List<Stream>();
            var memoryStream = new MemoryStream();
            memoryStream.Write(BitConverter.GetBytes(bmps.Count), 0, 2);
            var compress1 = new RLECompress();
            uint num1 = 0;
            for (var index1 = 0; index1 < bmps.Count; ++index1)
            {
                var bmp = bmps[index1];
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var bitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format16bppRgb565);
                var width = bmp.Width;
                var height = bmp.Height;
                var numArray1 = new byte[bitmapData.Stride * bmp.Height];
                Marshal.Copy(bitmapData.Scan0, numArray1, 0, numArray1.Length);
                if (bmp.Width * 2 != bitmapData.Stride)
                {
                    var numArray2 = new byte[bmp.Width * 2 * bmp.Height];
                    for (var index2 = 0; index2 < bmp.Height; ++index2)
                    {
                        for (var index3 = 0; index3 < bmp.Width * 2; ++index3)
                            numArray2[index2 * bmp.Width * 2 + index3] = numArray1[index2 * bitmapData.Stride + index3];
                    }

                    numArray1 = numArray2;
                }

                var stream = RLECompress.Compress(numArray1);
                streamList.Add(stream);
                if (index1 == 0)
                    num1 = (uint) ((ulong) (2 + bmps.Count * 10) + (ulong) stream.Length);
                else
                    num1 += (uint) stream.Length;
                Console.Out.WriteLine($"num1 = {num1}; length = {stream.Length}");
                var num2 = num1 - 1U;
                var buffer = new byte[10];
                var bytes1 = BitConverter.GetBytes(num2);
                Array.Copy(bytes1, buffer, bytes1.Length);
                var bytes2 = BitConverter.GetBytes((ushort) width);
                Array.Copy(bytes2, 0, buffer, 4, bytes2.Length);
                var bytes3 = BitConverter.GetBytes((ushort) height);
                Array.Copy(bytes3, 0, buffer, 6, bytes3.Length);
                var bytes4 = BitConverter.GetBytes(compress1.imageFormat);
                Array.Copy(bytes4, 0, buffer, 8, bytes4.Length);
                memoryStream.Write(buffer, 0, buffer.Length);
                bmp.Dispose();
            }

            using (var output = (Stream) File.Create(GetPathByExt(sFile, "bin")))
            {
                var array = memoryStream.ToArray();
                output.Write(array, 0, array.Length);
                for (var index = 0; index < bmps.Count; ++index)
                {
                    Console.Out.WriteLine($"CopyStream start={output.Position}; length={streamList[index].Length}");
                    CopyStream(streamList[index], output);
                }
            }
        }

        private static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[8192];
            int count;
            while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                output.Write(buffer, 0, count);
        }

        private static void SaveInfo(string sSavePath, string sCompress)
        {
            sSavePath = GetPathByExt(sSavePath, "ini");
            using (var streamWriter = new StreamWriter(sSavePath))
            {
                streamWriter.WriteLine("[Info]");
                streamWriter.WriteLine("Count=1");
                streamWriter.WriteLine("Compress=" + sCompress);
                streamWriter.WriteLine("Source=" + _sSourcePath);
                streamWriter.WriteLine("Depth=" + _depth);
            }
        }

        private static void SaveGifInfo(GifInfo info, string sSavePath, string sCompress)
        {
            sSavePath = Path.GetDirectoryName(sSavePath) + "\\" + Path.GetFileNameWithoutExtension(sSavePath) + ".ini";
            using (var streamWriter = new StreamWriter(sSavePath))
            {
                streamWriter.WriteLine("[Info]");
                streamWriter.WriteLine("Depth=" + _sSourcePath);
                streamWriter.WriteLine("Source=" + _sSourcePath);
                streamWriter.WriteLine("Count=" + (info.nCount + 1));
                streamWriter.WriteLine("Compress=" + sCompress);
                streamWriter.WriteLine("Depth=" + _depth);
                for (var index = 0; index < info.lstDelay.Count; ++index)
                    streamWriter.WriteLine("Delay" + index + "=" + info.lstDelay[index]);
            }
        }

        private static int GetGifDelay(Image img, int nIndex)
        {
            for (var index = 0; index < img.PropertyIdList.Length; ++index)
            {
                if ((int) img.PropertyIdList.GetValue(index) != 20736) continue;
                
                var propertyItem = (PropertyItem) img.PropertyItems.GetValue(index);
                return BitConverter.ToInt32(new[]
                {
                    propertyItem.Value[nIndex * 4],
                    propertyItem.Value[1 + nIndex * 4],
                    propertyItem.Value[2 + nIndex * 4],
                    propertyItem.Value[3 + nIndex * 4]
                }, 0) * 10;
            }

            return 0;
        }

        private static GifInfo ChangeGif(Image img, int targetWidth, int targetHeight, int sourceX, int sourceY,
            int sourceWidth, int sourceHeight,
            string savePath, int nDepth, string sCompress)
        {
            var newImage = (Image) new Bitmap(targetWidth, targetHeight);
            var graphics1 = Graphics.FromImage(newImage);
            var gifInfo = new GifInfo();
            var bmps = new List<Bitmap>();
            foreach (var frameDimensions in img.FrameDimensionsList)
            {
                var dimension = new FrameDimension(frameDimensions);
                var time = FrameDimension.Time;
                var frameCount = img.GetFrameCount(dimension);
                var encoder = GetEncoder(ImageFormat.Gif);
                var saveFlag = Encoder.SaveFlag;
                gifInfo.nCount = frameCount;
                gifInfo.lstDelay = new List<int>();
                for (var index = 0; index < frameCount; ++index)
                {
                    img.SelectActiveFrame(time, index);
                    gifInfo.lstDelay.Add(GetGifDelay(img, index));
                    var image2 = (Image) new Bitmap(targetWidth, targetHeight, _sDpp);
                    var graphics2 = Graphics.FromImage(image2);
                    graphics2.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics2.DrawImage(img, new Rectangle(0, 0, targetWidth, targetHeight),
                        new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                        GraphicsUnit.Pixel);
                    if (nDepth > 0)
                        image2 = LightenMargin((Bitmap) image2, nDepth);
                    image2.Save(GetPathByExt(savePath, "bmp"), ImageFormat.Bmp);
                    if (index == 0)
                        bmps.Add((Bitmap) image2.Clone());
                    bmps.Add((Bitmap) image2);
                    long num = 21;
                    if (index == 0)
                    {
                        image2.Save(GetPathByExt(savePath, "bak.bmp"), ImageFormat.Bmp);
                        image2.Save(GetPathByExt(savePath, "bmp"), ImageFormat.Bmp);
                        graphics1.DrawImage(img, new Rectangle(0, 0, targetWidth, targetHeight),
                            new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                            GraphicsUnit.Pixel);
                        num = 18L;
                        bindProperty(img, newImage);
                    }

                    var encoderParams = new EncoderParameters(2);
                    encoderParams.Param[0] = new EncoderParameter(saveFlag, num);
                    encoderParams.Param[1] = new EncoderParameter(Encoder.ScanMethod, 8);
                    if (index == 0)
                        newImage.Save(GetPathByExt(savePath, "bak.gif"), encoder, encoderParams);
                    else
                        newImage.SaveAdd(image2, encoderParams);
                    graphics2.Dispose();
                }

                var encoderParams1 = new EncoderParameters(2);
                encoderParams1.Param[0] = new EncoderParameter(saveFlag, 20L);
                encoderParams1.Param[1] = new EncoderParameter(Encoder.ScanMethod, 8);
                newImage.SaveAdd(encoderParams1);
            }

            img.Dispose();
            File.Copy(GetPathByExt(savePath, "bak.gif"), GetPathByExt(savePath, "gif"), true);
            SaveZipData(savePath, sCompress, bmps);
            return gifInfo;
        }

        private static string GetPathByExt(string sPath, string sExt) => Path.GetDirectoryName(sPath) + "\\" +
                                                                         Path.GetFileNameWithoutExtension(sPath) + "." +
                                                                         sExt;

        private static void bindProperty(Image a, Image b)
        {
            foreach (var t in a.PropertyItems)
                b.SetPropertyItem(t);
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders()
                .FirstOrDefault(imageDecoder => imageDecoder.FormatID == format.Guid);
        }

        private struct GifInfo
        {
            public int nCount;
            public List<int> lstDelay;
        }
    }
}