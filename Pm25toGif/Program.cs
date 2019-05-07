using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ImageMagick;
using Newtonsoft.Json;

namespace Pm25toGif
{
    class Program
    {
        private static readonly HttpClient Client = new HttpClient();
        private const int PicNum = 25;
        private const int GifFrame = 90;
        private static string _accessToken = "";//"e1dc65ceacd1ea3a27b84a06c66cc53f20089997";
        private static string _refreshToken = "";
        private const string ClientId = "0e539704c0790ec";
        private const string ClientSecert = "23a6aaf46a001c369446782089a4d5c7816eb586";

        static void Main(string[] args)
        {
            //var str = GetBase64FromFile("input.xml");
            //Console.WriteLine(str);
            //Console.ReadLine();
            //return;

            var date = DateTime.Now.ToString("yyMMdd");
            CheckFloder(date);
            //DownloadPng(date);
            //MakeGif(date);
            MakeGifWithoutDownload(date);
            //if (string.IsNullOrEmpty(_accessToken)) GetImgurAuthToken();

            //if (_accessToken.Length > 0)
            if (ClientId.Length>0)
            {
                Console.WriteLine("anonymous uploading image");
                var imgUrl = UploadToImgur(date, $"{date}\\{date}.gif", "gif");
                Console.WriteLine($"Image url: {imgUrl}");
            }
            else
            {
                //Console.WriteLine("No access token");
                Console.WriteLine("No ClientId");
            }

            Console.ReadLine();
        }


        #region Imgur
        static bool GetImgurAuthToken()
        {
            var requestUrl = $"https://api.imgur.com/oauth2/authorize?client_id={ClientId}&response_type=token";
            //try
            //{
            //    var request = WebRequest.Create(requestUrl);
            //    var response = request.GetResponse();
            //    var query = HttpUtility.ParseQueryString(response.ResponseUri.Query);
            //    _accessToken = query.Get("access_token");
            //    _refreshToken = query.Get("refresh_token");
            //    Console.WriteLine(_accessToken);
            //    return true;
            //}
            //catch
            //{
            //    return false;
            //}

            var response = Client.GetAsync(requestUrl).Result;
            var location = response.Headers.Location;
            var query = HttpUtility.ParseQueryString(location.ToString());
            _accessToken = query.Get("access_token");
            _refreshToken = query.Get("refresh_token");
            return true;
        }

        static string UploadToImgur(string title, string imgPath, string imgType)
        {
            var imgBase64 = ImageToBase64(imgPath, ImageFormat.Gif);
            if (imgBase64 == string.Empty) return string.Empty;

            var body = new
            {
                title,
                type = imgType,
                image = imgBase64
            };
            var jsonString = JsonConvert.SerializeObject(body);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            Client.BaseAddress = new Uri("https://api.imgur.com");
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", ClientId);
            var response = Client.PostAsync("/3/image", content).Result;
            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;
                //dynamic obj = responseContent;
                dynamic obj = JsonConvert.DeserializeObject(responseContent);
                return obj.data.link;
            }
            //fail
            Console.WriteLine(response.Content.ToString());
            Console.WriteLine($"Upload fail, status code:{response.StatusCode}");
            return string.Empty;

        }
        #endregion


        static void DownloadPng(string date)
        {
            using (WebClient webClient = new WebClient())
            {
                for (var i = 1; i <= PicNum; i++)
                {
                    var num = i.ToString("D2");
                    var url = $"https://pm25.jp/yosoku/parts/casu/{date}/{num}.png";
                    Console.WriteLine($"Downloading {url}");
                    webClient.DownloadFile(url, $"{date}\\{num}.png");
                    Thread.Sleep(300);
                }
            }
        }

        public static string GetBase64FromFile(string path)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(File.ReadAllText(path));
                return Convert.ToBase64String(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{MethodBase.GetCurrentMethod().ReflectedType}-{MethodBase.GetCurrentMethod().Name},msg:{ex.Message}");
                return string.Empty;
            }
        }

        public static string ImageToBase64(string path, ImageFormat format)
        {
            var image = Image.FromFile(path);
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to base 64 string
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }

        static void CheckFloder(string date)
        {
            Directory.CreateDirectory(date);
        }

        static void MakeGif(string date)
        {
            Console.WriteLine("Starting Make Gif...");
            using (MagickImageCollection collection = new MagickImageCollection())
            {
                for (int i = 1; i <= PicNum; i++)
                {
                    var num = i.ToString("D2");
                    collection.Add($"{date}\\{num}.png");
                    collection[i-1].AnimationDelay = GifFrame;
                }

                // Optionally optimize the images (images should have the same size).
                collection.Optimize();

                // Save gif
                collection.Write($"{date}\\{date}.gif");
                Console.WriteLine("Make Gif Done.");
            }
        }

        static void MakeGifWithoutDownload(string date)
        {
            Console.WriteLine("Starting Make Gif...");
            using (MagickImageCollection collection = new MagickImageCollection())
            {
                for (int i = 1; i <= PicNum; i++)
                {
                    var num = i.ToString("D2");
                    var url = $"https://pm25.jp/yosoku/parts/casu/{date}/{num}.png";
                    collection.Add(url);
                    collection[i - 1].AnimationDelay = GifFrame;
                }

                // Optionally optimize the images (images should have the same size).
                collection.Optimize();

                // Save gif
                collection.Write($"{date}\\{date}.gif");
                Console.WriteLine("Make Gif Done.");
            }
        }
    }
}
