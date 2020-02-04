using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Tools
{
    public sealed class Config
    {
        public string Version { get; private set; }
        public Staellites Satellite { get; private set; }
        public string ImageFolder { get; private set; }
        public int Interval { get; private set; }
        public bool SetwallPaper { get; private set; }
        public uint Size { get; private set; }
        public int Zoom { get; private set; }
        public string CloudName { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public SourceSelections SourceSelection { get; set; }
        public bool SaveTexture { get; set; }
        public string SaveDirectory { get; set; }
        public int SaveMaxCount { get; set; }
        private ApplicationDataContainer LocalSettings { get; }
        private static readonly Lazy<Config> lazyInstance = new Lazy<Config>(() => new Config());
        public static Config Instance { get { return lazyInstance.Value; } }
        public enum Staellites
        {
            Himawari8,
            FengYun4
        }
        public enum SourceSelections
        {
            Orgin,
            CDN
        }
        private Config()
        {
            LocalSettings = ApplicationData.Current.LocalSettings;
            Load();
        }
        public void Load()
        {
            Func<PropertyInfo, object> getvalueAction = (property) => LocalSettings.Values[property.Name.ToString()];
            Action<PropertyInfo, object> propertyAction = (property, value) => property.SetValue(this, value);
            Func<PropertyInfo, object, object> enumAction = (property, value) => Enum.Parse(property.PropertyType, value.ToString());
            WalkThourghAllProperties(getvalueAction, propertyAction, enumAction);
        }
        public void Save()
        {
            Func<PropertyInfo, object> getvalueAction = (property) => property.GetValue(this);
            Action<PropertyInfo, object> propertyAction = (property, value) => LocalSettings.Values[property.Name.ToString()] = value;
            Func<PropertyInfo, object, object> enumAction = (property, value) => value.ToString();
            WalkThourghAllProperties(getvalueAction, propertyAction, enumAction);
        }

        private void WalkThourghAllProperties(Func<PropertyInfo, object> getvalueAction, Action<PropertyInfo, object> propertyAction, Func<PropertyInfo, object, object> enumAction)
        {
            foreach (var property in GetAllNonStaticPublicProperties())
            {
                var value = getvalueAction(property);
                if (value != null)
                {
                    if (property.PropertyType.IsEnum && enumAction != null)
                    {
                        value = enumAction(property, value);
                    }
                    propertyAction(property, value);
                }
            }
        }

        //跳过静态变量
        private PropertyInfo[] GetAllNonStaticPublicProperties() => this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        public bool IsFirstStart() => !(ApplicationData.Current.LocalSettings.Values.ContainsKey("isFirstLaunch"));

        public void SetStarted() => LocalSettings.Values["isFirstLaunch"] = false;

        public void InitDefaultValues()
        {
            Version = GetAppVersion();
            Satellite = Staellites.Himawari8;
            ImageFolder = "";
            Interval = 30;
            SetwallPaper = false;
            Size = ScreenHelper.GetDefaultSize();
            Zoom = 100;
            CloudName = "";
            ApiKey = "";
            ApiSecret = "";
            SourceSelection = SourceSelections.Orgin;
            SaveTexture = false;
            SaveDirectory = "";
            SaveMaxCount = -1;
            Save();
        }

        private string GetAppVersion()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
    }
}
