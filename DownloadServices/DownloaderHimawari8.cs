using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tools;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;

namespace DownloadServices
{
    public class DownloaderHimawari8
    {
        private string imageID = "";
        private static string last_imageID = "0";
        private int ImageCount { get; set; }
        private string json_url = "http://himawari8.nict.go.jp/img/D531106/latest.json";
        private Config config;
        public DownloaderHimawari8()
        {
            ImageCount = 0;
            config = Config.Instance;
        }

        public async Task UpdateImage(CancellationTokenSource _source)
        {
            if (await GetImageID(_source) == -1)
            {
                return;
            }
            if (imageID.Equals(last_imageID))
            {
                return;
            }
            if (await SaveImage(_source) == 0)
            {
                var saveFile = await JoinImageAsync();
                if (UserProfilePersonalizationSettings.IsSupported())
                {
                    UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                    var success = await profileSettings.TrySetWallpaperImageAsync(saveFile);
                    //download_status.Text = success ? "success" : "failed";
                }
            }
            last_imageID = imageID;
        }


        private async Task<int> GetImageID(CancellationTokenSource _source)
        {
            HttpClient client = new HttpClient();
            try
            {
                var response = await client.GetAsync(json_url, _source.Token);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("[himawari8 connection error]");
                }
                if (!response.Content.Headers.ContentType.MediaType.Contains("application/json"))
                {
                    throw new Exception("[himawari8 no json recieved. your Internet connection is hijacked]");
                }
                string date = await response.Content.ReadAsStringAsync();
                imageID = date.Substring(9, 19).Replace("-", "/").Replace(" ", "/").Replace(":", "");
                Trace.WriteLine("[himawari8 get latest ImageID] " + imageID);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return -1;
            }
            return 0;
        }

        private async Task<int> SaveImage(CancellationTokenSource _source)
        {
            WebClient client = new WebClient();
            string image_source = "";
            _source.Token.Register(client.CancelAsync);
            if (config.SourceSelection == Config.SourceSelections.CDN)
            {
                image_source = "http://res.cloudinary.com/" + config.CloudName + "/image/fetch/http://himawari8-dl.nict.go.jp/himawari8/img/D531106";
            }
            else
            {
                image_source = "http://himawari8-dl.nict.go.jp/himawari8/img/D531106";
            }
            try
            {
                for (int ii = 0; ii < config.Size; ii++)
                {
                    for (int jj = 0; jj < config.Size; jj++)
                    {
                        string url = string.Format("{0}/{1}d/550/{2}_{3}_{4}.png", image_source,config.Size, imageID, ii, jj);
                        string image_name = string.Format("{0}_{1}.png", ii, jj); // remove the '/' in imageID
                        var destination = await ApplicationData.Current.LocalFolder.CreateFileAsync(image_name,CreationCollisionOption.ReplaceExisting);
                        await client.DownloadFileTaskAsync(url, destination.Path);
                    }
                }
                Trace.WriteLine("[save image] " + imageID);
                return 0;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message + " " + imageID);
                Trace.WriteLine(string.Format("[image_folder]{0} [image_source]{1} [size]{2}", config.ImageFolder, image_source,config.Size));
                return -1;
            }
        }

        private async Task<StorageFile> JoinImageAsync()
        {
            var saveFile= await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync("wallpaper.png", CreationCollisionOption.ReplaceExisting);
            using(var stream=await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var resolution = ScreenHelper.GetScreenResolution();
                byte[] canvans = new byte[resolution.Width* resolution.Height* 4];
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);
                for (int ii = 0; ii < config.Size; ii++)
                {
                    for (int jj = 0; jj < config.Size; jj++)
                    {
                        var file = await ApplicationData.Current.LocalFolder.GetFileAsync(string.Format("{0}_{1}.png", ii, jj));
                        using (IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read))
                        {
                            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(readStream);
                            var pixeldata = await decoder.GetPixelDataAsync();
                            var imagebyte = pixeldata.DetachPixelData();
                            uint widthLocation = Convert.ToUInt32(resolution.GetWidthBlackArea(config.Size) + 550 * ii);
                            uint heightLocation = Convert.ToUInt32(resolution.GetHeightBlackArea(config.Size) + 550 * jj);
                            canvans = PutOnCanvas(canvans, imagebyte, widthLocation, heightLocation, 550, 550, 3840);
                        }
                    }
                }
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, 3840, 2160, 96, 96, canvans);
                await encoder.FlushAsync();
            }
            return saveFile;
        }

        byte[] PutOnCanvas(byte[] Canvas, byte[] Image, uint x, uint y, uint imageheight, uint imagewidth, uint CanvasWidth)
        {
            for (uint row = y; row < y + imageheight; row++)
                for (uint col = x; col < x + imagewidth; col++)
                    for (uint i = 0; i < 4; i++)
                        Canvas[(row * CanvasWidth + col) * 4 + i] = Image[((row - y) * imagewidth + (col - x)) * 4 + i];

            return Canvas;
        }



        //public void CleanCDN()
        //{
        //    Config.Load();
        //    if (Config.api_key.Length == 0) return;
        //    if (Config.api_secret.Length == 0) return;
        //    try
        //    {
        //        HttpWebRequest request = WebRequest.Create("https://api.cloudinary.com/v1_1/" + Config.cloud_name + "/resources/image/fetch?prefix=http://himawari8-dl") as HttpWebRequest;
        //        request.Method = "DELETE";
        //        request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
        //        string svcCredentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(Config.api_key + ":" + Config.api_secret));
        //        request.Headers.Add("Authorization", "Basic " + svcCredentials);
        //        HttpWebResponse response = null;
        //        StreamReader reader = null;
        //        string result = null;
        //        for (int i = 0; i < 3; i++) // max 3 request each hour.
        //        {
        //            response = request.GetResponse() as HttpWebResponse;
        //            if (response.StatusCode != HttpStatusCode.OK)
        //            {
        //                throw new Exception("[himawari8 clean CND cache connection error]");
        //            }
        //            if (!response.ContentType.Contains("application/json"))
        //            {
        //                throw new Exception("[himawari8 clean CND cache no json recieved. your Internet connection is hijacked]");
        //            }
        //            reader = new StreamReader(response.GetResponseStream());
        //            result = reader.ReadToEnd();
        //            if (result.Contains("\"error\""))
        //            {
        //                throw new Exception("[himawari8 clean CND cache request error]\n" + result);
        //            }
        //            if (result.Contains("\"partial\":false"))
        //            {
        //                Trace.WriteLine("[himawari8 clean CDN cache done]");
        //                break; // end of Clean CDN
        //            }
        //            else
        //            {
        //                Trace.WriteLine("[himawari8 more images to delete]");
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Trace.WriteLine("[himawari8 error when delete CDN cache]");
        //        Trace.WriteLine(e.Message);
        //        return;
        //    }
        //}
        public void ResetState()
        {
            last_imageID = "0";
        }
    }
}
