using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace ClipImage
{
    public partial class Form1 : Form
    {
        int mouseX = 0, mouseY = 0;
        int mouseMoveX = 0, mouseMoveY = 0;
        bool drawing = false;
        Pen redPen = new Pen(Color.Red);
        Rectangle drawingRect = new Rectangle();
        String srcFolder = "";

        public Form1()
        {
            InitializeComponent();

            Init();
        }

        void Init()
        {
            this.toolStrip1.ItemClicked += new ToolStripItemClickedEventHandler(toolStrip1_ItemClicked);
            this.pictureBox1.MouseDown += new MouseEventHandler(pictureBox1_MouseDown);
            this.pictureBox1.MouseMove += new MouseEventHandler(pictureBox1_MouseMove);
            this.pictureBox1.MouseUp += new MouseEventHandler(pictureBox1_MouseUp);
            this.tvImgFiles.AfterSelect += new TreeViewEventHandler(tvImgFiles_AfterSelect);
        }

        void tvImgFiles_AfterSelect(object sender, TreeViewEventArgs e)
        {
            String imgFileName = Path.Combine(srcFolder, e.Node.Text);
            if (File.Exists(imgFileName))
            {
                if (this.pictureBox1.Image != null)
                {
                    this.pictureBox1.Image.Dispose();
                }
                this.pictureBox1.Image = Image.FromFile(imgFileName);
            }
        }

        void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.drawing)
            {
                this.drawing = false;
                mouseMoveX = e.X;
                mouseMoveY = e.Y;
                this.pictureBox1.Refresh();
            }
        }

        void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drawing)
            {
                mouseMoveX = e.X;
                mouseMoveY = e.Y;
                this.pictureBox1.Refresh();
            }
        }

        void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!drawing)
            {
                drawing = true;
                mouseX = e.X;
                mouseY = e.Y;
            }
        }

        void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Name.ToUpper())
            {
                case "TSBCLIP":
                    Clip();
                    break;
                case "TSBOPEN":
                    OpenImgFolder();
                    break;
                default:
                    break;
            }
        }

        void Clip()
        {
            if (drawing)
            {
                return;
            }
            String outputFolder = Path.Combine(srcFolder,"output");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            if (drawingRect.Height == 0 || drawingRect.Width == 0)
            {
                MessageBox.Show("还没有选择裁剪区域哦~");
                return;
            }

            Rectangle rect=new Rectangle();
            foreach (TreeNode tn in this.tvImgFiles.Nodes[0].Nodes)
            {
                String srcFile = System.IO.Path.Combine(srcFolder, tn.Text);

                Bitmap src = Image.FromFile(srcFile) as Bitmap;
                Bitmap target = new Bitmap(drawingRect.Width, drawingRect.Height);

                float scale = GetScale(src.Width,src.Height);
                int widthOnControl = (int)(scale * src.Width);
                int heightOnControl = (int)(scale * src.Height);
                int XOnControl=(pictureBox1.Width-widthOnControl)/2;
                int YonControl=(pictureBox1.Height-heightOnControl)/2;
                int ClipStartX = drawingRect.X - XOnControl;
                int ClipStartY = drawingRect.Y - YonControl;

                rect.X=(int)(ClipStartX/scale);
                rect.Y=(int)(ClipStartY/scale);
                rect.Width= (int)(drawingRect.Width/scale);
                rect.Height= (int)(drawingRect.Height/scale);

                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height),
                                     rect,
                                     GraphicsUnit.Pixel);
                }
                target.Save(System.IO.Path.Combine(outputFolder, tn.Text));
            }

            if (MessageBox.Show("转换完成了，需要我帮你打开输出文件夹吗？","提示",MessageBoxButtons.OKCancel)==DialogResult.OK)
            {
                Process.Start(outputFolder);
            }
        }

        float GetScale(int width, int height)
        {
            int w_i = width;
            int h_i = height;
            int w_c = pictureBox1.Width;
            int h_c = pictureBox1.Height;
            float imageRatio = w_i / (float)h_i; // image W:H ratio
            float containerRatio = w_c / (float)h_c; // container W:H ratio

            if (imageRatio >= containerRatio)
            {
                // horizontal image
                return w_c / (float)w_i;
            }
            else { 
                // vertical image
                return h_c / (float)h_i;
            }
        }

        List<String> GetFilesFrom(String searchFolder, String[] filters, bool isRecursive)
        {
            List<String> filesFound = new List<String>();
            var searchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var filter in filters)
            {
                filesFound.AddRange(Directory.GetFiles(searchFolder, String.Format("*.{0}", filter), searchOption));
            }
            return filesFound;
        }

        void OpenImgFolder()
        {
            String imgFolder = "";
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                imgFolder = fbd.SelectedPath;
                srcFolder = imgFolder;
            }
            else
            {
                return;
            }

            var filters = new String[] { "jpg", "jpeg", "png", "gif", "tiff", "bmp", "svg" };
            List<String> imgFiles = GetFilesFrom(imgFolder, filters, false);

            this.tvImgFiles.Nodes[0].Nodes.Clear();
            foreach (string item in imgFiles)
            {
                this.tvImgFiles.Nodes[0].Nodes.Add(new TreeNode(Path.GetFileName(item)));
            }

            this.tvImgFiles.Nodes[0].ExpandAll();
            if (this.tvImgFiles.Nodes[0].Nodes.Count!=0)
            {
                this.tvImgFiles.SelectedNode = this.tvImgFiles.Nodes[0].Nodes[0];
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            int startX = Math.Min(mouseX, mouseMoveX);
            int stopX = Math.Max(mouseX, mouseMoveX);
            int startY = Math.Min(mouseY, mouseMoveY);
            int stopY = Math.Max(mouseY, mouseMoveY);

            drawingRect.X = startX;
            drawingRect.Y = startY;
            drawingRect.Width = stopX - startX;
            drawingRect.Height = stopY - startY;
            e.Graphics.DrawRectangle(redPen, drawingRect);
        }

    }
}
