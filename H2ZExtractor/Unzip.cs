using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace H2ZExtractor
{
    class Unzip
    {
        public static byte[] GetData(byte[] data, bool decompress)
        {
            if (decompress)
            {
                using (MemoryStream input = new MemoryStream(data))
                using (MemoryStream output = new MemoryStream())
                using (DeflateStream deflateStream = new DeflateStream(input, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(output);
                    return output.ToArray();
                }
            }
            else
            {
                using (MemoryStream input = new MemoryStream(data))
                using (MemoryStream output = new MemoryStream())
                {
                    using (DeflateStream deflateStream = new DeflateStream(output, CompressionMode.Compress, true))
                    {
                        input.CopyTo(deflateStream);
                    }
                    return output.ToArray();
                }
            }
        }
    }
}