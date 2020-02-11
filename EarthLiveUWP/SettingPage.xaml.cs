using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Tools;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace EarthLiveUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        private Config config = Config.Instance;
        DispatcherTimer dispatcherTimer;
        public SettingPage()
        {
            this.InitializeComponent();
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            InitUI();
            UpdateInterval.SelectedTimeChanged += UpdateInterval_SelectedTimeChanged; //init delegate before the initialsation of UIs;
            OriginRadioButton.Checked += CDNRadioButton_Checked;
            CDNRadioButton.Checked += CDNRadioButton_Checked;
            CloudName.TextChanged += CloudName_TextChanged;
        }

        private void InitUI()
        {
            ZoomSlider.Value = config.Zoom;
            UpdateInterval.SelectedTime= TimeSpan.FromMinutes(config.Interval);
            SetWallPaperCheckBox.IsChecked = config.SetwallPaper;
            SaveImageCheckBox.IsChecked = !String.IsNullOrEmpty(config.SaveDirectory);
            OriginRadioButton.IsChecked = config.IsOriginSource();
            CDNRadioButton.IsChecked = config.IsCDNSource();
            CDNStackPanel.Visibility = config.IsCDNSource()? Visibility.Visible:Visibility.Collapsed;
            CloudName.Text = config.CloudName;
        }
        private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if(dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
            }
            if (config.Zoom == ZoomSlider.Value)
                return;
            dispatcherTimer.Start();
        }

        void dispatcherTimer_Tick(object sender, object e)
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
    }
}
