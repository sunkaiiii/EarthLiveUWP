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
        private int ImageCount { get; set; }
        private readonly string json_url = "http://himawari8.nict.go.jp/img/D531106/latest.json";
        private readonly uint size;
        private readonly string imageSource;
        private readonly int zoom;
        private readonly int lastZoom;
        private readonly string savePictureFolderName = "EarthLiveUWP";
        public DownloaderHimawari8()
        {
            Config config = Config.Instance;
            ImageCount = 0;
            size = config.Size;
            if (config.SourceSelection == Config.SourceSelections.CDN)
            {
                imageSource = "http://res.cloudinary.com/" + config.CloudName + "/image/fetch/http://himawari8-dl.nict.go.jp/himawari8/img/D531106";
            }
            else
            {
                imageSource = "http://himawari8-dl.nict.go.jp/himawari8/img/D531106";
            }
            zoom = config.Zoom;
            lastZoom = config.LastZoom;
        }

        public async Task UpdateImage(CancellationTokenSource _source)
        {
            var saveFile = await GetLiveEarthPictureForWallPaper(_source);
            if (saveFile == null)
            {
                return;
            }
            if (UserProfilePersonalizationSettings.IsSupported())
            {
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                var success = await profileSettings.TrySetWallpaperImageAsync(saveFile);
                //download_status.Text = success ? "success" : "failed";
            }
        }
        public async Task<StorageFile> GetLiveEarthPictureForShowing(CancellationTokenSource source)
        {
            var imageID = await GetImageID(source);
            return await DownloadImageAsync(source, imageID);
        }
        public async Task<StorageFile> GetLiveEarthPictureForWallPaper(CancellationTokenSource source)
        {
            var imageID = await GetImageID(source);
            var savedFile = await GetSavedImageByIdAsync(imageID);
            if (savedFile != null)
                return savedFile;
            var pureFile = DownloadImageAsync(source, imageID);
            if (pureFile == null)
                return null;
            return await PutEarthIntoCavansAsync(pureFile, imageID);
        }

        private async Task<StorageFile> DownloadImageAsync(CancellationTokenSource source, string imageID)
        {
            if (imageID != null)
            {
                StorageFile savedImage = await GetSavedPureImageByIDAsync(imageID);
                if (savedImage != null)
                    return savedImage;
                if (await SaveImage(source, imageID) == 0)
                {
                    Config.Instance.SetLastImageID(imageID);
                    return await BlitEarthImageAsync(imageID);
                }
            }
            return null;
        }

        public async Task<StorageFile> GetSavedImageByIdAsync(string imageid)
        {
            if (zoom != lastZoom)
                return null;
            try
            {
                return await ApplicationData.Current.LocalFolder.GetFileAsync(imageid.Replace("/", "_") + ".png");
            }
            catch (FileNotFoundException e)
            {
                return null;
            }
        }
        public async Task<StorageFile> GetSavedPureImageByIDAsync(string imageID)
        {
            try
            {
                return await ApplicationData.Current.LocalFolder.GetFileAsync(imageID.Replace("/", "_") + "_pure.png");
            }
            catch (FileNotFoundException e)
            {
                return null;
            }
        }

        private async Task<string> GetImageID(CancellationTokenSource _source)
        {
            HttpClient client = new HttpClient();
            string imageID = null;
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
            }
            return imageID;
        }

        private async Task<int> SaveImage(CancellationTokenSource _source, string imageID)
        {
            WebClient client = new WebClient();
            _source.Token.Register(client.CancelAsync);
            try
            {
                for (int ii = 0; ii < size; ii++)
                {
                    for (int jj = 0; jj < size; jj++)
                    {
                        string url = string.Format("{0}/{1}d/550/{2}_{3}_{4}.png", imageSource, size, imageID, ii, jj);
                        string image_name = string.Format("{0}_{1}.png", ii, jj); // remove the '/' in imageID
                        var destination = await ApplicationData.Current.LocalFolder.CreateFileAsync(image_name, CreationCollisionOption.ReplaceExisting);
                        await client.DownloadFileTaskAsync(url, destination.Path);
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                return -1;
            }
        }

        private async Task<StorageFile> PutEarthIntoCavansAsync(Task<StorageFile> earthPath, string imageId)
        {
            var saveFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(imageId.Replace("/", "_") + ".png", CreationCollisionOption.ReplaceExisting);
            using (var stream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var resolution = ScreenHelper.GetScreenResolution();
                byte[] canvans = new byte[resolution.Width * resolution.Height * 4];
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                var scaledEarth = await ApplicationData.Current.LocalFolder.CreateFileAsync(imageId.Replace("/", "_") + "_scaled.png",CreationCollisionOption.ReplaceExisting);
                using(IRandomAccessStream originalEarthStream = await (await earthPath).OpenAsync(FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(originalEarthStream);
                    using(var scaledImageStream=await scaledEarth.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var scaledEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, scaledImageStream);
                        if (zoom < 100)
                        {
                            scaledEncoder.BitmapTransform.ScaledWidth = Convert.ToUInt32(decoder.PixelWidth * (zoom / 100.0));
                            scaledEncoder.BitmapTransform.ScaledHeight = Convert.ToUInt32(decoder.PixelHeight * (zoom / 100.0));
                        }
                        var imageByte = (await decoder.GetPixelDataAsync()).DetachPixelData();
                        scaledEncoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, decoder.PixelWidth, decoder.PixelHeight, 96, 96, imageByte);
                        await scaledEncoder.FlushAsync();
                    }
                }
                using (IRandomAccessStream readStream = await scaledEarth.OpenAsync(FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(readStream);
                    var pixeldata = await decoder.GetPixelDataAsync();
                    var imagebyte = pixeldata.DetachPixelData();
                    uint widthLocation = resolution.GetWidthBlackArea(decoder.PixelWidth);
                    uint heightLocation = resolution.GetHeightBlackArea(decoder.PixelHeight);
                    canvans = PutOnCanvas(canvans, imagebyte, widthLocation, heightLocation, decoder.PixelHeight, decoder.PixelWidth, resolution.Width);
                }
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, resolution.Width, resolution.Height, 96, 96, canvans);
                await encoder.FlushAsync();
            }
            Config.Instance.SetLastZoom(zoom);
            if(Config.Instance.IsSavePicture)
            {
                await SaveImageToImageFolder(saveFile);
            }
            return saveFile;
        }

        private async Task SaveImageToImageFolder(StorageFile saveFile)
        {
            try
            {
                StorageFolder folder = null;
                try
                {
                    folder = await KnownFolders.PicturesLibrary.GetFolderAsync(savePictureFolderName);
                }catch(Exception e)
                {
                    Trace.WriteLine(e);
                    await KnownFolders.PicturesLibrary.CreateFolderAsync(savePictureFolderName);
                    folder = await KnownFolders.PicturesLibrary.GetFolderAsync(savePictureFolderName);
                }
                await saveFile.CopyAsync(folder);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }

        private async Task<StorageFile> BlitEarthImageAsync(string imageID)
        {
            string savedName = imageID.Replace("/", "_") + "_pure.png";
            var earthFile = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(savedName, CreationCollisionOption.ReplaceExisting);
            using (var stream = await earthFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                uint width = size * 550;
                uint height = size * 550;
                byte[] canvans = new byte[width * height * 4];
                for (int ii = 0; ii < size; ii++)
                {
                    for (int jj = 0; jj < size; jj++)
                    {
                        var file = await ApplicationData.Current.LocalFolder.GetFileAsync(string.Format("{0}_{1}.png", ii, jj));
                        using (IRandomAccessStream readStream = await file.OpenAsync(FileAccessMode.Read))
                        {
                            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(readStream);
                            var pixeldata = await decoder.GetPixelDataAsync();
                            var imagebyte = pixeldata.DetachPixelData();
                            uint widthLocation = Convert.ToUInt32(550 * ii);
                            uint heightLocation = Convert.ToUInt32(550 * jj);
                            canvans = PutOnCanvas(canvans, imagebyte, widthLocation, heightLocation, 550, 550, width);
                        }
                    }
                }
                double scale = 1;
                var screenResolution = ScreenHelper.GetScreenResolution();
                scale = Math.Min(scale, Convert.ToDouble(screenResolution.Height) / height);
                scale = Math.Min(scale, Convert.ToDouble(screenResolution.Width) / width);
                if (scale < 1) //scale the image to fit the screen resolution
                {
                    encoder.BitmapTransform.ScaledWidth = Convert.ToUInt32(width * scale);
                    encoder.BitmapTransform.ScaledHeight = Convert.ToUInt32(height * scale);
                }
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, width, height, 96, 96, canvans);
                await encoder.FlushAsync();
            }
            return earthFile;
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
    }
}
