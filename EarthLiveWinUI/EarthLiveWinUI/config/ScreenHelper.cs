using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;

namespace EarthLiveWinUI.config
{
    class ScreenHelper
    {
        private static ScreenResolution  cacheResolution = new ScreenResolution(1366, 768);
        public struct ScreenResolution
        {
            public uint Width { get; }
            public uint Height { get; }

            public ScreenResolution(double width, double height)
            {
                this.Width = Convert.ToUInt32(width);
                this.Height = Convert.ToUInt32(height);
            }

            public ScreenResolution(uint width, uint height)
            {
                this.Width = width;
                this.Height = height;
            }

            public uint GetWidthBlackArea(uint imageWidth)
            {
                return CalculateArea(this.Width, imageWidth);
            }

            public uint GetHeightBlackArea(uint imageHeight)
            {
                return CalculateArea(this.Height, imageHeight);
            }

            private uint CalculateArea(uint pixel, uint imagePixel)
            {
                int result = (Convert.ToInt32(pixel) - Convert.ToInt32(imagePixel)) / 2; //in case the uint overflow
                return result >= 0 ? Convert.ToUInt32(result) : 0;
            }
        }
        public static ScreenResolution GetScreenResolution(Microsoft.UI.Xaml.Window window)
        {
            //var displayInformation = DisplayInformation.GetForCurrentView();
            cacheResolution = new ScreenResolution(window.Bounds.Width,window.Bounds.Height);
            return cacheResolution;
        }

        public static ScreenResolution GetScreenResolution()
        {
            return cacheResolution;
        }
        public static uint GetDefaultSize(Microsoft.UI.Xaml.Window window)
        {
            var screenResolution = GetScreenResolution(window);
            double widthSize = Math.Round(Math.Sqrt(screenResolution.Width / 550.0));
            double heightSize = Math.Round(Math.Sqrt(screenResolution.Height / 550.0));
            return Convert.ToUInt32(Math.Pow(2, Math.Min(widthSize, heightSize)));
        }
    }
}
