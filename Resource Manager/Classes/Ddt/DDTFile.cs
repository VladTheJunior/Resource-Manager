using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Resource_Manager.Classes.Ddt
{
    public enum DdtFileTypeAlpha : byte
    {
        None = 0,
        Player = 1,
        Trans = 4,
        Blend = 8
    }

    public enum DdtFileTypeFormat : byte
    {
        Bgra = 1,
        Dxt1 = 4,
        Dxt1DE = 5,
        Grey = 7,
        Dxt3 = 8,
        Dxt5 = 9
    }

    public enum DdtFileTypeUsage : byte
    {
        Unk0 = 0,
        Unk1 = 1,
        Bump = 6,
        Unk2 = 7,
        Cube = 8
    }

    public class DdtFile
    {
        public DdtFile(byte[] data, bool AlphaPart)
        {
            using (var stream = new MemoryStream(data))
            {
                using (var binaryReader = new BinaryReader(stream))
                {
                    Head = new string(binaryReader.ReadChars(4));
                    Usage = (DdtFileTypeUsage)binaryReader.ReadByte();
                    Alpha = (DdtFileTypeAlpha)binaryReader.ReadByte();
                    Format = (DdtFileTypeFormat)binaryReader.ReadByte();
                    MipmapLevels = binaryReader.ReadByte();
                    BaseWidth = binaryReader.ReadInt32();
                    BaseHeight = binaryReader.ReadInt32();
                    var images = new List<DdtImage>();
                    var numImagesPerLevel = Usage == DdtFileTypeUsage.Cube ? 6 : 1;
                    for (var index = 0; index < MipmapLevels * numImagesPerLevel; ++index)
                    {
                        binaryReader.BaseStream.Position = 16 + 8 * index;
                        var width = BaseWidth >> (index / numImagesPerLevel);
                        if (width < 1)
                            width = 1;
                        var height = BaseHeight >> (index / numImagesPerLevel);
                        if (height < 1)
                            height = 1;
                        var offset = binaryReader.ReadInt32();
                        var length = binaryReader.ReadInt32();
                        binaryReader.BaseStream.Position = offset;
                        images.Add(new DdtImage(width, height, offset, length, binaryReader.ReadBytes(length)));
                    }
                    Images = new ReadOnlyCollection<DdtImage>(images);
                }
            }
            Bitmap = GetBitmap(AlphaPart);
        }

        public string Head { get; }
        public DdtFileTypeUsage Usage { get; }
        public DdtFileTypeAlpha Alpha { get; }
        public DdtFileTypeFormat Format { get; }
        public byte MipmapLevels { get; }
        public int BaseWidth { get; }
        public int BaseHeight { get; }
        public IReadOnlyCollection<DdtImage> Images { get; }

        public string DdtInfo
        {
            get
            {
                return BaseWidth.ToString() + "x" + BaseHeight.ToString() + ", " + Format.ToString().ToUpper() + " (" + ((byte)Usage).ToString() + " " + ((byte)Alpha).ToString() + " " + ((byte)Format).ToString() + ")";
            }
        }

        public BitmapSource Bitmap { get; }

        public byte[] ToByteArray()
        {
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(Head);
                    bw.Write((byte)Usage);
                    bw.Write((byte)Alpha);
                    bw.Write((byte)Format);
                    bw.Write(MipmapLevels);
                    bw.Write(BaseWidth);
                    bw.Write(BaseHeight);
                    foreach (var image in Images)
                    {
                        bw.Write(image.Length);
                        bw.Write(image.Offset);
                    }
                    foreach (var image in Images)
                        bw.Write(image.RawData);
                    return ms.ToArray();
                }
            }
        }

        private BitmapSource GetBitmap(bool AlphaPart)
        {
            var ddtImage = Images.FirstOrDefault();
            if (ddtImage == null)
                return null;

            byte[] data;
            switch (Format)
            {
                case DdtFileTypeFormat.Bgra:
                    {
                        data = ddtImage.RawData;
                        break;
                    }
                case DdtFileTypeFormat.Grey:
                    {
                        var rawData = ddtImage.RawData;
                        data = new byte[ddtImage.Width * ddtImage.Height * 4];
                        for (var i = 0; i < ddtImage.Width * ddtImage.Height; i++)
                        {
                            data[i * 4] =
                                data[i * 4 + 1] =
                                    data[i * 4 + 2] = rawData[i];
                            data[i * 4 + 3] = 255;
                        }
                        break;
                    }
                case DdtFileTypeFormat.Dxt1DE:
                case DdtFileTypeFormat.Dxt1:
                    {
                        data = DxtFileUtils.DecompressDxt1(ddtImage.RawData, ddtImage.Width, ddtImage.Height);

                        break;
                    }
                case DdtFileTypeFormat.Dxt3:
                    {
                        data = DxtFileUtils.DecompressDxt3(ddtImage.RawData, ddtImage.Width, ddtImage.Height);

                        break;
                    }
                case DdtFileTypeFormat.Dxt5:
                    {

                        if (Usage == DdtFileTypeUsage.Bump)
                            data = DxtFileUtils.DecompressDxt5Bump(ddtImage.RawData, ddtImage.Width, ddtImage.Height);
                        else
                            data = DxtFileUtils.DecompressDxt5(ddtImage.RawData, ddtImage.Width, ddtImage.Height);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(Format), Format, null);
            }

            if (AlphaPart)

            {
                byte[] alphaData = new byte[ddtImage.Width * ddtImage.Height * 4];
                if (Alpha == DdtFileTypeAlpha.None)
                {
                    alphaData = Enumerable.Repeat((byte)255, alphaData.Length).ToArray();
                }

                else
                {
                    for (var i = 0; i < ddtImage.Width * ddtImage.Height; i++)
                    {
                        alphaData[i * 4] =
                            alphaData[i * 4 + 1] =
                                alphaData[i * 4 + 2] = 0;
                        alphaData[i * 4 + 3] = (byte)(255 - data[i * 4 + 3]);
                    }
                }
                byte[] dataWithoutAlpha = data.ToArray();
                for (var i = 0; i < ddtImage.Width * ddtImage.Height; i++)
                {
                    dataWithoutAlpha[i * 4 + 3] = 255;
                }

                var image = dataWithoutAlpha.Concat(alphaData);

                return BitmapSource.Create(ddtImage.Width, ddtImage.Height * 2, 96, 96, PixelFormats.Pbgra32, null, image.ToArray(), 4 * ddtImage.Width);
            }
            else
                return BitmapSource.Create(ddtImage.Width, ddtImage.Height, 96, 96, PixelFormats.Pbgra32, null, data, 4 * ddtImage.Width);

        }
    }


    public class DdtImage
    {
        public DdtImage(int width, int height, int offset, int length, byte[] rawData)
        {
            if (rawData == null)
                throw new ArgumentNullException(nameof(rawData));
            if (rawData.Length != length)
                throw new ArgumentOutOfRangeException(nameof(length), length, @"rawData.Length != length");
            Width = width;
            Height = height;
            Offset = offset;
            RawData = rawData;
        }

        public int Width { get; }
        public int Height { get; }
        public int Offset { get; }
        public int Length => RawData?.Length ?? 0;
        public byte[] RawData { get; }
    }
}
