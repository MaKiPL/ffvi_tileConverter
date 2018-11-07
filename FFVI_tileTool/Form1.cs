using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FFVI_tileTool
{
    public partial class Form1 : Form
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

        string[] st;
        public Form1()
        {
            InitializeComponent();
        }

        private void browseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog() { Description = "Show me path to your map*.bin files" })
            {
                if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;
                st = Directory.GetFiles(folderBrowserDialog.SelectedPath, "map*.bin", SearchOption.TopDirectoryOnly);
                if (st.Length == 0) return;
                listBox1.DataSource = (from a in st select Path.GetFileName(a)).ToArray();
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBox listBox = (sender as ListBox);
            if (listBox.Items.Count == 0) return;
            if (st == null) return;
            if (st.Length == 0) return;

            
            string filePath = st.Where(x => Path.GetFileName(x) == (string)listBox.SelectedValue).First();
            RenderImage(filePath);
        }

        private void RenderImage(string file)
        {
            byte[] paletteBuffer = new byte[1024];
            byte[] secPaletteBuffer = new byte[4096];
            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            paletteBuffer = br.ReadBytes(1024);
            byte[] firstImageBuffer = br.ReadBytes(512 * 512);
            //secPaletteBuffer = br.ReadBytes(4096);
            byte[] secondImageBuffer = new byte[0];
            if (fs.Length > 0x80400 + 1024)
            {
                fs.Seek(-0x80400, SeekOrigin.End); //Ark's hack
                secPaletteBuffer = br.ReadBytes(1024);
                secondImageBuffer = br.ReadBytes((int)(fs.Length - fs.Position));
            }
            br.Close();
            fs.Close();
            fs.Dispose();


            Bitmap bmpOne = new Bitmap(512, 512, PixelFormat.Format8bppIndexed);

            MapTile mapTile = new MapTile() { palette = new Color[256], imgBuff = firstImageBuffer };
            for (int i = 0; i < mapTile.palette.Length; i++)
                mapTile.palette[i] = new Color() { R = paletteBuffer[i * 4], G = paletteBuffer[i * 4 + 1], B = paletteBuffer[i * 4 + 2], A = paletteBuffer[i * 4 + 3] };
            ColorPalette cp = bmpOne.Palette;
            for (int i = 0; i < 256; i++)
                cp.Entries[i] = System.Drawing.Color.FromArgb(
                    255 - mapTile.palette[i].A,
                    mapTile.palette[i].B,
                    mapTile.palette[i].G,
                    mapTile.palette[i].R);
            bmpOne.Palette = cp;
            BitmapData bmpData = bmpOne.LockBits(new Rectangle(0, 0, bmpOne.Width, bmpOne.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            byte[] bmpDataBuffer = new byte[bmpData.Width * bmpData.Height];
            Marshal.Copy(mapTile.imgBuff, 0, bmpData.Scan0, mapTile.imgBuff.Length);
            bmpOne.UnlockBits(bmpData);

            pictureBox1.Image = bmpOne;

            //two
            if (secondImageBuffer.Length > 512)
            {
                Bitmap bmpTwo = new Bitmap(512, secondImageBuffer.Length / 512, PixelFormat.Format8bppIndexed);
                mapTile = new MapTile() { palette = new Color[256], imgBuff = secondImageBuffer };
                for (int i = 0; i < mapTile.palette.Length; i++)
                    mapTile.palette[i] = new Color() { R = secPaletteBuffer[i * 4], G = secPaletteBuffer[i * 4 + 1], B = secPaletteBuffer[i * 4 + 2], A = secPaletteBuffer[i * 4 + 3] };
                cp = bmpTwo.Palette;
                for (int i = 0; i < 256; i++)
                    cp.Entries[i] = System.Drawing.Color.FromArgb(
                        255 - mapTile.palette[i].A,
                        mapTile.palette[i].B,
                        mapTile.palette[i].G,
                        mapTile.palette[i].R);
                bmpTwo.Palette = cp;
                bmpData = bmpTwo.LockBits(new Rectangle(0, 0, bmpTwo.Width, bmpTwo.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                bmpDataBuffer = new byte[bmpData.Width * bmpData.Height];
                Marshal.Copy(mapTile.imgBuff, 0, bmpData.Scan0, mapTile.imgBuff.Length);
                bmpTwo.UnlockBits(bmpData);
                pictureBox2.Image = bmpTwo;
            }

            pictureBox1.Image = bmpOne;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "*.PNG|*.PNG", FileName = $"{listBox1.SelectedValue}_chunk1.png" })
                if(listBox1.Items.Count != 0)
                    if (sfd.ShowDialog() == DialogResult.OK)
                        pictureBox1.Image.Save(sfd.FileName);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "*.PNG|*.PNG", FileName = $"{listBox1.SelectedValue}_chunk2.png" })
                if (listBox1.Items.Count != 0)
                    if (sfd.ShowDialog() == DialogResult.OK)
                        pictureBox2.Image.Save(sfd.FileName);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //is 1st chunk
            string path = "";
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "*.PNG|*.PNG", Multiselect = false })
                if (ofd.ShowDialog() == DialogResult.OK)
                    path = ofd.FileName;
                else return;

            Bitmap bmp = new Bitmap(path);
            if(bmp.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                MessageBox.Show("PNG is not 8BPP");
                return;
            }
            if(bmp.Height != 512 || bmp.Width != 512)
            {
                MessageBox.Show($"Chunk 1 is always 512x512! You are trying to import {bmp.Width}x{bmp.Height} PNG.");
                return;
            }
            byte[] palBuffer = BuildPalette(bmp);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, 512, 512), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            byte[] b = new byte[512 * 512 + 1024];
            Buffer.BlockCopy(palBuffer, 0, b, 0, 1024);
            Marshal.Copy(bmpData.Scan0, b, 1024, 512 * 512);
            bmp.UnlockBits(bmpData);

            string filePath = st.Where(x => Path.GetFileName(x) == (string)listBox1.SelectedValue).First();
            byte[] bb = File.ReadAllBytes(filePath);
            Buffer.BlockCopy(b, 0, bb, 0, b.Length);
            File.WriteAllBytes(filePath, bb);
            RenderImage(filePath);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //2nd chunk
            string path = "";
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "*.PNG|*.PNG", Multiselect = false })
                if (ofd.ShowDialog() == DialogResult.OK)
                    path = ofd.FileName;
                else return;

            Bitmap bmp = new Bitmap(path);
            if (bmp.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                MessageBox.Show("PNG is not 8BPP");
                return;
            }
            if (bmp.Width != 512)
            {
                MessageBox.Show($"Chunk 2 is always 512x width! You are trying to import {bmp.Width}x{bmp.Height} PNG.");
                return;
            }
            byte[] palBuffer = BuildPalette(bmp);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, 512, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
            byte[] b = new byte[512 * bmp.Height + 1024];
            Buffer.BlockCopy(palBuffer, 0, b, 0, 1024);
            Marshal.Copy(bmpData.Scan0, b, 1024, 512 * bmp.Height);
            bmp.UnlockBits(bmpData);

            string filePath = st.Where(x => Path.GetFileName(x) == (string)listBox1.SelectedValue).First();
            byte[] bb = File.ReadAllBytes(filePath);
            //if(bb.Length < 512*512+1024+b.Length + 512*24)
            //{
            //    MessageBox.Show("Second chunk is too big!");
            //    return;
            //}

            Buffer.BlockCopy(b, 0, bb, bb.Length-0x80400, b.Length);
            File.WriteAllBytes(filePath, bb);
            RenderImage(filePath);
        }

        private byte[] PaletteToByte(System.Drawing.Color[] pal)
        {
            throw new Exception("NO");
            byte[] b = new byte[1024];
            
            return b;
        }

        private static byte[] BuildPalette(Bitmap bmp)
        {
            byte[] palBuffer = new byte[1024];
            for (int i = 0; i < 256; i++)
            {
                palBuffer[i * 4 + 0] = bmp.Palette.Entries[i].B;
                palBuffer[i * 4 + 1] = bmp.Palette.Entries[i].G;
                palBuffer[i * 4 + 2] = bmp.Palette.Entries[i].R;
                palBuffer[i * 4 + 3] = (byte)(255 - bmp.Palette.Entries[i].A);
            }
            return palBuffer;
        }

        private void browseAndMassExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("In next release! Sorry, forgot about it yet I want to release working version right now");
            return;
        }
    }
}
