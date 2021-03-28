using System.Runtime.InteropServices;

namespace imger.extract
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct FrameInfo
    {
        public uint EndPosition;
        public uint StartPosition;
        public ushort Width;
        public ushort Height;
        public ushort ImageFormat;
    }
}