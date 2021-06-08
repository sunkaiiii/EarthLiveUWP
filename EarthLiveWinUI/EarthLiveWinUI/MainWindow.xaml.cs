using CommunityToolkit.WinUI.UI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using EarthLiveWinUI.config;
using EarthLiveWinUI.service;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EarthLiveWinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private CancellationTokenSource cancelToken = new System.Threading.CancellationTokenSource();
        private Config config = Config.Instance;
        DispatcherTimer dispatcherTimer;
        private bool downloadComplete = true;
        NumberFormatInfo percentProvider;

        public MainWindow()
        {
            this.InitializeComponent();
            if (Config.Instance.IsFirstStart())
            {
                Config.Instance.InitDefaultValues(this);
            }
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            percentProvider = new NumberFormatInfo();
            percentProvider.PercentDecimalDigits = 2;
            percentProvider.CurrencyPositivePattern = 1;
            InitUI();
            UpdateInterval.SelectedTimeChanged += UpdateInterval_SelectedTimeChanged; //init delegate before the initialsation of UIs;
            OriginRadioButton.Checked += CDNRadioButton_Checked;
            CDNRadioButton.Checked += CDNRadioButton_Checked;
            CloudName.TextChanged += CloudName_TextChanged;
            ChangeWidgetState();
            this.DispatcherQueue.TryEnqueue(async () => await LoadPreviousImage()); //load the cache image
            this.DispatcherQueue.TryEnqueue(async () => await GetEarthPicture()); //load the current image
        }

        private async Task LoadPreviousImage()
        {
            var previouseImageID = Config.Instance.LastImageID;
            if (!String.IsNullOrEmpty(previouseImageID))
            {
                var file = await new DownloaderHimawari8().GetSavedPureImageByIDAsync(previouseImageID);
                if (file != null)
                {
                    await SetImageViewAsync(file);
                }
            }
        }

        private async Task GetEarthPicture()
        {
            var cancelationToken = new CancellationTokenSource();
            var file = await new DownloaderHimawari8().GetLiveEarthPictureForShowing(cancelationToken, (current, all) =>
            {
                var result = Convert.ToDouble(current) / all;
                LoadingProgressText.Text = result.ToString("P", percentProvider);
            });
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
                await ChangeImageScaleAsync((float)ZoomSlider.Value / 100);
            }
        }

        private async void button_start_Click(object sender, RoutedEventArgs e)
        {
            //if (!taskHelper.IsTaskRunning)
            //{
                await StartProcess();
            //}
            //else
            //{
            //    StopProcess();
            //}
        }

        private void ChangeWidgetState()
        {
            //button_start.Content = taskHelper.IsTaskRunning ? "Stop" : "Start";
            button_start.Visibility = downloadComplete ? Visibility.Visible : Visibility.Collapsed;
            loadingProcessRing.Visibility = button_start.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private async Task StartProcess()
        {
            //if (!taskHelper.IsTaskRunning)
            //{
                //taskHelper.RegistBackGroundTask();
                cancelToken = new CancellationTokenSource();
                Task downloadImage = new DownloaderHimawari8().UpdateImage(cancelToken);
                downloadComplete = false;
                ChangeWidgetState();
                await downloadImage;
                downloadComplete = true;
                ChangeWidgetState();
            //}
        }

        private void StopProcess()
        {
            //taskHelper.UnregistBackgroundTask();
            ChangeWidgetState();
            if (cancelToken != null && !cancelToken.IsCancellationRequested)
            {
                cancelToken.Cancel();
                cancelToken = null;
            }
        }

        private void InitUI()
        {
            ZoomSlider.Value = config.Zoom;
            UpdateInterval.SelectedTime = TimeSpan.FromMinutes(config.Interval);
            SetWallPaperCheckBox.IsChecked = config.SetwallPaper;
            SaveImageCheckBox.IsChecked = config.IsSavePicture;
            OriginRadioButton.IsChecked = config.IsOriginSource();
            CDNRadioButton.IsChecked = config.IsCDNSource();
            CDNStackPanel.Visibility = config.IsCDNSource() ? Visibility.Visible : Visibility.Collapsed;
            CloudName.Text = config.CloudName;
            LoadingProgressText.Text = 0.0.ToString("P", percentProvider);
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
            PannelBackground.Scale = new Vector3(scale);
            //await PannelBackground.Scale(scaleX: scale, scaleY: scale, duration: 400, centerX: centerXY.centerX, centerY: centerXY.centerY).StartAsync();
        }

        private void ChangeImageScale(float scale)
        {
            (float centerX, float centerY) centerXY = GetCenterXY();
            PannelBackground.Scale = new Vector3(scale);
            //PannelBackground.Scale(scaleX: scale, scaleY: scale, duration: 400, centerX: centerXY.centerX, centerY: centerXY.centerY).Start();
        }

        private (float, float) GetCenterXY()
        {
            float centerX = (float)PannelBackground.ActualWidth / 2;
            float centerY = (float)PannelBackground.ActualHeight / 2;
            return (centerX, centerY);
        }

        private void SaveImageCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            config.SetSaveImage();
        }

        private void SaveImageCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            config.CancelSaveImage();
        }

    }
}
