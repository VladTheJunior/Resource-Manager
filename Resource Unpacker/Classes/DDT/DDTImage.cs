using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resource_Unpacker.Classes.DDT
{
    public class DDTImage
    {

        private const int DDT_DIFFUSE = 0;
        private const int DDT_DIFFUSE2 = 1;
        private const int DDT_BUMP = 6;
        private const int DDT_BUMP2 = 7;
        private const int DDT_CUBE = 8;// to-do, not yet supported

        private const int DDT_NONE = 0;
        private const int DDT_PLAYER = 1;
        private const int DDT_TRANSPARENT = 4;
        private const int DDT_BLEND = 8;

        private const int DDT_BGRA = 1;
        private const int DDT_DXT1 = 4;
        private const int DDT_GREYSCALE = 7;
        private const int DDT_DXT3 = 8;
        private const int DDT_DXT5 = 9;
        public uint Magic { get; set; }
        public byte TextureUsage { get; set; }
        public byte TextureAlphaUsage { get; set; }
        public byte TextureType { get; set; }
        public byte ImageCount { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        uint Offset { get; set; }
        uint Size { get; set; }


        public Pixel[] Palette;
        //     public byte[] PixelData;

        public struct Pixel
        {
            public byte R, G, B, A;
            public override string ToString()
            {
                return (((((((A << 8) + R) << 8) + G) << 8) + B)).ToString("x");
            }
            public Pixel Lerp(Pixel p2, int lerp, int denom)
            {
                Pixel p1 = this;
                return new Pixel()
                {
                    R = (byte)(p1.R + ((int)p2.R - p1.R) * lerp / denom),
                    G = (byte)(p1.G + ((int)p2.G - p1.G) * lerp / denom),
                    B = (byte)(p1.B + ((int)p2.B - p1.B) * lerp / denom),
                    A = (byte)(p1.A + ((int)p2.A - p1.A) * lerp / denom),
                };
            }
        }


        /*
        * 
        * IMAGE HEADER
        uint32 {4}       - Header (RTS3)
        byte {3}         - Unknown (DXT1=0,0,4 DXT3/5=1,4,8 or 0,4,8)
        byte {1}         - Number Of MipMaps
        uint32 {4}       - Image Width
        uint32 {4}       - Image Height
        for each mipmap
        uint32 {4}       - Image Data Offset
        uint32 {4}       - Image Data Length

        IMAGE DATA
        for each mipmap
        byte {X}         - DDS Image Data
        * 
        */


        public byte[] pixels;
        public byte[] bytes;
        public void LoadDDT(Stream stream)
        {

            using var reader = new BinaryReader(stream);
            Magic = reader.ReadUInt32();

            if (Magic != 0x33535452) //RTS3
            {
                throw new Exception("'RTS3' not detected - Not a valid DDT file!");
            }

            TextureUsage = reader.ReadByte();
            TextureAlphaUsage = reader.ReadByte();
            TextureType = reader.ReadByte();
            ImageCount = reader.ReadByte();
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();


            List<Tuple<int, int>> images = new List<Tuple<int, int>>();
            for (int l = 0; l < ImageCount; ++l)
            {
                int off = reader.ReadInt32();
                int len = reader.ReadInt32();
                images.Add(new Tuple<int, int>(off, len));
            }


            stream.Seek(images[0].Item1, SeekOrigin.Begin);
            bytes = new byte[images[0].Item2];
            reader.Read(bytes, 0, bytes.Length);




            pixels = new byte[Width * Height * 4];

            switch (TextureType)
            {
                case DDT_BGRA:
                    if (bytes.Length != pixels.Length)
                        throw new Exception("DDT_BGRA data is not correct size");
                    Buffer.BlockCopy(bytes, 0, pixels, 0, pixels.Length);

                    break;

                case DDT_GREYSCALE:
                    for (int b = 0; b < Width * Height; ++b)
                    {
                        pixels[b * 4 + 0] =
                        pixels[b * 4 + 1] =
                        pixels[b * 4 + 2] = bytes[b];
                        pixels[b * 4 + 3] = 255;
                    }
                    break;

                case DDT_DXT3:
                case DDT_DXT1:
                case DDT_DXT5:




                    Pixel[] blockPalette = new Pixel[2];
                    Pixel[] block4Palette = new Pixel[4];
                    int stride = 8;
                    int colOff = 4;
                    int colStride = 1;
                    int alpOff = 0;
                    int alpBits = 0;
                    int alpStrideBits = 0;
                    Action<byte[], int, Pixel[]> extractor = null;
                    switch (TextureType)
                    {
                        case DDT_DXT1: extractor = Convert565ToRGB; stride = 8; colOff = 4; colStride = 1; break;
                        case DDT_DXT3: extractor = Convert555ToRGB; stride = 10; colOff = 6; colStride = 1; alpOff = 4; alpBits = 1; alpStrideBits = 4 * 1; break;
                        case DDT_DXT5: extractor = Convert444ToRGB; stride = 16; colOff = 6; colStride = 3; alpOff = 4; alpBits = 4; alpStrideBits = 6 * 4; break;
                    }
                    for (int x = 0; x < Width / 4; ++x)
                    {
                        for (int y = 0; y < Height / 4; ++y)
                        {
                            int pixStart = stride * (y * Width / 4 + x);
                            int palStart = pixStart;
                            int colStart = pixStart + colOff;
                            int alpStart = pixStart + alpOff;
                            extractor(bytes, palStart, blockPalette);
                            block4Palette[0] = blockPalette[0];
                            block4Palette[2] = blockPalette[0].Lerp(blockPalette[1], 1, 3);
                            block4Palette[3] = blockPalette[0].Lerp(blockPalette[1], 2, 3);
                            block4Palette[1] = blockPalette[1];
                            for (int sy = 0; sy < 4; ++sy)
                            {
                                int py = y * 4 + sy;
                                int imgOff = sy * colStride + colStart;
                                int data = bytes[imgOff];
                                for (int sx = 0; sx < 4; ++sx)
                                {
                                    int v = (data >> (sx * 2)) & 0x03;
                                    Pixel c = block4Palette[v];
                                    int px = x * 4 + sx;
                                    int p = (px + py * Width) * 4;
                                    pixels[p + 0] = c.B;
                                    pixels[p + 1] = c.G;
                                    pixels[p + 2] = c.R;
                                    pixels[p + 3] = c.A;
                                }
                                if (alpBits > 0)
                                {
                                    int alpMask = (1 << alpBits) - 1;
                                    int alphaStartBit = alpStart * 8 + sy * alpStrideBits;
                                    for (int sx = 0; sx < 4; ++sx)
                                    {
                                        int alphaBit = alphaStartBit + sx * alpBits;
                                        int alphaByte = alphaBit / 8;
                                        int alphaBitOff = alphaBit - alphaByte * 8;
                                        int alphaData = ((bytes[alphaByte + 1]) << 8) + bytes[alphaByte];
                                        int px = x * 4 + sx;
                                        int p = (px + py * Width) * 4;
                                        pixels[p + 3] = (byte)(((alphaData >> alphaBitOff) & alpMask) * 255 / alpMask);
                                    }
                                }
                            }
                        }
                    }
                    break;

            }

        }





        private static void Convert565ToRGB(byte[] data, int startData, Pixel[] palette)
        {
            ConvertToRGB(data, startData, palette, 0, 5, 5, 6, 11, 5, 16);
        }
        private static void Convert555ToRGB(byte[] data, int startData, Pixel[] palette)
        {
            ConvertToRGB(data, startData, palette, 1, 5, 6, 5, 11, 5, 16);
        }
        private static void Convert444ToRGB(byte[] data, int startData, Pixel[] palette)
        {
            ConvertToRGB(data, startData, palette, 4, 4, 8, 4, 12, 4, 16);
        }
        private static void ConvertToRGB(byte[] data, int startData, Pixel[] palette, int rOff, int rCnt, int gOff, int gCnt, int bOff, int bCnt, int stride)
        {
            int rMask = ((1 << rCnt) - 1);
            int gMask = ((1 << gCnt) - 1);
            int bMask = ((1 << bCnt) - 1);
            for (int c = 0; c < palette.Length; ++c)
            {
                int cBit = startData * 8 + c * stride;
                int dByte = cBit / 8;
                // Will be such that the color starts at 24th bit
                //            Debug.Assert(dByte < data.Length,
                //                "Not enough data!");
                int b1 = data[dByte + 0];
                int b2 = (dByte + 1 < data.Length ? data[dByte + 1] : 0);
                int b3 = 0;// (dByte + 2 < data.Length ? data[dByte + 2] : 0);
                if (rCnt == 4)
                {
                    palette[c] = new Pixel()
                    {
                        R = (byte)((b1 << 4) & 0xf0),
                        G = (byte)((b1 << 0) & 0xf0),
                        B = (byte)((b2 << 4) & 0xf0),
                        A = 255,
                    };
                    //                Debug.Assert((b2 & 0xfffffff0) == 0);
                }
                else
                {
                    int dataV = ((((b3 << 8) + b2) << 8) + b1) << (cBit - dByte * 8);
                    int r = (((dataV << rOff) >> (16 - rCnt)) & rMask) * 255 / rMask;
                    int g = (((dataV << gOff) >> (16 - gCnt)) & gMask) * 255 / gMask;
                    int b = (((dataV << bOff) >> (16 - bCnt)) & bMask) * 255 / bMask;
                    palette[c] = new Pixel()
                    {
                        R = (byte)r,
                        G = (byte)g,
                        B = (byte)b,
                        A = 255,
                    };
                    if (rCnt == 5 && gCnt == 5 && bCnt == 5)
                    {
                        palette[c].A = ((dataV & 0x8000) == 0 ? (byte)0 : (byte)255);
                    }
                }
            }
        }
    }
}
