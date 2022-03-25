using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClientgRPC;
using Microsoft.Win32;
using Services.AtmosphericData;
using Services.CloudService;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        gRpcProvider Provider;
        CancellationToken SubscriptionStatus;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Provider = new gRpcProvider();       
            Task.Run(async () =>
            {
                var data = Provider.CloudService.ListFiles(new Services.CloudService.Empty());
                SubscriptionStatus = new CancellationToken();
                do
                {
                    await data.ResponseStream.MoveNext(SubscriptionStatus);
                    ImagesList list = data.ResponseStream.Current;
                    string[] files = list.FileNames.ToArray();
                    Dispatcher.Invoke(() =>
                    {
                        ListFilesAvailable.ItemsSource = files;
                    });
                }
                while (!SubscriptionStatus.IsCancellationRequested);
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AtmosphericData data = new AtmosphericData()
                {
                    Date = datePicker.SelectedDate.ToString(),
                    Temperature = int.Parse(txbTemperature.Text),
                    Humidity = int.Parse(txbHumidity.Text),
                    Pressure = int.Parse(txbPressure.Text),
                };
                Provider.AtmosphericDataHandler.SaveData(data);
            }
            catch (Exception ex)
            {
                labelSaveDataError.Content = "Incorrect data";
            }
        }

        private void AtmosphericData_Click(object sender, RoutedEventArgs e)
        {
            if(datePickerGetAtmospheric.SelectedDate == null)
            {
                var response = Provider.AtmosphericDataHandler.GetAllDataAsync(new Services.AtmosphericData.Empty());
                response.ResponseAsync.ContinueWith((res) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ListAtmosphericData.ItemsSource = res.Result.Data.ToList();
                    });
                });         
            }
            else
            {
                var response = Provider.AtmosphericDataHandler.GetDataAsync(new AtmosphericDataRequest()
                {
                    Date = datePickerGetAtmospheric.SelectedDate.ToString()
                });
                response.ResponseAsync.ContinueWith((res) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ListAtmosphericData.ItemsSource = res.Result.Data.ToList();
                    });
                });
            }
        }

        private async void SendFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF";
            bool? fileSelected = openFileDialog.ShowDialog();
            if(fileSelected.HasValue && fileSelected.Value)
            {
                Stream sr = openFileDialog.OpenFile();
                var upload = Provider.CloudService.Upload();
                await upload.RequestStream.WriteAsync(new Services.CloudService.ImageData() { 
                    FileName = openFileDialog.SafeFileName, 
                    FileSize = (int)sr.Length
                });
                do
                {
                    long readLength = sr.Length - sr.Position >= 512 ? 512 : sr.Length - sr.Position;
                    byte[] data = new byte[readLength];
                    sr.Read(data, 0, (int)readLength);
                    pbUpload.Value = (float)sr.Position / sr.Length * 100;
                    await upload.RequestStream.WriteAsync(new Services.CloudService.ImageData()
                    {
                        Data = Google.Protobuf.ByteString.CopyFrom(data)
                    });
                } while (sr.Position != sr.Length);
                await upload.RequestStream.CompleteAsync();
                pbUpload.Value = 0;
            }
        }

        private void DownloadFile_Click(object sender, RoutedEventArgs e)
        {
            if(sender is not Button)
            {
                return;
            }
            Button btn = sender as Button;
            string filename = (string)btn.Tag;
            Task.Run(async () =>
            {
                var download = Provider.CloudService.Download(
                new DownloadImageRequest() { FileName = filename.Split('\\')[^1] }
                );
                CancellationToken token = new CancellationToken();
                await download.ResponseStream.MoveNext(token);
                if (download.ResponseStream.Current == null || download.ResponseStream.Current.FileSize == 0)
                {
                    return;
                }
                int size = download.ResponseStream.Current.FileSize;
                if (string.IsNullOrEmpty(download.ResponseStream.Current.FileName))
                    filename = Guid.NewGuid().ToString();
                else
                    filename = download.ResponseStream.Current.FileName;
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                FileStream fs = File.Create($"{path}/{filename}");
                do
                {
                    await download.ResponseStream.MoveNext(token);
                    if (download.ResponseStream.Current != null)
                    {
                        byte[] data = download.ResponseStream.Current.Data.ToArray();
                        Dispatcher.Invoke(() =>
                        {
                            pbUpload.Value = (float)fs.Position / size * 100;
                        });                     
                        fs.Write(data, 0, data.Length);
                    }
                }
                while (fs.Position != size);
                fs.Close();
                Dispatcher.Invoke(() =>
                {
                    pbUpload.Value = 0;
                });
            });
        }
    }
}
