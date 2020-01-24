using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EarthLiveUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private CancellationTokenSource cancelToken = new System.Threading.CancellationTokenSource();
        private bool taskRegistered = false;
        private readonly string taskName = "BackGroundDownloadService";
        private readonly string taskEntryPoint = "Tasks.DownloadServicesBackgroundTask";
        public MainPage()
        {
            this.InitializeComponent();
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    taskRegistered = true;
                }
            }
            ChangeWidgetState();
        }

        private void button_start_Click(object sender, RoutedEventArgs e)
        {
            if (!taskRegistered)
            {
                //var trigger = new TimeTrigger(15,false);
                var trigger = new SystemTrigger(SystemTriggerType.TimeZoneChange,false);
                var builder = new BackgroundTaskBuilder();
                builder.Name = taskName;
                builder.TaskEntryPoint = taskEntryPoint;
                builder.SetTrigger(trigger);
                var task = builder.Register();
                task.Completed += Task_CompletedAsync;
                taskRegistered = true;
            }
            ChangeWidgetState();
            //cancelToken = new CancellationTokenSource();
            //await new DownloaderHimawari8().UpdateImage(cancelToken, imageView);
        }

        private async void Task_CompletedAsync(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
             {
                 var writeablebmp = new WriteableBitmap(550 * Config.size, 550 * Config.size);
                 for (int ii = 0; ii < Config.size; ii++)
                 {
                     for (int jj = 0; jj < Config.size; jj++)
                     {
                         var file = await ApplicationData.Current.LocalFolder.GetFileAsync(string.Format("{0}_{1}.png", ii, jj));
                         var image = await BitmapFactory.FromStream(new FileStream(file.Path, FileMode.Open));
                         Debug.WriteLine(file.Path);
                         writeablebmp.Blit(new Windows.Foundation.Rect(550 * ii, 550 * jj, 550, 550), image, new Windows.Foundation.Rect(0, 0, image.PixelWidth, image.PixelHeight));
                     }
                 }
                 var storageFile = await WriteableBitmapToStorageFile(writeablebmp, FileFormat.Jpeg);
                 if (UserProfilePersonalizationSettings.IsSupported())
                 {
                     UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                     var success = await profileSettings.TrySetWallpaperImageAsync(storageFile);
                     //download_status.Text = success ? "success" : "failed";
                 }
             }
            );
            
        }
        private enum FileFormat
        {
            Jpeg,
            Png,
            Bmp,
            Tiff,
            Gif
        }
        private async Task<StorageFile> WriteableBitmapToStorageFile(WriteableBitmap WB, FileFormat fileFormat)
        {
            string FileName = "wallpaper";
            Guid BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            switch (fileFormat)
            {
                case FileFormat.Jpeg:
                    FileName += ".jpeg";
                    BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                    break;

                case FileFormat.Png:
                    FileName += ".png";
                    BitmapEncoderGuid = BitmapEncoder.PngEncoderId;
                    break;

                case FileFormat.Bmp:
                    FileName += ".bmp";
                    BitmapEncoderGuid = BitmapEncoder.BmpEncoderId;
                    break;

                case FileFormat.Tiff:
                    FileName += ".tiff";
                    BitmapEncoderGuid = BitmapEncoder.TiffEncoderId;
                    break;

                case FileFormat.Gif:
                    FileName += ".gif";
                    BitmapEncoderGuid = BitmapEncoder.GifEncoderId;
                    break;
            }

            var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoderGuid, stream);
                Stream pixelStream = WB.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                    (uint)WB.PixelWidth,
                                    (uint)WB.PixelHeight,
                                    96.0,
                                    96.0,
                                    pixels);
                await encoder.FlushAsync();
            }
            return file;
        }

        private void ChangeWidgetState()
        {
            button_start.IsEnabled = !taskRegistered;
            button_setting.IsEnabled = !taskRegistered;
            button_stop.IsEnabled = taskRegistered;
        }

        private void button_stop_Click(object sender, RoutedEventArgs e)
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    task.Value.Unregister(true);
                }
            }
            taskRegistered = false;
            ChangeWidgetState();
            //cancelToken.Cancel();
        }



        private async Task DownloadBingImage()
        {
            download_status.Text = "";
            var requestUrl = "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=7";
            var baseUrl = "https://www.bing.com";
            using (var client = new HttpClient())
            {
                try
                {
                    var json = await client.GetStringAsync(requestUrl);
                    var imageBean = JsonConvert.DeserializeObject<ImageBean>(json);
                    var filename = "File" + DateTime.Now.Ticks.ToString() + ".jpg";
                    var source = new Uri(baseUrl + imageBean.Images[0].Url);
                    StorageFile destination = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename);
                    WebClient webClient = new WebClient();
                    await webClient.DownloadFileTaskAsync(source, destination.Path);
                    if (UserProfilePersonalizationSettings.IsSupported())
                    {
                        UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                        var success = await profileSettings.TrySetWallpaperImageAsync(destination);
                        download_status.Text = success ? "success" : "failed";
                    }
                }
                catch (Exception e)
                {
                    download_status.Text = e.Message;
                    Trace.WriteLine(e);
                }
            }
        }

        private void button_setting_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingPage));
        }
    }
}
