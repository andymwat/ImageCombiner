using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageCombiner
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            button2.Enabled = checkBox1.Checked;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            refreshPreview();
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }


        private enum ResizeMode
        {
            ShrinkLarger,
            ExpandSmaller,
            NoResize_Transparency,
            NoResize_BorderColorBackground
        }

        /// <summary>
        /// Combines two images above each other, with an optional border.
        /// </summary>
        /// <param name="top">The top image.</param>
        /// <param name="bottom">The bottom image.</param>
        /// <param name="borderSize">The size of the border, in pixels. -1 is no border.</param>
        /// <param name="borderColor">The color of the border, if there is one.</param>
        /// <param name="resizeMode">Options for resizing smaller/larger images.</param>
        /// <returns></returns>
        private Image combineImages(Image top, Image bottom, int borderSize, Color borderColor, ResizeMode resizeMode)
        {
            int whichSmaller = 0; //-1 = bottom, 0 = even; 1 = top
            if (top.Width < bottom.Width)
                whichSmaller = 1;
            if (bottom.Width < top.Width)
                whichSmaller = -1;

            Image resized = new Bitmap(1,1);
            if (whichSmaller == 0)
            {
                resized = new Bitmap(top.Width, 1);//set width for later (border calculation)
            }
            int totalHeight, totalWidth;
            totalHeight = totalWidth = 0;
            totalWidth = top.Width;
            if (whichSmaller != 0 && resizeMode == ResizeMode.ExpandSmaller) //if needs to be resized and expanding the smaller one
            {
                
                double scaleRatio; //what the smaller image needs to be multiplied by to scale up
                if (whichSmaller == -1) //bottom smaller
                {
                    scaleRatio = (double)top.Width / bottom.Width;
                    resized = ResizeImage(bottom, top.Width, Convert.ToInt32(bottom.Height * scaleRatio));
                    totalHeight = resized.Height + top.Height;
                }
                else if (whichSmaller == 1) //top smaller
                {
                    scaleRatio = (double)bottom.Width / top.Width;
                    resized = ResizeImage(top, bottom.Width, Convert.ToInt32(top.Height * scaleRatio));
                    totalHeight = resized.Height + bottom.Height;
                }
                else
                {
                    throw new Exception("Should never get here.");
                }

                totalWidth = resized.Width;
            }

            
            if (borderSize > 0)
            {
                totalHeight += borderSize;
            }


            Image resultImage = new Bitmap(totalWidth, totalHeight);
            
            Graphics g = Graphics.FromImage(resultImage);
            Point offset = new Point(0, 0);


            //draw top image
            if (whichSmaller != 0 && resizeMode == ResizeMode.ExpandSmaller)
            {
                if (whichSmaller == -1) //bottom smaller
                {
                    g.DrawImage(top, offset.X, offset.Y, top.Width, top.Height);
                    offset.Y += top.Height;
                }
                else if (whichSmaller == 1) //top smaller
                {
                    g.DrawImage(resized, offset.X, offset.Y, resized.Width, resized.Height);
                    offset.Y += resized.Height;
                }
                else
                {
                    throw new Exception("Should never get here.");
                }
                
            }

            //draw border (if any)
            if (borderSize > 0)
            {
                g.FillRectangle(new SolidBrush(borderColor), offset.X, offset.Y, resized.Width, borderSize);
                offset.Y += borderSize;
            }


            //draw bottom image
            if (whichSmaller != 0 && resizeMode == ResizeMode.ExpandSmaller)
            {
                if (whichSmaller == -1) //bottom smaller
                {
                    g.DrawImage(resized, offset.X, offset.Y, resized.Width, resized.Height);
                }
                else if (whichSmaller == 1) //top smaller
                {
                    g.DrawImage(bottom, offset.X, offset.Y, bottom.Width, bottom.Height);
                }
                else
                {
                    throw new Exception("Should never get here.");
                }

            }
            

            return resultImage;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            button1.Enabled = !checkBox2.Checked;
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private Color color = Color.Black;
        private List<Image> images = new List<Image>();
        private List<string> imageNames = new List<string>();
        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult result = colorDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                color = colorDialog1.Color;
                pictureBox2.Image = new Bitmap(pictureBox2.Width, pictureBox2.Height);
                Graphics g = Graphics.FromImage(pictureBox2.Image);
                g.Clear(color);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                foreach (string file in openFileDialog1.FileNames)
                {
                    Image newImage = Image.FromFile(file);
                    images.Add(newImage);
                    imageNames.Add(Path.GetFileName(file));
                }
                toolStripStatusLabel1.Text = "File(s) loaded.";

            }

            if (checkBox2.Checked)
            {
                refreshPreview();
            }

            listView1.Items.Clear();
            foreach (string i in imageNames)
            {
                listView1.Items.Add(i);
            }
            

        }

        private void refreshPreview()
        {
            if (images.Count < 2)
            {
                return;
            }
            Image final = images[0];
            for (int i = 1; i < images.Count; i++)
            {
                final = combineImages(final, images[i], 10, color, ResizeMode.ExpandSmaller);
            }

            pictureBox1.Image = final;
        }



        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                pictureBox1.Image.Save(saveFileDialog1.FileName);
                toolStripStatusLabel1.Text = "Saved.";
            }
            else
            {
                toolStripStatusLabel1.Text = "Save canceled.";
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            DialogResult result = colorDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                color = colorDialog1.Color;
                pictureBox2.Image = new Bitmap(pictureBox2.Width, pictureBox2.Height);
                Graphics g = Graphics.FromImage(pictureBox2.Image);
                g.Clear(color);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count <= 0 || listView1.FocusedItem == null)
                return;

            images.RemoveAt(listView1.FocusedItem.Index);
            imageNames.RemoveAt(listView1.FocusedItem.Index);
            listView1.Items.Clear();
            foreach (string i in imageNames)
            {
                listView1.Items.Add(i);
            }
        }
    }
}
