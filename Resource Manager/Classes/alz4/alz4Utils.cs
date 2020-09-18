using K4os.Compression.LZ4;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Resource_Manager.Classes.Alz4
{
    public static class Alz4Utils
    {
        private const string alz4Header = "alz4";


        #region Check

        public static bool IsAlz4File(string fileName)
        {
            using (var fileStream = File.Open(fileName, FileMode.Open))
            {
                return StreamIsAlz4(fileStream);
            }
        }

        public static bool IsAlz4File(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data, false))
            {
                return StreamIsAlz4(memoryStream);
            }
        }

        private static bool StreamIsAlz4(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                try
                {
                    var fileHeader = new string(reader.ReadChars(4));
                    return fileHeader == alz4Header;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        #endregion
        public static async Task<byte[]> ExtractAlz4BytesAsync(byte[] zipData)
        {
            using (var fileStream = new MemoryStream(zipData, false))
            using (var fileStreamReader = new BinaryReader(fileStream))
                return await ExtractAlz4StreamAsync(fileStreamReader);
        }


        public static async Task<int> ReadCompressedSizeAlz4Async(string inputFileName)
        {



            var data = await File.ReadAllBytesAsync(inputFileName);

            using (var fileStream = new MemoryStream(data, false))
            using (var reader = new BinaryReader(fileStream))
            {
                var fileHeader = new string(reader.ReadChars(4));
                int compressedFileSize;
                int uncompressedFileSize;

                switch (fileHeader.ToLower())
                {
                    case alz4Header:

                        uncompressedFileSize = reader.ReadInt32();
                        compressedFileSize = reader.ReadInt32();
                        return compressedFileSize;
                    default:
                        throw new FileLoadException($"Header '{fileHeader}' is not recognized as a valid type");
                }
            }


        }


        public static async Task<byte[]> ExtractAlz4StreamAsync(BinaryReader reader)
        {
            var fileHeader = new string(reader.ReadChars(4));
            int compressedFileSize;
            int uncompressedFileSize;
            int version;

            switch (fileHeader.ToLower())
            {
                case alz4Header:

                    uncompressedFileSize = reader.ReadInt32();
                    compressedFileSize = reader.ReadInt32();
                    version = reader.ReadInt32();
                    var source = reader.ReadBytes(compressedFileSize);
                    var target = new byte[uncompressedFileSize];
                    await Task.Run(() =>
                    {
                        LZ4Codec.Decode(source, 0, source.Length, target, 0, target.Length);
                    });
                    return target;
                default:
                    throw new FileLoadException($"Header '{fileHeader}' is not recognized as a valid type");
            }

        }

        public static async Task CompressBytesAsAlz4Async(byte[] inputBytes, string outputFileName)
        {
            try
            {

                using (var fileStream = new MemoryStream(inputBytes))
                using (var fileStreamFinal = File.Open(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var outputFileStreamWriter = new BinaryWriter(fileStreamFinal))
                {
                    outputFileStreamWriter.Write(alz4Header.ToCharArray());
                    outputFileStreamWriter.Write(Convert.ToInt32(inputBytes.Length));
                    await Task.Run(() =>
                    {
                        var data = new byte[LZ4Codec.MaximumOutputSize(inputBytes.Length)];
                        var compressedSize = LZ4Codec.Encode(inputBytes, 0, inputBytes.Length, data, 0, data.Length);

                        //var data = LZ4Pickler.Pickle(inputBytes);

                        outputFileStreamWriter.Write(Convert.ToInt32(compressedSize));
                        outputFileStreamWriter.Write(Convert.ToInt32(1));
                        outputFileStreamWriter.Write(data, 0, compressedSize);
                    });
                    

                }
      
            }
            catch (Exception)
            {
                if (File.Exists(outputFileName))
                    File.Delete(outputFileName);

                throw;
            }
        }







    }
}
