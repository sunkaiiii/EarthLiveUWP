using DownloadServices;
using Microsoft.Toolkit.Uwp.UI.Animations;
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
        private Config config = Config.Instance;
        DispatcherTimer dispatcherTimer;
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
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            InitUI();
            UpdateInterval.SelectedTimeChanged += UpdateInterval_SelectedTimeChanged; //init delegate before the initialsation of UIs;
            OriginRadioButton.Checked += CDNRadioButton_Checked;
            CDNRadioButton.Checked += CDNRadioButton_Checked;
            CloudName.TextChanged += CloudName_TextChanged;
            ChangeWidgetState();
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await LoadPreviousImage());//load the cache image
            var ignored2 = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => await GetEarthPicture()); //load the current image
        }

        private async Task LoadPreviousImage()
        {
            var previouseImageID = Config.Instance.LastImageID;
            if(!String.IsNullOrEmpty(previouseImageID))
            {
                var file = await new DownloaderHimawari8().GetSavedPureImageByIDAsync(previouseImageID);
                if(file!=null)
                {
                    await SetImageViewAsync(file);
                }
            }
        }

        private async Task GetEarthPicture()
        {
            var cancelationToken = new CancellationTokenSource();
            var file = await new DownloaderHimawari8().GetLiveEarthPictureForShowing(cancelationToken);
            if (file == null)
                return;
            await SetImageViewAsync(file);
        }

        private async Task SetImageViewAsync(StorageFile file)
        {
            using (var randomAccessStream = await file.OpenAsync(FileAccessMode.Read))
            {
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(randomAccessStream);
                PannelBackground.Source = bitmap;
                await Task.Delay(100); //wait for bitmap setting to scale picture
                await ChangeImageScaleAsync((float)ZoomSlider.Value/100);
            }
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

        private void InitUI()
        {
            ZoomSlider.Value = config.Zoom;
            UpdateInterval.SelectedTime = TimeSpan.FromMinutes(config.Interval);
            SetWallPaperCheckBox.IsChecked = config.SetwallPaper;
            SaveImageCheckBox.IsChecked = !String.IsNullOrEmpty(config.SaveDirectory);
            OriginRadioButton.IsChecked = config.IsOriginSource();
            CDNRadioButton.IsChecked = config.IsCDNSource();
            CDNStackPanel.Visibility = config.IsCDNSource() ? Visibility.Visible : Visibility.Collapsed;
            CloudName.Text = config.CloudName;
        }
        private async void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
            }
            if (config.Zoom == ZoomSlider.Value)
                return;
            dispatcherTimer.Start();
            float scale = (float)ZoomSlider.Value / 100;
            await ChangeImageScaleAsync(scale);
        }

        private void dispatcherTimer_Tick(object sender, object e)
        {
            dispatcherTimer.Stop();
            config.SetZoom(ZoomSlider.Value);
        }

        private void UpdateInterval_SelectedTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
        {
            config.SetInteval(sender.SelectedTime);
        }

        private void CDNRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            bool isCDNChecked = CDNRadioButton.IsChecked ?? false;
            CDNStackPanel.Visibility = isCDNChecked ? Visibility.Visible : Visibility.Collapsed;
            config.SetSourceSelection(isCDNChecked);
        }

        private void CloudName_TextChanged(object sender, TextChangedEventArgs e)
        {
            config.SetCloudName(((TextBox)sender).Text);
        }

        private async Task ChangeImageScaleAsync(float scale)
        {
            (float centerX, float centerY) centerXY = GetCenterXY();
            await PannelBackground.Scale(scaleX: scale, scaleY: scale, duration: 400, centerX: centerXY.centerX, centerY: centerXY.centerY).StartAsync();
        }

        private void ChangeImageScale(float scale)
        {
            (float centerX, float centerY) centerXY = GetCenterXY();
            PannelBackground.Scale(scaleX: scale, scaleY: scale, duration: 400, centerX: centerXY.centerX, centerY: centerXY.centerY).Start();
        }

        private (float,float) GetCenterXY()
        {
            float centerX = (float)PannelBackground.ActualWidth / 2;
            float centerY = (float)PannelBackground.ActualHeight / 2;
            return (centerX, centerY);
        }
    }
}
