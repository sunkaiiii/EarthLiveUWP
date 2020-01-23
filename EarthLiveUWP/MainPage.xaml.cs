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
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
        private BackgroundTaskRegistration task;
        private bool taskRegistered = false;
        private readonly string taskName = "BackGroundDownloadService";
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
                var builder = new BackgroundTaskBuilder();
                builder.Name = taskName;
                builder.TaskEntryPoint = "EarthLiveUWP.Services";
                builder.SetTrigger(new TimeTrigger(15, false));
                task = builder.Register();
                taskRegistered = true;
            }
            ChangeWidgetState();
            //cancelToken = new CancellationTokenSource();
            //await new DownloaderHimawari8().UpdateImage(cancelToken, imageView);
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
