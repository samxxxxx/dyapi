using DYApi.Infrastructure.Configuration;
using DYApi.Infrastructure.Extensions;
using DYService.Data;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.RegularExpressions;

namespace DYService
{
    public class BiVideoService : VideoParseService
    {
        private readonly IFlurlClientCache _flurlClient;

        public BiVideoService(IFlurlClientCache flurlClient,
            IWebShortUrlService webShortUrlService,
            IHttpContextAccessor httpContextAccessor) : base(webShortUrlService, httpContextAccessor)
        {
            this._flurlClient = flurlClient;
        }

        protected override bool CanParse(ParseVideoInput input)
        {
            return input.DeKey.Contains("b23.tv");
        }

        protected override async Task<ParseResult<VideoInfoData>> Request(string url)
        {
            var client = _flurlClient.Get("bilibili");
            var res = await url.WithAutoRedirect(false)
                .GetAsync();

            if (!res.Headers.TryGetFirst("Location", out string location))
            {
                //没有重定向头，可能发生错误
                return new ParseResult<VideoInfoData>
                {
                    Error = new[] { "解析错误" }
                };
            }

            //存取cookie
            //buvid3 b_nut
            BiVideoData biVideoData = null;
            using (var session = new CookieSession(client))
            {
                var data = await session.Request(location)
                    .WithAutoRedirect(true)
                    .GetAsync();

                var resp = await data.GetStringAsync();

                var initStr = GetInitStr(resp);
                if (initStr.IsEmpty())
                {
                    Log.Error("获取aid失败 {url}", url);
                    return new ParseResult<VideoInfoData>
                    {
                        Error = new[] { "解析错误" }
                    };
                }
                biVideoData = System.Text.Json.JsonSerializer.Deserialize<BiVideoData>(initStr);
                Log.Information("initStr {initStr} location {location} biVideoData {@biVideoData}", initStr, location, biVideoData);
            }
            BVideoPlayer BVideoPlayer = null;
            int count = 3;
            while (true)
            {
                var requestUrl = $"https://api.bilibili.com/x/player/playurl?avid={biVideoData.aid}&cid={biVideoData.videoData.cid}&qn=1&type=&otype=json&platform=html5&high_quality=1";
                //var requestUrl = $"https://api.bilibili.com/x/player/wbi/playurl?cid={biVideoData.videoData.cid}&bvid={biVideoData.bvid}";
                var res3 = await requestUrl
                        .WithHeader("Referer", "https://www.bilibili.com")
                        .WithHeader("Accept-Encoding", "gzip, deflate, br")
                        .WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0")
                        .GetAsync();
                var tmp = await res3.GetStringAsync();
                BVideoPlayer = System.Text.Json.JsonSerializer.Deserialize<BVideoPlayer>(tmp);
                if (BVideoPlayer.data != null && BVideoPlayer.data.durl != null)
                    break;

                Thread.Sleep(100);

                count--;
                if (count <= 0)
                {
                    Log.Error("没有得到api 结果 requestUrl {requestUrl} {tmp} videoData {@biVideoData} ", requestUrl, tmp, biVideoData);
                    return new ParseResult<VideoInfoData>
                    {
                        Error = new[] { "解析错误" }
                    };
                }
            }

            var mainUrl = BVideoPlayer.data.durl[0].url;

            return new ParseResult<VideoInfoData>
            {
                Data = new VideoInfoData
                {
                    detail = new VideoDetail
                    {
                        desc = biVideoData.videoData.desc,
                        url = mainUrl,
                        time = "",
                    }
                }
            };
        }

        private string GetInitStr(string resp)
        {
            var reg = new Regex("(?<=window\\.__INITIAL_STATE__=).*(?=;\\(function)");
            var match = reg.Match(resp);
            if (match.Success)
            {
                return match.Value;
            }
            return string.Empty;

        }


    }
}
