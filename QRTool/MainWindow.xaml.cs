using AForge.Video;
using AForge.Video.DirectShow;
using QRTool.ViewModel;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using ZXing;

// {"url":"https:\/\/highendsmoke.kassesvn.tn-rechenzentrum1.de\/","token":"4eff6c8315d1cab7f0080c0c016faebef611ffbbce187a0a1edaf6c498611475"}

// https://highendsmoke.kassesvn.tn-rechenzentrum1.de?token=4eff6c8315d1cab7f0080c0c016faebef611ffbbce187a0a1edaf6c498611475

namespace QRTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;

        QrViewModel qrwm = new QrViewModel();

        //bool makePhoto = false;
        private object objQR = new object();
        //System.Drawing.Image img;


        //private Bitmap frame;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = qrwm;

            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            qrwm.FrameStart = delegate (bool start)
            {
                if (start)
                {
                    videoSource.NewFrame += VideoSource_NewFrame;
                }
                else
                {
                    videoSource.NewFrame -= VideoSource_NewFrame;
                }
            };
                //new Action<bool>(StartFrame);

            if (videoDevices.Count > 0)
            {
                StartScanning();
            }

        }

        //private void Button_Click(object sender, RoutedEventArgs e)
        private void StartScanning()
        {

            try
            {

                if ((videoSource == null) || (!videoSource.IsRunning))
                {
                    // front camera
#if DEBUG
                    int ind = 0;
#else
                    int ind = 1;
#endif
                    videoSource = new VideoCaptureDevice(videoDevices[ind].MonikerString);
                    videoSource.VideoResolution = videoSource.VideoCapabilities.Last();
                    videoSource.ProvideSnapshots = true;

                    qrwm.StepSize = videoSource.SnapshotCapabilities.Length;
                    qrwm.FrameStart(true);

                    videoSource.SetCameraProperty(CameraControlProperty.Focus, 100, CameraControlFlags.Auto);
                    videoSource.SetCameraProperty(CameraControlProperty.Exposure, -5, CameraControlFlags.Auto);

                    videoSource.Start();

                }
                else
                {
                    qrwm.FrameStart(false);
                    videoSource.SignalToStop();

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


        }

        private void VideoSource_SnapshotFrame(object sender, NewFrameEventArgs eventArgs)
        {
            System.Drawing.Image img = (Bitmap)eventArgs.Frame.Clone();
        }

        Thread thr; // = new Thread();

        private void VideoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {

            qrwm.img = (Bitmap)eventArgs.Frame.Clone();

            if (qrwm.makePhoto)
            {
                qrwm.FrameStart(false);
                qrwm.makePhoto = false;
                
                qrwm.IsPhotMade = true;
                qrwm.DecodedText = String.Empty;
                return;
            }

            MemoryStream ms = new MemoryStream();
            qrwm.img.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();

            bi.Freeze();
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                if (qrwm.IsLandscape)
                {
                    //frameHolder.Source = bi;
                }
                else
                {
                    frameHolderPort.Source = bi;
                }
            }));

            if (!qrwm.IsQRDecoded)
            {
                try
                {
                    if ((thr == null) || (thr.ThreadState != ThreadState.Running))
                    {
                        thr = new Thread(new ThreadStart(delegate
                        {
                            try
                            {
                                Bitmap bitmap = new Bitmap(qrwm.img);
                                BarcodeReader reader = new BarcodeReader { AutoRotate = true, TryInverted = true };
                                reader.ResultFound += qrwm.Reader_ResultFound;
                                Result result = reader.Decode(bitmap);
                            }
                            catch { }
                        }));
                        thr.Start();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void VideoSource_VideoSourceError(object sender, AForge.Video.VideoSourceErrorEventArgs eventArgs)
        {
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ((videoSource != null) && (videoSource.IsRunning))
            {
                videoSource.SignalToStop();
            }
        }

        private void Button_Click_Rotate(object sender, RoutedEventArgs e)
        {
            qrwm.IsLandscapeInverse();
        }



        private void Button_Click_Exit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
