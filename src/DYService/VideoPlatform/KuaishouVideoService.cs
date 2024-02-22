using DYService.Data;
using Flurl.Http;
using Flurl.Http.Configuration;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService
{
    /// <summary>
    /// 快手视频解析服务
    /// </summary>
    public class KuaishouVideoService : VideoParseService
    {
        private readonly IFlurlClientCache _flurlClient;
        private readonly IWebShortUrlService _webShortUrlService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public KuaishouVideoService(IFlurlClientCache flurlClient,
            IWebShortUrlService webShortUrlService,
            IHttpContextAccessor httpContextAccessor) : base(webShortUrlService, httpContextAccessor)
        {
            this._flurlClient = flurlClient;
            this._webShortUrlService = webShortUrlService;
            this._httpContextAccessor = httpContextAccessor;
        }

        protected override bool CanParse(ParseVideoInput input)
        {
            return input.DeKey.Contains("v.kuaishou.com");
        }

        protected override async Task<ParseResult<VideoInfoData>> Request(string url)
        {
            var data = new ParseResult<VideoInfoData>();
            var client = _flurlClient.Get("kuaishou");

            using (var session = new CookieSession(client))
            {
                var resp = await session.Request(url)
                    .WithAutoRedirect(true)
                    .GetAsync();

                if (!resp.Headers.TryGetFirst("location", out string location))
                {
                    if (string.IsNullOrWhiteSpace(location) && resp.StatusCode == 200)
                    {
                        location = resp.ResponseMessage.RequestMessage.RequestUri.ToString();
                    }
                    else
                    {
                        Log.Error("未取到重定向地址 {url}", url);
                        return new ParseResult<VideoInfoData>
                        {
                            Error = new[] { "解析错误" }
                        };
                    }
                }

                //http://www.kuaishou.com/short-video/3x3yb79by57mw62?fid=0&cc=share_copylink&followRefer=151&shareMethod=TOKEN&docId=9&kpn=KUAISHOU&subBiz=BROWSE_SLIDE_PHOTO&photoId=3x3yb79by57mw62&shareId=17775704905295&shareToken=X-993QnTeN2HHbGq&shareResourceType=PHOTO_OTHER&userId=3xdwer6ihtq3qqg&shareType=1&et=1_i%252F2006753625142438049_bs4143%2524s&shareMode=APP&originShareId=17775704905295&appType=21&shareObjectId=5251478763035649789&shareUrlOpened=0&timestamp=1706228039104&utm_source=app_share&utm_medium=app_share&utm_campaign=app_share&location=app_share

                var photoId = Util.Helpers.Regex.GetValue(location, "(?<=short-video/).*(?=\\?)");
                if(string.IsNullOrEmpty(photoId))
                {
                    photoId = Util.Helpers.Regex.GetValue(location, "(?<=photo/).*(?=\\?)");
                }
                //参考 https://github.com/5ime/video_spider/blob/master/src/video_spider.php

                var postData = new
                {
                    photoId = photoId,
                    isLongVideo = false
                };

                var did = resp.Cookies.FirstOrDefault(x => x.Name == "did");
                var didv = resp.Cookies.FirstOrDefault(x => x.Name == "didv");

                if (did == null)
                {
                    Log.Error("没取到cookie did {did}", did);
                }

                //https://github.com/ihmily/DouyinLiveRecorder/issues/53
                // Referer 用www开头的不会有滑块验证
                var jsonData = await session.Request("https://v.m.chenzhongtech.com/rest/wd/photo/info?kpn=KUAISHOU&captchaToken=")
                    .WithHeader("Referer", location)
                    .WithCookie("did", did?.Value)
                    .WithCookie("didv", didv?.Value)
                    .PostJsonAsync(postData)
                    .ReceiveJson<KuaishouData>();

                if (jsonData != null && jsonData.Success)
                {
                    return new ParseResult<VideoInfoData>
                    {
                        Data = new VideoInfoData
                        {
                            detail = new VideoDetail
                            {
                                desc = jsonData.photo.caption,
                                url = jsonData.mp4Url
                            }
                        }
                    };
                }

                Log.Error("解析错误: {@jsonData}", jsonData);
                return new ParseResult<VideoInfoData>
                {
                    Error = new[] { "解析失败" }
                };
            }
        }
    }

    public class KuaishouData
    {
        public bool Success => result == 1;
        public int result { get; set; }
        public string mp4Url { get; set; }
        public photo photo { get; set; }
        public string error_msg { get; set; }
    }

    public class photo
    {
        public string caption { get; set; }
    }
}
