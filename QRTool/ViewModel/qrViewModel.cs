using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace QRTool.ViewModel
{
    class QrViewModel : INotifyPropertyChanged
    {

        public Action<bool> FrameStart;

        private string decodedText;
        int minVal, maxVal, stepSize, defVal;
        string cameraControlFlags;
        bool isLandscape = false;
        //bool isQRScanned = false;
        bool isPhotMade;
        private string labelText;
        public bool makePhoto;
        public Image img;

        #region Commands
        private RelayCommand makeQRAgainCommand;
        public RelayCommand MakeQRAgainCommand
        {
            get
            {
                if (makeQRAgainCommand == null)
                {
                    makeQRAgainCommand = new RelayCommand(DoMakeQRAgain);
                }
                return makeQRAgainCommand;
            }
        }

        private RelayCommand makePhotoAgainCommand;
        public RelayCommand MakePhotoAgainCommand
        {
            get
            {
                if (makePhotoAgainCommand == null)
                {
                    makePhotoAgainCommand = new RelayCommand(DoMakePhotoAgain);
                }
                return makePhotoAgainCommand;
            }
        }

        private RelayCommand sendPhotoCommand;
        public RelayCommand SendPhotoCommand
        {
            get
            {
                if (sendPhotoCommand == null)
                {
                    sendPhotoCommand = new RelayCommand(DoSendPhotoAsync);
                }
                return sendPhotoCommand;
            }
        }

        private void DoSendPhotoAsync(object obj)
        {
            TaskSendPhotoAsync();
        }

        private RelayCommand makePhotoCommand;

        public RelayCommand MakePhotoCommand
        {
            get
            {
                if (makePhotoCommand == null)
                {
                    makePhotoCommand = new RelayCommand(DoMakePhoto);
                }
                return makePhotoCommand;
            }
        }

        #endregion

        #region Commands methods
        private void DoMakePhoto(object obj)
        {
            makePhoto = true;
        }

        private async void TaskSendPhotoAsync()
        {
            //Task sendPhotoAsync = new SendPhotoAsync();
            await Task.Run(() => SendPhoto(img));
            //await SendPhoto(img);
            IsPhotMade = false;
            DecodedText = String.Empty;
            FrameStart(true);
        }

        private void DoMakeQRAgain(object obj)
        {
            IsPhotMade = false;
            DecodedText = String.Empty;
            LabelText = "Scannen von QR-Code";
            FrameStart(true);
        }

        private void DoMakePhotoAgain(object obj)
        {
            IsPhotMade = false;
            DecodedText = "DecodedText";
            FrameStart(true);
        }

        #endregion

        public QrViewModel()
        {
            LabelText = "Scannen von QR-Code";
        }

        public string LabelText
        {
            get => labelText;
            set
            {
                labelText = value;
                OnPropertyChanged("LabelText");
            }
        }

        public bool IsPhotMade
        {
            get => isPhotMade;
            set
            {
                isPhotMade = value;
                OnPropertyChanged("IsPhotMade");
            }
        }

        public bool IsLandscape
        {
            get => isLandscape;
            set
            {
                isLandscape = value;
                OnPropertyChanged("IsLandscape");
            }
        }

        public bool IsQRDecoded
        {
            get
            {
                return (decodedText != null) && (decodedText != String.Empty);
            }
        }


        public bool IsButtonsVisL
        {
            get
            {
                return IsQRDecoded && isLandscape;
            }
        }

        public bool IsButtonsVisP
        {
            get
            {
                return IsQRDecoded && (!isLandscape);
            }
        }


        public string DecodedText
        {
            get => decodedText;
            set
            {
                decodedText = value;
                LabelText = "Bitte nehmen Sie ein Foto Ihres Belegs auf";
                OnPropertyChanged("IsQRDecoded");
                OnPropertyChanged("DecodedText");
                OnPropertyChanged("IsButtonsVisP");
                OnPropertyChanged("IsButtonsVisL");
                OnPropertyChanged("LabelText");
            }
        }

        public string CameraControlFlags
        {
            get => cameraControlFlags;
            set
            {
                cameraControlFlags = value;
                OnPropertyChanged("CameraControlFlags");
            }
        }


        public int MinVal
        {
            get => minVal;
            set
            {
                minVal = value;
                OnPropertyChanged("MinVal");
            }
        }

        public int MaxVal
        {
            get => maxVal;
            set
            {
                maxVal = value;
                OnPropertyChanged("MaxVal");
            }
        }
        public int StepSize
        {
            get => stepSize;
            set
            {
                stepSize = value;
                OnPropertyChanged("StepSize");
            }
        }
        public int DefVal
        {
            get => defVal;
            set
            {
                defVal = value;
                OnPropertyChanged("DefVal");
            }
        }

        public string UrlAddress { get; internal set; }
        public string TokenVal { get; internal set; }

        private void SendPhoto(Image myImage)
        {

            MemoryStream myMemoryStream = new MemoryStream();
            myImage.Save(myMemoryStream, ImageFormat.Jpeg);
            myMemoryStream.Position = 0;

            var client = new HttpClient();
            var image = myMemoryStream.ToArray(); // File.ReadAllBytes("c:\\test.png");
            var formDataLocal = new MultipartFormDataContent
            {
                { new StreamContent(myMemoryStream), "image", "tmp.ipeg" }
            };
            //formData.Add(new StringContent("content"), "name");
            var tmoURL = "http://reorder2-dev.telenorma.info:7777/image";
            var response = client.PostAsync(tmoURL, formDataLocal).Result;

            if (!response.IsSuccessStatusCode)
            {
                //Console.WriteLine(response.StatusCode);
                return;
            }
            else
            {

                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                var myAnonyClassObject = new[] { new { url = string.Format("{0}/{1}", tmoURL, content) } };
                string myJson = String.Format("{{\"url\": \"{0}/{1}\"}}", tmoURL, content);
                Console.WriteLine(myJson);
                var formData = new MultipartFormDataContent();
                var data = new StringContent(myJson, Encoding.UTF8, "application/json");
                formData.Add(data, "data");
                //formData.Add(new StringContent(JsonConvert.SerializeObject(myAnonyClassObject), Encoding.UTF8, "application/json"), "data");
                formData.Add(new StringContent(TokenVal), "token");
                Console.WriteLine(UrlAddress);
                response = client.PostAsync(UrlAddress, formData).Result;

            }
        }

        readonly string[] f = new string[] { "url", "token", "/", "{", "}", "\\", ":", "\"", "," };

        public void Reader_ResultFound(Result obj)
        {

            string decoded = obj.Text.Trim();
            DecodedText = decoded;
            var res = decoded.Split(f, StringSplitOptions.RemoveEmptyEntries);
            if (res.Length == 3)
            {
                //qrwm.urlAddress = string.Format("{0}://{1}?token={2}", res[0], res[1], res[2]);
                UrlAddress = string.Format("{0}://{1}/api/add_image/", res[0], res[1]);
                TokenVal = res[2];
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        internal void IsLandscapeInverse()
        {
            isLandscape = !isLandscape;
            OnPropertyChanged("IsLandscape");
        }
    }
}
