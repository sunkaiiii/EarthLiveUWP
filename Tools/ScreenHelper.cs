using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;

namespace Tools
{
    public static class ScreenHelper
    {
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
                int result = (Convert.ToInt32(pixel) - Convert.ToInt32(imagePixel))/2; //in case the uint overflow
                return result>=0?Convert.ToUInt32(result):0;
            }
        }
        public static ScreenResolution GetScreenResolution()
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            return new ScreenResolution(displayInformation.ScreenWidthInRawPixels, displayInformation.ScreenHeightInRawPixels);
        }
        public static uint GetDefaultSize()
        {
            var screenResolution = GetScreenResolution();
            double widthSize =  Math.Round(Math.Sqrt(screenResolution.Width/550.0));
            double heightSize = Math.Round(Math.Sqrt(screenResolution.Height / 550.0));
            return Convert.ToUInt32(Math.Pow(2, Math.Min(widthSize, heightSize)));
        }

    }
}
