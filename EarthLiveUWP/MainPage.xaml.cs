using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Download_start_Click(object sender, RoutedEventArgs e)
        {
            await DownloadBingImage();
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
    }
}
