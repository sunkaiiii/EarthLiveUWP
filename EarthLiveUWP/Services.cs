using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace EarthLiveUWP
{
    class Services:IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        private CancellationTokenSource _source;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnCanceled;
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
