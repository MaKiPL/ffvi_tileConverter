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
            secPaletteBuffer = br.ReadBytes(4096);
            fs.Seek(512 * 24, SeekOrigin.Current);
            byte[] secondImageBuffer = br.ReadBytes((int)(fs.Length - fs.Position));
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
                    mapTile.palette[i] = new Color() { R = paletteBuffer[i * 4], G = paletteBuffer[i * 4 + 1], B = paletteBuffer[i * 4 + 2], A = paletteBuffer[i * 4 + 3] };
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
            //is first import button; img needs to be 512x512
            //WIP - import the image via bitlock, copy buffer, check if 512x512 and parse palette- therefore check if 8bpp
            //then just put to st[] array (filelist) and copy to buffer on overwrite
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //see button2_Click
        }
    }
}
