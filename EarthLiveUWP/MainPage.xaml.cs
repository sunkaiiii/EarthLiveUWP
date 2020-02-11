using DownloadServices;
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
using Tools;
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

        private async void button_start_Click(object sender, RoutedEventArgs e)
        {
            if (!taskRegistered)
            {
                var trigger = new TimeTrigger(Config.Instance.Interval, false);
                var builder = new BackgroundTaskBuilder();
                builder.Name = taskName;
                builder.TaskEntryPoint = taskEntryPoint;
                builder.SetTrigger(trigger);
                var task = builder.Register();
                taskRegistered = true;
                Task downloadImage = new DownloaderHimawari8().UpdateImage(new CancellationTokenSource());
                ChangeWidgetState();
                await downloadImage;
            }
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



        private void button_setting_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingPage));
        }
    }
}
