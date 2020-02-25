using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Resource_Unpacker.Classes.CompressedFiles
{
    public class CompressedFile
    {
        private uint header { get; set; }
        private uint fileLength { get; set; }
        private byte[] zlibHeader { get; set; }

        public Stream decompressedFile { get; set; }

        public async Task LoadCompressedFile(Stream stream)
        {
            using var reader = new BinaryReader(stream);
            header = reader.ReadUInt32();
            // This appears to be a proper non compressed XMB file
            if (header != 0x7433336C) // l33t
            {
                decompressedFile = new MemoryStream();
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(decompressedFile);
                decompressedFile.Seek(0, SeekOrigin.Begin);
            }
            // This is a compressed XMB file
            else
            {
                fileLength = reader.ReadUInt32();
                zlibHeader = reader.ReadBytes(2);
                decompressedFile = new MemoryStream();
                using (var decompStream = new DeflateStream(stream, CompressionMode.Decompress, true))
                {

                    await decompStream.CopyToAsync(decompressedFile);
                }
                decompressedFile.Seek(0, SeekOrigin.Begin);
            }
        }


    }
}
