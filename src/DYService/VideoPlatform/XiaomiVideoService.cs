using DYService.Data;
using Flurl.Http;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService.VideoPlatform
{
    public class XiaomiVideoService : VideoParseService
    {
        public XiaomiVideoService(
            IWebShortUrlService webShortUrlService, IHttpContextAccessor httpContextAccessor) : base(webShortUrlService, httpContextAccessor)
        {
        }

        protected override bool CanParse(ParseVideoInput input)
        {
            return input.DeKey.Contains("m.video.xiaomi.com");
        }

        protected override async Task<ParseResult<VideoInfoData>> Request(string url)
        {
            var uri = new Uri(url);
            var query = uri.Query;
            var id = uri.Query.Split("/", StringSplitOptions.RemoveEmptyEntries).Last();
            var api = $"https://m.video.xiaomi.com/api/a3/play?id={id}&app_key=339465011&source=h5";
            var data = await api.WithHeader("Accept", "application/json, text/plain, */*")
                .WithHeader("Origin", "https://mvstatic.cdn.pandora.xiaomi.com")
                .WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36")
                .GetJsonAsync<xiaomidata>();
            var titledata = await $"https://m.video.xiaomi.com/api/a3/entity/{id}?source=h5"
                .GetJsonAsync<titledata>();

            var resp = await data.url.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36")
                .GetAsync();

            if (!resp.Headers.TryGetFirst("location", out string mp4address))
            {
                //return new ParseResult<VideoInfoData>
                //{
                //    Error = new[] { "解析失败" }
                //};
            }

            return new ParseResult<VideoInfoData>()
            {
                Data = new VideoInfoData
                {
                    detail = new VideoDetail
                    {
                        desc = titledata.meta.title,
                        url = (mp4address ?? data.url)?.Replace("http://", "https://")
                    }
                }
            };
        }

        public class xiaomidata
        {
            public string title { get; set; }

            public List<play_info> play_info { get; set; }
            public string url
            {
                get
                {
                    return play_info[0].play_url;
                }
            }
        }
        public class play_info
        {
            public string play_url { get; set; }
        }

        public class titledata
        {
            public meta meta { get; set; }
        }

        public class meta
        {
            public string title { get; set; }
        }

    }
}
