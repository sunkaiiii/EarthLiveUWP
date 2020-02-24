using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools;
using Windows.ApplicationModel.Background;

namespace Tasks
{
    public  class BackGroundTaskHelper
    {
        private readonly string taskName = "BackGroundDownloadService";
        private readonly string taskEntryPoint = "Tasks.DownloadServicesBackgroundTask";
        private bool taskRegistered = false;
        private static readonly Lazy<BackGroundTaskHelper> lazyInstance = new Lazy<BackGroundTaskHelper>(() => new BackGroundTaskHelper());
        public static BackGroundTaskHelper Instance { get { return lazyInstance.Value; } }
        public bool IsTaskRunning { get { return taskRegistered; } }

        private BackGroundTaskHelper()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    taskRegistered = true;
                }
            }
        }
        public void RegistBackGroundTask()
        {
            var trigger = new TimeTrigger(Config.Instance.Interval, false);
            var builder = new BackgroundTaskBuilder();
            builder.Name = taskName;
            builder.TaskEntryPoint = taskEntryPoint;
            builder.SetTrigger(trigger);
            var task = builder.Register();
            taskRegistered = true;
        }

        public void UnregistBackgroundTask()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == taskName)
                {
                    task.Value.Unregister(true);
                }
            }
            taskRegistered = false;
        }

      
    }
}
