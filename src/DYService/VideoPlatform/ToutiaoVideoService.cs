using DYService.Data;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DYService
{
    /// <summary>
    /// 头条视频解析服务
    /// </summary>
    public class ToutiaoVideoService : VideoParseService
    {
        private readonly IFlurlClientCache _flurlClient;

        public ToutiaoVideoService(IFlurlClientCache flurlClient,
            IWebShortUrlService webShortUrlService,
            IHttpContextAccessor httpContextAccessor) : base(webShortUrlService, httpContextAccessor)
        {
            this._flurlClient = flurlClient;
        }

        protected override bool CanParse(ParseVideoInput input)
        {
            return input.DeKey.Contains("toutiao.com");
        }

        protected override async Task<ParseResult<VideoInfoData>> Request(string url)
        {
            var data = new ParseResult<VideoInfoData>();
            var client = _flurlClient.Get("toutiao");

            using (var session = new CookieSession(client))
            {
                var res = await session.Request(url)
                    .WithAutoRedirect(true)
                    .GetAsync();

                var timestamp = Util.Helpers.Time.GetUnixTimestamp() * 1000;

                //获取ttwid
                var ttwidresp = await session.Request("https://ttwid.bytedance.com/ttwid/union/register/")
                    .WithHeader("Accept", "application/json, text/plain, */*")
                    .WithHeader("Content-Type", "application/x-www-form-urlencoded")
                    .WithHeader("Referer", "https://www.toutiao.com/")
                    .PostJsonAsync(new
                    {
                        aid = 24,
                        service = "www.toutiao.com",
                        region = "cn",
                        union = true,
                        needFid = false
                    });

                var ttwid = ttwidresp.Cookies.FirstOrDefault(x => x.Name == "ttwid");

                var reloadUrl = $"{res.ResponseMessage.RequestMessage.RequestUri.AbsoluteUri}";
                res = await session.Request($"{reloadUrl}&wid={timestamp}")
                    .WithHeader("Referer", reloadUrl)
                    .WithCookie("ttwid", ttwid.Value)
                    .GetAsync();

                var str = string.Empty;
                if (res.ResponseMessage.Content.Headers.ContentEncoding.Contains("br"))
                {
                    using var aw = await res.GetStreamAsync();
                    using var br = new BrotliStream(aw, CompressionMode.Decompress);
                    using var sr = new StreamReader(br);
                    str = sr.ReadToEnd();
                }
                else
                {
                    str = await res.GetStringAsync();
                }

                var encodeStr = Util.Helpers.Regex.GetValue(str, "(?<=<script id=\"RENDER_DATA\" type=\"application/json\">).*(?=</script></head>)");
                var jsonStr = HttpUtility.UrlDecode(encodeStr);

                var jsonData = System.Text.Json.JsonSerializer.Deserialize<toutiaoData>(jsonStr);
                data.Data = new VideoInfoData
                {
                    detail = new VideoDetail
                    {
                        desc = jsonData.desc,
                        url = jsonData.url
                    }
                };
            }

            return data;
        }

        public class toutiaoData
        {
            public data data { get; set; }

            public string desc => data.initialVideo.title;
            public string url
            {
                get
                {
                    var items = data.initialVideo.videoPlayInfo.video_list;
                    var sel = Math.Ceiling(items.Count / 2M);//取中间清晰度视频
                    var u = items[Convert.ToInt32(sel)].main_url;
                    return u.Replace("http://", "https://");
                }
            }
        }

        public class data
        {
            public initialVideo initialVideo { get; set; }
        }

        public class initialVideo
        {
            public string title { get; set; }
            public videoPlayInfo videoPlayInfo { get; set; }
        }

        public class videoPlayInfo
        {
            public List<video_list> video_list { get; set; }
        }

        public class video_list
        {
            public string main_url { get; set; }
            public int quality_type { get; set; }
        }

    }
}
