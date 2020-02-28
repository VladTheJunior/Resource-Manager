using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace Resource_Unpacker.Classes.Ddt
{
    public static class DdtFileUtils
    {
        public static void Ddt2Png(string ddtFile)
        {
            var outname = ddtFile.ToLower().Replace(".ddt", ".png");
            if (File.Exists(outname))
                File.Delete(outname);
            using (var fileStream = new FileStream(outname, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(new DdtFile(File.ReadAllBytes(ddtFile)).Bitmap));
                encoder.Save(fileStream);
            }
        }
    }
}
