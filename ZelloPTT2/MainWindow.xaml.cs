using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.IO.Ports;

namespace ZelloPTT2
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref System.Drawing.Point lpPoint);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
        private List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();
        private List<System.Windows.Controls.Image> Images = new List<System.Windows.Controls.Image>();

        SerialPort serial;

        const int SampleWidth = 100;
        const int SampleHeight = 100;
        const int treshold = 5;

        public MainWindow()
        {
            InitializeComponent();
            
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();

            //COMPorts.Items.Add("COM1");
            //COMPorts.Items.Add("COM3");

            foreach (var p in ports)
            {
                COMPorts.Items.Add(p);
            }

            serial = new SerialPort();

            var bmp = new WriteableBitmap(SampleWidth, SampleHeight, 96d, 96d, PixelFormats.Bgr32, null);
            bitmaps.Add(bmp);
            var img = new System.Windows.Controls.Image() { Margin = new Thickness(5) };
            Images.Add(img);
            ImgStackPanel.Children.Add(img);

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        public BitmapImage ConvertToBitmapImage(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            int zelloPixels = 0;

            //for (int i = 0; i < 10; i++)
            //{

                int startX = (int)System.Windows.SystemParameters.PrimaryScreenWidth - 100;
                int startY = 200 /*+ i * SampleHeight*/;

                Rectangle rect = new Rectangle(startX, startY, SampleWidth, SampleHeight);
                Bitmap bmp = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics g = Graphics.FromImage(bmp);
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);

                int fromX = 10;
                int toX = SampleWidth - fromX;
                int fromY = 40;
                int toY = SampleHeight - fromY;


                for (int x = fromX; x < toX; x++)
                    for (int y = fromY; y < toY; y++)
                    {
                        var p = bmp.GetPixel(x, y);

                        if ((p.R > 110 && p.R < 130 && p.G > 220 && p.B > 80 && p.B < 110) || (p.R > 80 && p.R < 130 && p.G > 180 && p.B > 80 && p.B < 160) || (p.R > 25 && p.R < 50 && p.G > 160 && p.B > 10 && p.B < 60))
                        {
                            zelloPixels++;
                        }

                        bmp.SetPixel(x, fromY, System.Drawing.Color.ForestGreen);
                        bmp.SetPixel(x, toY, System.Drawing.Color.ForestGreen);
                        bmp.SetPixel(fromX, y, System.Drawing.Color.ForestGreen);
                        bmp.SetPixel(toX, y, System.Drawing.Color.ForestGreen);
                    }

                Images[0].Source = ConvertToBitmapImage(bmp);
            //}

            Hits.Text = "Hits: " + zelloPixels.ToString();
            PTT.Foreground = new SolidColorBrush((zelloPixels > treshold) ? Colors.Red : Colors.Black);

            serial.DtrEnable = (zelloPixels > treshold);
        }

        private void COMPorts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (serial.IsOpen) serial.Close();
            serial = new SerialPort(COMPorts.SelectedValue.ToString(), 115200, Parity.None, 8, StopBits.One);
            serial.Open();
            serial.DtrEnable = false; // sends 0 to DTR
        }
    }
}



