using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLiveUWP
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
