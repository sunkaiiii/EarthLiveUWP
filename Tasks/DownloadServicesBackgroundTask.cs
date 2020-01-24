using DownloadServices;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace Tasks
{
    public sealed class DownloadServicesBackgroundTask:IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        private CancellationTokenSource _source;

        public async void Run(IBackgroundTaskInstance taskInstance)
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
            _deferral = taskInstance.GetDeferral();
            _source = new CancellationTokenSource();
            await new DownloaderHimawari8().UpdateImage(_source);
            _deferral.Complete();
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _source.Cancel();
        }
    }
}
