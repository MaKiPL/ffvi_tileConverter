using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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

        static void Main(string[] args)
        {
#if DEBUG
            //args = new string[] { @"D:\SteamLibrary\steamapps\common\Final Fantasy 6\obb\field\map" };
            args = new string[] { @"D:\SteamLibrary\steamapps\common\Final Fantasy 6\obb\field\map\convertedTiles" };
#endif

            if (args.Length == 0)
            {
                Console.WriteLine("Just run this with a path to your *.bin folder or convertedTiles folder");
                Console.ReadLine();
                return;
            }

            if (args[0].Contains("map\\convertedTiles"))
            #region IMPORT
            {
                string[] files = Directory.GetFiles(args[0], "map*.bin.png");
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
                    FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fs);
                    paletteBuffer = br.ReadBytes(1024);
                    byte[] imageBuffer = br.ReadBytes((int)(fs.Length - fs.Position));
                    br.Close();
                    fs.Close();
                    fs.Dispose();

                    Bitmap bmp = new Bitmap(512, imageBuffer.Length / 512 + 1, PixelFormat.Format32bppArgb);
                    MapTile mapTile = new MapTile() { palette = new Color[256], imgBuff = imageBuffer };
                    for (int i = 0; i < mapTile.palette.Length; i++)
                        mapTile.palette[i] = new Color() { R = paletteBuffer[i * 4], G = paletteBuffer[i * 4 + 1], B = paletteBuffer[i * 4 + 2], A = paletteBuffer[i * 4 + 3] };
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    byte[] bmpDataBuffer = new byte[bmpData.Width * bmpData.Height * 4];
                    Marshal.Copy(bmpData.Scan0, bmpDataBuffer, 0, bmpDataBuffer.Length);
                    for (int i = 0; i < mapTile.imgBuff.Length; i++)
                    {
                        bmpDataBuffer[i * 4] = mapTile.palette[mapTile.imgBuff[i]].R;
                        bmpDataBuffer[i * 4 + 1] = mapTile.palette[mapTile.imgBuff[i]].G;
                        bmpDataBuffer[i * 4 + 2] = mapTile.palette[mapTile.imgBuff[i]].B;
                        bmpDataBuffer[i * 4 + 3] = (byte)((bmpDataBuffer[i * 4] == 0 && bmpDataBuffer[i * 4 + 1] == 0 && bmpDataBuffer[i * 4 + 2] == 0) ?
                            0 :
                            (byte)(255 - mapTile.palette[mapTile.imgBuff[i]].A));
                    }
                    Marshal.Copy(bmpDataBuffer, 0, bmpData.Scan0, bmpDataBuffer.Length);
                    bmp.UnlockBits(bmpData);
                    bmp.Save($"{Path.GetDirectoryName(file)}\\convertedTiles\\{Path.GetFileName(file)}.png", ImageFormat.Png);
                }
            }
            #endregion
        }
    }
}
