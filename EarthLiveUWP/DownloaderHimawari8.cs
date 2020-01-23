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
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.UserProfile;
using Windows.UI.Xaml.Media.Imaging;

namespace EarthLiveUWP
{
    class DownloaderHimawari8
    {
        private string imageID = "";
        private static string last_imageID = "0";
        private int ImageCount { get; set; }
        private string json_url = "http://himawari8.nict.go.jp/img/D531106/latest.json";
        public DownloaderHimawari8()
        {
            ImageCount = 0;
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
            if (Config.source_selection == 1)
            {
                image_source = "http://res.cloudinary.com/" + Config.cloud_name + "/image/fetch/http://himawari8-dl.nict.go.jp/himawari8/img/D531106";
            }
            else
            {
                image_source = "http://himawari8-dl.nict.go.jp/himawari8/img/D531106";
            }
            try
            {
                for (int ii = 0; ii < Config.size; ii++)
                {
                    for (int jj = 0; jj < Config.size; jj++)
                    {
                        string url = string.Format("{0}/{1}d/550/{2}_{3}_{4}.png", image_source, Config.size, imageID, ii, jj);
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
                Trace.WriteLine(string.Format("[image_folder]{0} [image_source]{1} [size]{2}", Config.image_folder, image_source, Config.size));
                return -1;
            }
        }

        private async Task<WriteableBitmap> JoinImageAsync()
        {
            var writeablebmp = new WriteableBitmap(550*Config.size,550*Config.size);
            for (int ii = 0; ii < Config.size; ii++)
            {
                for (int jj = 0; jj < Config.size; jj++)
                {
                    var file = await ApplicationData.Current.LocalFolder.GetFileAsync(string.Format("{0}_{1}.png", ii, jj));
                    var image = await BitmapFactory.FromStream(new FileStream(file.Path, FileMode.Open));
                    writeablebmp.Blit(new Windows.Foundation.Rect(550*ii, 550*jj, 550,550), image, new Windows.Foundation.Rect(0, 0, image.PixelWidth, image.PixelHeight));
                }
            }
            return writeablebmp;
            //// join & convert the images to wallpaper.bmp
            //Bitmap bitmap = new Bitmap(550 * Config.size, 550 * Config.size);
            //Image[,] tile = new Image[Config.size, Config.size];
            //Graphics g = Graphics.FromImage(bitmap);
            //for (int ii = 0; ii < Config.size; ii++)
            //{
            //    for (int jj = 0; jj < Config.size; jj++)
            //    {
            //        var file = await ApplicationData.Current.LocalFolder.GetFileAsync(string.Format("{0}_{1}.png", ii, jj));
            //        tile[ii, jj] = Image.FromFile(file.Path);
            //        g.DrawImage(tile[ii, jj], 550 * ii, 550 * jj);
            //        tile[ii, jj].Dispose();
            //    }
            //}
            //g.Save();
            //g.Dispose();
            //var wallpaper = await ApplicationData.Current.LocalFolder.CreateFileAsync("wallpaper.bmp");
            //if (Config.zoom == 100)
            //{
            //    bitmap.Save(wallpaper.Path, System.Drawing.Imaging.ImageFormat.Bmp);
            //}
            //else if (1 < Config.zoom & Config.zoom < 100)
            //{
            //    int new_size = bitmap.Height * Config.zoom / 100;
            //    Bitmap zoom_bitmap = new Bitmap(new_size, new_size);
            //    Graphics g_2 = Graphics.FromImage(zoom_bitmap);
            //    g_2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            //    g_2.DrawImage(bitmap, 0, 0, new_size, new_size);
            //    g_2.Save();
            //    g_2.Dispose();
            //    zoom_bitmap.Save(wallpaper.Path, System.Drawing.Imaging.ImageFormat.Bmp);
            //    zoom_bitmap.Dispose();
            //}
            //else
            //{
            //    Trace.WriteLine("[himawari8 zoom error]");
            //}

            //bitmap.Dispose();

            //if (Config.saveTexture && Config.saveDirectory != "selected Directory")
            //{
            //    if (ImageCount >= Config.saveMaxCount)
            //    {
            //        ImageCount = 0;
            //    }
            //    try
            //    {
            //        File.Copy(string.Format("{0}\\wallpaper.bmp", Config.image_folder), Config.saveDirectory + "\\" + "wallpaper_" + ImageCount + ".bmp", true);
            //        ImageCount++;
            //    }
            //    catch (Exception e)
            //    {
            //        Trace.WriteLine("[can't save wallpaper to distDirectory]");
            //        Trace.WriteLine(e.Message);
            //        return;
            //    }
            //}
        }

        private void InitFolder()
        {
            //if (Directory.Exists(Config.image_folder))
            //{
            //    // delete all images in the image folder.
            //    //string[] files = Directory.GetFiles(image_folder);
            //    //foreach (string fn in files)
            //    //{
            //    //    File.Delete(fn);
            //    //}
            //}
            //else
            //{
            //    Trace.WriteLine("[himawari8 create folder]");
            //    Directory.CreateDirectory(Config.image_folder);
            //}
        }
        public async Task UpdateImage(CancellationTokenSource _source, Windows.UI.Xaml.Controls.Image imageView=null)
        {
            InitFolder();
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
                var bitmap = await JoinImageAsync();
                if(imageView!=null)
                {
                    imageView.Source = bitmap;
                }
                var storageFile = await WriteableBitmapToStorageFile(bitmap, FileFormat.Jpeg);
                if (UserProfilePersonalizationSettings.IsSupported())
                {
                    UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                    var success = await profileSettings.TrySetWallpaperImageAsync(storageFile);
                    //download_status.Text = success ? "success" : "failed";
                }
            }
            last_imageID = imageID;
        }
        private async Task<StorageFile> WriteableBitmapToStorageFile(WriteableBitmap WB, FileFormat fileFormat)
        {
            string FileName = "wallpaper";
            Guid BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            switch (fileFormat)
            {
                case FileFormat.Jpeg:
                    FileName += ".jpeg";
                    BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                    break;

                case FileFormat.Png:
                    FileName += ".png";
                    BitmapEncoderGuid = BitmapEncoder.PngEncoderId;
                    break;

                case FileFormat.Bmp:
                    FileName += ".bmp";
                    BitmapEncoderGuid = BitmapEncoder.BmpEncoderId;
                    break;

                case FileFormat.Tiff:
                    FileName += ".tiff";
                    BitmapEncoderGuid = BitmapEncoder.TiffEncoderId;
                    break;

                case FileFormat.Gif:
                    FileName += ".gif";
                    BitmapEncoderGuid = BitmapEncoder.GifEncoderId;
                    break;
            }

            var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoderGuid, stream);
                Stream pixelStream = WB.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                    (uint)WB.PixelWidth,
                                    (uint)WB.PixelHeight,
                                    96.0,
                                    96.0,
                                    pixels);
                await encoder.FlushAsync();
            }
            return file;
        }

        private enum FileFormat
        {
            Jpeg,
            Png,
            Bmp,
            Tiff,
            Gif
        }
        public void CleanCDN()
        {
            Config.Load();
            if (Config.api_key.Length == 0) return;
            if (Config.api_secret.Length == 0) return;
            try
            {
                HttpWebRequest request = WebRequest.Create("https://api.cloudinary.com/v1_1/" + Config.cloud_name + "/resources/image/fetch?prefix=http://himawari8-dl") as HttpWebRequest;
                request.Method = "DELETE";
                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                string svcCredentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(Config.api_key + ":" + Config.api_secret));
                request.Headers.Add("Authorization", "Basic " + svcCredentials);
                HttpWebResponse response = null;
                StreamReader reader = null;
                string result = null;
                for (int i = 0; i < 3; i++) // max 3 request each hour.
                {
                    response = request.GetResponse() as HttpWebResponse;
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception("[himawari8 clean CND cache connection error]");
                    }
                    if (!response.ContentType.Contains("application/json"))
                    {
                        throw new Exception("[himawari8 clean CND cache no json recieved. your Internet connection is hijacked]");
                    }
                    reader = new StreamReader(response.GetResponseStream());
                    result = reader.ReadToEnd();
                    if (result.Contains("\"error\""))
                    {
                        throw new Exception("[himawari8 clean CND cache request error]\n" + result);
                    }
                    if (result.Contains("\"partial\":false"))
                    {
                        Trace.WriteLine("[himawari8 clean CDN cache done]");
                        break; // end of Clean CDN
                    }
                    else
                    {
                        Trace.WriteLine("[himawari8 more images to delete]");
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("[himawari8 error when delete CDN cache]");
                Trace.WriteLine(e.Message);
                return;
            }
        }
        public void ResetState()
        {
            last_imageID = "0";
        }
    }
}
