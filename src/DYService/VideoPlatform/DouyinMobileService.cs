using DYApi.Infrastructure.Extensions;
using DYService.Data;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DYService
{
    /// <summary>
    /// 抖音移动端的视频解析
    /// </summary>
    public class DouyinMobileService : VideoParseService
    {
        private readonly IFlurlClientCache _flurlClient;

        public DouyinMobileService(IFlurlClientCache flurlClient, IWebShortUrlService webShortUrlService,
            IHttpContextAccessor httpContextAccessor) : base(webShortUrlService, httpContextAccessor)
        {
            this._flurlClient = flurlClient;
        }

        protected override bool CanParse(ParseVideoInput input)
        {
            return input.DeKey.Contains("v.douyin.com")|| input.DeKey.Contains("iesdouyin.com");
        }

        protected override async Task<ParseResult<VideoInfoData>> Request(string url)
        {
            var data = new ParseResult<VideoInfoData>();
            var client = _flurlClient.Get("ies");

            using (var session = new CookieSession(client))
            {
                var resp = await session.Request(url.Replace("点击链接直接打开", ""))
                    .WithHeader("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1")
                    .WithAutoRedirect(false)
                    .GetAsync();

                if (resp.Headers.TryGetFirst("location", out string location))
                {
                    resp = await session.Request(location)
                        .WithHeader("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 16_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Mobile/15E148 Safari/604.1")
                        .GetAsync();
                }

                var text = string.Empty;
                var isbr = resp.ResponseMessage.Content.Headers.ContentEncoding.Contains("br");
                if (isbr)
                {
                    var aw = await resp.GetStreamAsync();
                    //https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/middleware/request-decompression?view=aspnetcore-8.0
                    using var br = new BrotliStream(aw, CompressionMode.Decompress);
                    using var sr = new StreamReader(br);
                    text = sr.ReadToEnd();
                }
                else
                {
                    text = await resp.GetStringAsync();
                }

                var content = GetInitStr(text);

                if (content.IsEmpty())
                {
                    return new ParseResult<VideoInfoData>
                    {
                        Error = new[] { "解析错误" }
                    };
                }

                var jsonData = System.Text.Json.JsonSerializer.Deserialize<DyMobileData>(content);
                if (jsonData == null)
                {
                    return new ParseResult<VideoInfoData>
                    {
                        Error = new[] { "解析错误" }
                    };
                }
                var mainUrl = jsonData.url.Replace("http://", "https://");
                
                return new ParseResult<VideoInfoData>
                {
                    Data = new VideoInfoData
                    {
                        detail = new VideoDetail
                        {
                            desc = jsonData.title,
                            url = mainUrl,
                        }
                    }
                };
            }
        }

        private string GetInitStr(string resp)
        {
            var reg = new Regex("(?<=window\\._SSR_DATA =).*(?=</script>)");
            var match = reg.Match(resp);
            if (match.Success)
            {
                return match.Value;
            }
            return string.Empty;

        }
    }

    public class DyMobileData
    {
        public data data { get; set; }
        public string title
        {
            get
            {
                string t = string.Empty;
                try
                {
                    t = data.storeState.detail.videoData.result.title;
                }
                catch (Exception ex)
                {


                }
                return t;
            }
        }

        public string url
        {
            get
            {
                var u = string.Empty;
                try
                {
                    u = data.storeState.detail.videoData.result.url;
                }
                catch { }
                return u;
            }
        }
    }

    public class data
    {
        public storeState storeState { get; set; }
    }

    public class storeState
    {
        public detail detail { get; set; }
    }

    public class detail
    {
        public videoData videoData { get; set; }
    }

    public class videoData
    {
        public result result { get; set; }
    }

    public class result
    {
        public string url { get; set; }
        public string title { get; set; }
    }

}
