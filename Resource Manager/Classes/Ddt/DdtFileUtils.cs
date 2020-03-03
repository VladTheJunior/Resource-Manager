using Resource_Manager.Classes.L33TZip;
using System.IO;
using System.Windows.Media.Imaging;

namespace Resource_Manager.Classes.Ddt
{
    public static class DdtFileUtils
    {
        public static async System.Threading.Tasks.Task Ddt2PngAsync(string ddtFile)
        {
            var outname = ddtFile.ToLower().Replace(".ddt", ".png");

            using (var fileStream = new FileStream(outname, FileMode.Create))
            {
               // DdtFile ddt = new DdtFile(File.ReadAllBytes(ddtFile));
                BitmapEncoder encoder = new PngBitmapEncoder();
                var data = await File.ReadAllBytesAsync(ddtFile);
            
                if (L33TZipUtils.IsL33TZipFile(data))
                    data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);

                await File.WriteAllBytesAsync("test", data);

                encoder.Frames.Add(BitmapFrame.Create(new DdtFile(data, false).Bitmap));
                encoder.Save(fileStream);
            }
        }
    }
}
