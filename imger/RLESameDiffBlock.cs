using System.IO;

namespace imger
{
    public class RLESameDiffBlock
    {
        public Stream sameBlock { get; set; }

        public Stream diffBlock { get; set; }

        public RLESameDiffBlock(ushort[] src, int sameIndex, int sameLenght)
        {
            this.sameBlock = (Stream) new MemoryStream();
            this.diffBlock = (Stream) new MemoryStream();
            if (src != null)
            {
                byte[] buffer = new byte[src.Length * 2];
                for (int index = 0; index < src.Length; ++index)
                {
                    buffer[index * 2] = (byte) src[index];
                    buffer[index * 2 + 1] = (byte) ((uint) src[index] >> 8);
                }

                if ((uint) sameIndex > 0U)
                    this.diffBlock.Write(buffer, 0, sameIndex * 2);
                if ((uint) sameLenght > 0U)
                    this.sameBlock.Write(buffer, sameIndex * 2, sameLenght * 2);
            }

            this.sameBlock.Seek(0L, SeekOrigin.Begin);
            this.diffBlock.Seek(0L, SeekOrigin.Begin);
        }
    }
}