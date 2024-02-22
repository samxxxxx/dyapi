using AutoMapper;
using DYApi.Infrastructure.Configuration;
using DYService.Data;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DYService
{
    public class DouyinService : VideoParseService
    {
        private readonly IFlurlClientCache _flurlClientCache;
        private readonly IWebShortUrlService _webShortUrlService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MemorySettings _memorySettings;
        private readonly IMapper _mapper;

        public DouyinService(IFlurlClientCache flurlClient, 
            IWebShortUrlService webShortUrlService, 
            IHttpContextAccessor httpContextAccessor,
            IOptions<MemorySettings> memorySettings,
            AutoMapper.IMapper mapper) : base(webShortUrlService, httpContextAccessor)
        {
            this._flurlClientCache = flurlClient;
            this._webShortUrlService = webShortUrlService;
            this._httpContextAccessor = httpContextAccessor;
            this._memorySettings = memorySettings.Value;
            this._mapper = mapper;
        }

        protected override bool CanParse(ParseVideoInput input)
        {
            return input.DeKey.Contains("v.douyin.com") || input.DeKey.Contains("iesdouyin.com");
        }

        protected override async Task<ParseResult<VideoInfoData>> Request(string url)
        {
            /*
             * 省去了计算 __ac_signature cookie值的步骤，经测试下面json api是不需要__ac_signature这个参数
             有二种域名：
                1、访问 https://v.douyin.com/iLkweNS1/
                    a、跳转到 https://www.iesdouyin.com/share/note/7324204555275750667 从cookie拿到 ttwid
                    b、访问a后，跳转到 https://www.douyin.com/video/7318693128016252212?previous_page=app_code_link 从cookie拿到 __ac_nonce
                    c、访问 json api拿到视频数据 https://www.douyin.com/aweme/v1/web/aweme/detail/?device_platform=webapp&aid=6383&channel=channel_pc_web&aweme_id={videoId}

                2、访问 https://www.iesdouyin.com/share/video/7316418665430158631/?region=CN&mid=7316418944175213362 从cookie拿到 ttwid
                    a、跳转到 https://www.douyin.com/video/7316418665430158631?previous_page=app_code_link 从cookie拿到 __ac_nonce
                    b、访问 json api拿到视频数据 https://www.douyin.com/aweme/v1/web/aweme/detail/?device_platform=webapp&aid=6383&channel=channel_pc_web&aweme_id={videoId}

             */

            //var sign = await _nodeJSService.InvokeFromFileAsync<string>(@"C:\github\ad\main.js", args: new object[] { "06597cb71006ca713fe1" });
            ParseResult<VideoInfoData> data = null;
            DYDataDto videoDYData = null;

            var iesClient = _flurlClientCache.Get("ies");
            var dyClient = _flurlClientCache.Get("dy");

            try
            {
                using (var dySession = new CookieSession(dyClient))
                {
                    var vdyResp = await dySession
                        .Request(url)
                        .WithAutoRedirect(false)
                        .GetAsync();

                    if (!vdyResp.Headers.TryGetFirst("location", out string location))
                    {
                        return new ParseResult<VideoInfoData>
                        {
                            Error = new[] { "不支持的链接" }
                        };
                    }

                    FlurlCookie? ttwid = vdyResp.Cookies.FirstOrDefault(x => x.Name == "ttwid");

                    //从请求头中的重定向头 获取videoid
                    var videoId = GetVideoId(location) ?? GetVideoId(location, @"\b(?<=share/note/)\d+\b");

                    if (string.IsNullOrEmpty(videoId))
                    {
                        return new ParseResult<VideoInfoData>
                        {
                            Error = new[] { "视频获取失败" }
                        };
                    }
                    Log.Debug($"video id{videoId}");

                    if (ttwid == null)
                    {
                        var iesRequest = iesClient.WithAutoRedirect(false).Request(location);
                        IFlurlResponse iesResp = await iesRequest.GetAsync();
                        iesResp.Headers.TryGetFirst("location", out location);

                        Log.Debug($"location {location}");

                        //**********只有拿到ttwid值，才能进行后面的请求**********
                        //**********从cookie中等到 ttwid值**********
                        ttwid = iesResp.Cookies.FirstOrDefault(x => x.Name == "ttwid");
                    }

                    if (ttwid == null)
                    {
                        return new ParseResult<VideoInfoData>
                        {
                            Error = new[] { "ttwid为空" }
                        };
                    }

                    var dyRequest = dySession.Request(location)
                            .WithAutoRedirect(false)
                            .WithHeader("Referer", $"https://www.douyin.com/video/{videoId}?previous_page=web_code_link")
                            ;

                    IFlurlResponse dyResp = await dyRequest.GetAsync();

                    var res = await dySession
                        .Request($"https://www.douyin.com/aweme/v1/web/aweme/detail/?device_platform=webapp&aid=6383&channel=channel_pc_web&aweme_id={videoId}")
                        .WithAutoRedirect(false)
                        .WithHeader("Referer", $"https://www.douyin.com/video/{videoId}?previous_page=web_code_link")
                        .WithCookie("__ac_referer", "__ac_blank")
                        .WithCookie("ttwid", $"{ttwid.Value}")
                        .GetAsync();

                    //获取到__ac_nonce,需要得到签名值：__ac_signature

                    using var aw = await res.GetStreamAsync();
                    var isbr = res.ResponseMessage.Content.Headers.ContentEncoding.Contains("br");
                    if (isbr)
                    {
                        //https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/middleware/request-decompression?view=aspnetcore-8.0
                        using var br = new BrotliStream(aw, CompressionMode.Decompress);
                        using var sr = new StreamReader(br);
                        var brStr = sr.ReadToEnd();
                        var jsonData = System.Text.Json.JsonSerializer.Deserialize<DYData>(brStr);

                        if (jsonData != null)
                        {
                            if (jsonData.aweme_detail == null)
                            {
                                Log.Error($"视频不存在或删除 {brStr}");
                                return new ParseResult<VideoInfoData>
                                {
                                    Error = new[] { "视频不存在或删除" }
                                };
                            }
                            videoDYData = _mapper.Map<DYDataDto>(jsonData);

                            data = new ParseResult<VideoInfoData>();
                            data.Data = new VideoInfoData
                            {
                                detail = new VideoDetail
                                {
                                    desc = videoDYData.aweme_detail.desc,
                                    url = videoDYData.aweme_detail.url,
                                }
                            };
                        }
                    }
                    else
                    {
                        //#region 其他压缩方式 解压

                        ////deflate
                        //using (var ds = new DeflateStream(aw, CompressionMode.Decompress))
                        //using (var sr = new StreamReader(ds))
                        //{
                        //    var text = sr.ReadToEnd();
                        //}

                        ////gzip
                        //using var gzip = new GZipStream(aw, CompressionMode.Decompress);
                        //using var stream = new MemoryStream();
                        //gzip.CopyTo(stream);

                        //var buffer = stream.ToArray();
                        //var strJson = Encoding.Default.GetString(buffer);
                        //var jsonData = System.Text.Json.JsonSerializer.Deserialize<DYData>(strJson);
                        //#endregion
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.ToString());
                return new ParseResult<VideoInfoData>
                {
                    Error = new[] { "服务出现异常!" }
                };
            }

            return data;
        }

        public string GetVideoId(string key, string? pattern = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;

            var regex = new Regex(pattern ?? "\\b(?<=video/)\\d+\\b");
            var match = regex.Match(key);
            string? url = null;
            if (match.Success)
            {
                url = match.Value;
            }
            return url;
        }
    }

    public class DYData
    {
        public aweme_detail aweme_detail { get; set; }
    }

    public class aweme_detail
    {
        /// <summary>
        /// 视频描述
        /// </summary>
        public string desc { get; set; }

        public string preview_title { get; set; }

        /// <summary>
        /// 视频时长 毫秒
        /// </summary>
        public int duration { get; set; }
        public string time
        {
            get
            {
                var d = TimeSpan.FromMilliseconds(duration);
                return d.ToString(@"mm\:ss");
            }
        }

        public string url
        {
            get
            {
                if (video != null)
                {
                    return video.play_addr.url_list[0];
                }
                return string.Empty;
            }
        }

        public dyvideo video { get; set; }
    }

    public class dyvideo
    {
        public play_addr play_addr { get; set; }
    }

    public class play_addr
    {
        public List<string> url_list { get; set; }
    }

    public class DYDataDto
    {
        [JsonPropertyName("detail")]
        public aweme_detailDto aweme_detail { get; set; }
    }


    public class aweme_detailDto
    {
        /// <summary>
        /// 视频描述
        /// </summary>
        public string desc { get; set; }

        /// <summary>
        /// 视频时长 毫秒
        /// </summary>
        [JsonIgnore]
        public int duration { get; set; }
        public string time
        {
            get
            {
                var d = TimeSpan.FromMilliseconds(duration);
                return d.ToString(@"mm\:ss");
            }
        }

        public string url
        {
            get
            {
                if (video != null)
                {
                    return video.play_addr.url_list[0].Replace("http://", "https://");
                }
                return string.Empty;
            }
        }

        [JsonIgnore]
        public dyvideo video { get; set; }

        /// <summary>
        /// 短地址
        /// </summary>
        public string ShortUri { get; set; }
    }
}
