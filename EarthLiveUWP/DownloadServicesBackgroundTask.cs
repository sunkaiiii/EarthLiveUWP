using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace EarthLiveUWP
{
    class DownloadServicesBackgroundTask:IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        private CancellationTokenSource _source;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text="Download has been started"
                        },
                        new AdaptiveText()
                        {
                            Text=""
                        }
                    }
                }
            };
            var toastContent = new ToastContent()
            {
                Visual = visual
            };
            var toast = new ToastNotification(toastContent.GetXml());
            ToastNotificationManager.CreateToastNotifier().Show(toast);
            taskInstance.Canceled += OnCanceled;
            _source = new CancellationTokenSource();
            new DownloaderHimawari8().UpdateImage(_source);
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _source.Cancel();
        }
    }
}
