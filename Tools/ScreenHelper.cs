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

            public uint GetWidthBlackArea(uint imageCount)
            {
                return CalculateArea(this.Width, imageCount);
            }

            public uint GetHeightBlackArea(uint imageCount)
            {
                return CalculateArea(this.Height, imageCount);
            }

            private uint CalculateArea(uint pixel, uint imageCount)
            {
                return (pixel -  550 * imageCount) / 2;
            }
        }
        public static ScreenResolution GetScreenResolution()
        {
            var displayInformation = DisplayInformation.GetForCurrentView();
            return new ScreenResolution(displayInformation.ScreenWidthInRawPixels, displayInformation.ScreenHeightInRawPixels);
        }


    }
}
