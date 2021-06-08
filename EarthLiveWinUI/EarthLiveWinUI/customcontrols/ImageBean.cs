using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLiveWinUI.customcontrols
{
    class ImageBean
    {
        public List<ImageInfo> Images { get; set; }

        public class ImageInfo
        {
            public string Url { get; set; }
        }
    }
}
