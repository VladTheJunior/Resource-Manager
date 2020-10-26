using Resource_Manager.Classes.Alz4;
using Resource_Manager.Classes.L33TZip;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Resource_Manager.Classes.Ddt
{
    public static class DdtFileUtils
    {
        public static async Task Ddt2PngAsync(string ddtFile)
        {
            var outname = ddtFile.ToLower().Replace(".ddt", ".png");

            using (var fileStream = new FileStream(outname, FileMode.Create))
            {
               // DdtFile ddt = new DdtFile(File.ReadAllBytes(ddtFile));
                BitmapEncoder encoder = new PngBitmapEncoder();
                var data = await File.ReadAllBytesAsync(ddtFile);

                if (Alz4Utils.IsAlz4File(data))
                {
                    data = await Alz4Utils.ExtractAlz4BytesAsync(data);
                }
                else
                {
                    if (L33TZipUtils.IsL33TZipFile(data))
                        data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
                }


                //await File.WriteAllBytesAsync("test", data);

                encoder.Frames.Add(BitmapFrame.Create(new DdtFile(data, false).Bitmap));
                encoder.Save(fileStream);
            }
        }

        public static async Task DdtBytes2PngAsync(byte[] ddt, string path)
        {
            var outname = path.ToLower().Replace(".ddt", ".png");

            using (var fileStream = new FileStream(outname, FileMode.Create))
            {
                // DdtFile ddt = new DdtFile(File.ReadAllBytes(ddtFile));
                BitmapEncoder encoder = new PngBitmapEncoder();
                var data = ddt;

                if (Alz4Utils.IsAlz4File(data))
                {
                    data = await Alz4Utils.ExtractAlz4BytesAsync(data);
                }
                else
                {
                    if (L33TZipUtils.IsL33TZipFile(data))
                        data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
                }


                //await File.WriteAllBytesAsync("test", data);

                encoder.Frames.Add(BitmapFrame.Create(new DdtFile(data, false).Bitmap));
                encoder.Save(fileStream);
            }
        }
    }
}
