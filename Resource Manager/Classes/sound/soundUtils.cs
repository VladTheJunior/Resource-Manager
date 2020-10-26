using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Resource_Manager.Classes.sound
{
    public static class soundUtils
    {
        public static ulong RotateLeft(this ulong value, int count)
        {
            return (value << count) | (value >> (64 - count));
        }

        public async static Task<byte[]> DecryptSound(byte[] data)
        {

            ulong qword8 = 2564355413007450943;
            ulong qword10 = 4159887087525648499;
            ulong qword18 = 3096547199993908069;
            long currentBlock = data.Length / 8;
            using (var streamReader = new MemoryStream(data, false))
            using (var reader = new BinaryReader(streamReader))
            using (var streamWriter = new MemoryStream())
            using (var writer = new BinaryWriter(streamWriter))
            {
                await Task.Run(() =>
                {
                    while (currentBlock > 0)
                    {
                        var block = reader.ReadUInt64();

                        qword18 = RotateLeft(qword10 * (qword18 + qword8), 32);
                        writer.Write(block ^ qword18);
                        currentBlock--;
                    }
                });
                return streamWriter.ToArray();
            }

        }
    }
}
