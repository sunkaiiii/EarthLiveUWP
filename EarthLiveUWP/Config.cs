using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLiveUWP
{
    public static class Config
    {
        public static string version;
        public static string satellite;
        public static string image_folder;
        public static int interval;
        public static bool autostart;
        public static bool setwallpaper;
        public static int size=2;
        public static int zoom;
        public static string cloud_name;
        public static string api_key;
        public static string api_secret;
        public static int source_selection;
        public static bool saveTexture;
        public static string saveDirectory;
        public static int saveMaxCount;
        public static void Load()
        {

        }
        public static void Save()
        {
        }
    }
}
