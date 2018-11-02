using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FFVI_tileTool
{
    class Program
    {
        struct Color
        {
            public byte R;
            public byte G;
            public byte B;
            public byte A;
        }

        struct MapTile
        {
            public Color[] palette;
            public byte[] imgBuff;
        }

        [STAThread]
        static void Main(string[] args)
        {
#if DEBUG
            //args = new string[] { @"D:\SteamLibrary\steamapps\common\Final Fantasy 6\obb\field\map" };
            //args = new string[] { @"D:\SteamLibrary\steamapps\common\Final Fantasy 6\obb\field\map\convertedTiles" };
#endif

            if (args.Length == 0)
            {
            Application.EnableVisualStyles();
            Application.Run(new Form1());
                }

            //fallback to old algorithm

            if (args[0].Contains("map\\convertedTiles"))
            #region IMPORT
            {
                string[] files = Directory.GetFiles(args[0], "map*.bin.png");
                foreach(string file in files)
                {
                    Bitmap bmp = new Bitmap(file);
                    switch(bmp.PixelFormat)
                    {
                        case PixelFormat.Format8bppIndexed:
                            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
                                throw new Exception("Conversion failed. Contact Maki...");
                            byte[] palBuffer = BuildPalette(bmp);
                            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                            byte[] imageBuffer = new byte[bmpData.Width * bmpData.Height];
                            Marshal.Copy(bmpData.Scan0, imageBuffer, 0, imageBuffer.Length);
                            bmp.UnlockBits(bmpData);
                            using (FileStream fs = new FileStream(file.Substring(0, file.Length - 4), FileMode.OpenOrCreate, FileAccess.Write))
                                using (BinaryWriter bw = new BinaryWriter(fs)) { bw.Write(palBuffer); bw.Write(imageBuffer); }
                                break;
                        case PixelFormat.Format24bppRgb:
                        case PixelFormat.Format32bppArgb:
                            ConvertBPP(ref bmp, file);
                            goto case PixelFormat.Format8bppIndexed;
                        default:
                            throw new Exception($"Unsupported PixelFormat in {file}. Expected: 8, 24 or 32BPP; Ind; RGB; ARGB. Got: {bmp.PixelFormat}");
                    }
                }
//TODO
            }
            #endregion
            #region EXPORT
            else
            {
                string[] files = Directory.GetFiles(args[0], "*.bin");
                if (!Directory.Exists(Path.Combine(args[0], "convertedTiles")))
                    Directory.CreateDirectory(Path.Combine(args[0], "convertedTiles"));
                foreach (string file in files)
                {
                    byte[] paletteBuffer = new byte[1024];
                    byte[] secPaletteBuffer = new byte[4096];
                    FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fs);
                    paletteBuffer = br.ReadBytes(1024);
                    byte[] firstImageBuffer = br.ReadBytes(512*512);
                    //byte[] firstImageBuffer = br.ReadBytes((int)(fs.Length - fs.Position));
                    //fs.Seek(263168, SeekOrigin.Begin);
                    secPaletteBuffer = br.ReadBytes(4096);
                    byte[] secondImageBuffer = br.ReadBytes((int)(fs.Length - fs.Position));
                    br.Close();
                    fs.Close();
                    fs.Dispose();


                    Bitmap bmpOne = new Bitmap(512, 512, PixelFormat.Format8bppIndexed);
                    //bmpSecond ; varied size
                    //second bmp is 512* (imagebuffer/512). It's always INT
                    throw new Exception("NOT IMPLEMENTED, Go away");
                    MapTile mapTile = new MapTile() { palette = new Color[256], imgBuff = firstImageBuffer };
                    for (int i = 0; i < mapTile.palette.Length; i++)
                        mapTile.palette[i] = new Color() { R = paletteBuffer[i * 4], G = paletteBuffer[i * 4 + 1], B = paletteBuffer[i * 4 + 2], A = paletteBuffer[i * 4 + 3] };
                    ColorPalette cp = bmpOne.Palette;
                    for (int i = 0; i < 256; i++)
                        cp.Entries[i] = System.Drawing.Color.FromArgb(
                            255-mapTile.palette[i].A,
                            mapTile.palette[i].B,
                            mapTile.palette[i].G,
                            mapTile.palette[i].R);
                    bmpOne.Palette = cp;
                    BitmapData bmpData = bmpOne.LockBits(new Rectangle(0, 0, bmpOne.Width, bmpOne.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                    byte[] bmpDataBuffer = new byte[bmpData.Width * bmpData.Height];

                    Marshal.Copy(mapTile.imgBuff, 0, bmpData.Scan0, mapTile.imgBuff.Length);
                    //Marshal.Copy(bmpData.Scan0, bmpDataBuffer, 0, bmpDataBuffer.Length);
                    //for (int i = 0; i < mapTile.imgBuff.Length; i++)
                    //{
                    //   /* bmpDataBuffer[i * 4] = mapTile.palette[mapTile.imgBuff[i]].R;
                    //    bmpDataBuffer[i * 4 + 1] = mapTile.palette[mapTile.imgBuff[i]].G;
                    //    bmpDataBuffer[i * 4 + 2] = mapTile.palette[mapTile.imgBuff[i]].B;
                    //    bmpDataBuffer[i * 4 + 3] = (byte)((bmpDataBuffer[i * 4] == 0 && bmpDataBuffer[i * 4 + 1] == 0 && bmpDataBuffer[i * 4 + 2] == 0) ?
                    //        0 :
                    //        (byte)(255 - mapTile.palette[mapTile.imgBuff[i]].A));*/
                       
                    //}
                    //Marshal.Copy(bmpDataBuffer, 0, bmpData.Scan0, bmpDataBuffer.Length);
                    bmpOne.UnlockBits(bmpData);
                    bmpOne.Save($"{Path.GetDirectoryName(file)}\\convertedTiles\\{Path.GetFileName(file)}.png", ImageFormat.Png);
                }
            }
            #endregion
        }

        private static byte[] BuildPalette(Bitmap bmp)
        {
            byte[] palBuffer = new byte[1024];
            for(int i = 0; i<256; i++)
            {
                palBuffer[i * 4 + 0] = bmp.Palette.Entries[i].B;
                palBuffer[i * 4 + 1] = bmp.Palette.Entries[i].G;
                palBuffer[i * 4 + 2] = bmp.Palette.Entries[i].R;
                palBuffer[i * 4 + 3] = (byte)(255-bmp.Palette.Entries[i].A);
            }
            return palBuffer;
        }

        private static void ConvertBPP(ref Bitmap bmp, string file)
        {
            Console.WriteLine($"{file} is not 8BPP. Converting from {bmp.PixelFormat}");
            Dictionary<Color, int> colorList = new Dictionary<Color, int>();
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            byte[] bmpBuffer = new byte[bmpData.Width*bmpData.Height*(bmp.PixelFormat==PixelFormat.Format24bppRgb?3:4)];
            Marshal.Copy(bmpData.Scan0, bmpBuffer, 0, bmpBuffer.Length);
            bmp.UnlockBits(bmpData);
            int multiplier = bmp.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;
            for (int i = 0; i<bmpBuffer.Length; i+= multiplier)
            {
                Color color = new Color()
                {
                    R = bmpBuffer[i],
                    G = bmpBuffer[i + 1],
                    B = bmpBuffer[i + 2],
                    A = (byte)(bmp.PixelFormat == PixelFormat.Format24bppRgb ? 255 : bmpBuffer[i + 3])
                };
                if (colorList.ContainsKey(color))
                    colorList[color]++;
                else colorList.Add(color, 1);
            }
            if (colorList.Count > 255)
                Console.WriteLine($"File {file} contains more than 255 colors. Got: {colorList.Count}");
            var cl = colorList.Keys.ToList();
            var clv = colorList.Values.ToList();
            List<Tuple<Color, int>> cpl = new List<Tuple<Color, int>>();
            for (int i = 0; i < cl.Count; i++)
                cpl.Add(new Tuple<Color, int>(cl[i], clv[i]));
            cpl = cpl.OrderByDescending(x => x.Item2).ToList();

            Bitmap bb = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format8bppIndexed);
            ColorPalette cp = bb.Palette;
            for (int i = 0; i < 256; i++)
                if (i >= cpl.Count)
                    cp.Entries[i] = System.Drawing.Color.Black;
                else
                    cp.Entries[i] = System.Drawing.Color.FromArgb(cpl[i].Item1.A, cpl[i].Item1.R, cpl[i].Item1.G, cpl[i].Item1.B);
            bb.Palette = cp;
            BitmapData bbData = bb.LockBits(new Rectangle(0, 0, bb.Width, bb.Height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            byte[] bbBuffer = new byte[bbData.Height * bbData.Width];
            
            Marshal.Copy(bbData.Scan0, bbBuffer, 0, bbBuffer.Length);
            
            for (int i = 0; i<bbBuffer.Length; i++)
            {
                byte R = bmpBuffer[i * multiplier];
                byte G = bmpBuffer[i * multiplier + 1];
                byte B = bmpBuffer[i * multiplier + 2];
                byte A = (byte)(multiplier == 3 ? 255 : bmpBuffer[i * multiplier + 3]);
                System.Drawing.Color clr = System.Drawing.Color.FromArgb(A, R, G, B);
                int colorIndex = -1;
                for (int n = 0; n < 256; n++)
                    if (bb.Palette.Entries[n] == clr) { colorIndex = n; break; }
                bbBuffer[i] = (byte)(colorIndex != -1 ? colorIndex : 255);
            }
            Marshal.Copy(bbBuffer, 0, bbData.Scan0, bbBuffer.Length);
            bb.UnlockBits(bbData);
            bmp = bb;
        }
    }
}
