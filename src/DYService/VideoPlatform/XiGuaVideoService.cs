using DYApi.EntityframeworkCore.Migrations;
using DYApi.Infrastructure.Configuration;
using DYApi.Infrastructure.Extensions;
using DYService.Data;
using DYService.Extensions;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DYService
{    
    /// <summary>
    /// 西瓜视频解析
    /// </summary>
    public class XiGuaVideoService : VideoParseService
    {
        /*
         * 返回数据结构
anyVideo{}
gidInformation{}
packerData{}
video{}

	title
	videoResource{}
		normal{}
			video_list{} video_1 video_2 video_3
				definition 清晰度 360p 480p 720p 
				main_url 播放地址base64
				bitrate 比特率

        */

        private readonly IFlurlClientCache _flurlClient;

        public XiGuaVideoService(IFlurlClientCache flurlClient,
            IWebShortUrlService webShortUrlService,
            IHttpContextAccessor httpContextAccessor) : base(webShortUrlService, httpContextAccessor)
        {
            this._flurlClient = flurlClient;
        }

        protected override bool CanParse(ParseVideoInput input)
        {
            return input.DeKey.Contains("ixigua.com");
        }

        protected override async Task<ParseResult<VideoInfoData>> Request(string url)
        {
            var data = new ParseResult<VideoInfoData>();
            var client = _flurlClient.Get("xigua");

            using (var session = new CookieSession(client))
            {
                var r = await session.Request(url.Replace("点击链接直接打开", ""))
                    .WithAutoRedirect(true)
                    .GetAsync();
                var resp = await r.GetStringAsync();
                var initStr = GetInitStr(resp);
                if (initStr.IsEmpty())
                {
                    var uri = r.ResponseMessage.RequestMessage.RequestUri.AbsoluteUri;
                    r = await session.Request(uri + "&wid_try=1")
                        .WithHeader("Referer", uri)
                        .WithCookies(session.Cookies)
                        .GetAsync();
                    resp = await r.GetStringAsync();

                    initStr = GetInitStr(resp);
                    if (initStr.IsEmpty())
                    {
                        Log.Error("获取失败 {url}", url);
                        return new ParseResult<VideoInfoData>
                        {
                            Error = new[] { "解析错误" }
                        };
                    }

                    try
                    {
                        var replaceUndefined = initStr.Replace(":undefined", ":\"\"");  //替换 :undefined     :""
                        var jsonData = System.Text.Json.JsonSerializer.Deserialize<XiGuaData>(replaceUndefined);
                        if (jsonData == null || jsonData.detail == null)
                        {
                            Log.Error("获取失败 {@initStr}", initStr);
                            return new ParseResult<VideoInfoData>
                            {
                                Error = new[] { "解析错误" }
                            };
                        }

                        var mainUrl = jsonData.detail.main_url.Replace("xg-web-pc", "default");//这里因为v9-xg-web-pc域名需要referer，替换成v-default可直接访问，也可以使用v9-dy访问，抓别人的小程序包发现
                        data.Data = new VideoInfoData
                        {
                            detail = new VideoDetail
                            {
                                desc = jsonData.title,
                                url = mainUrl
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        Log.Error("获取失败 发生错误", initStr, ex);
                        return new ParseResult<VideoInfoData>
                        {
                            Error = new[] { "解析错误" }
                        };
                    }

                }
            }

            return data;
        }

        private string GetInitStr(string resp)
        {
            var reg = new Regex("(?<=window\\._SSR_HYDRATED_DATA=).*(?=</script><s)");
            var match = reg.Match(resp);
            if (match.Success)
            {
                return match.Value;
            }
            return string.Empty;

        }
    }

    public class XiGuaData
    {
        public anyVideo anyVideo { get; set; }
        public string title
        {
            get
            {
                var ti = string.Empty;
                try
                {
                    ti = anyVideo.gidInformation.packerData.video.title;
                }
                catch { }
                if (ti.IsEmpty())
                {
                    ti = anyVideo.gidInformation.packerData.albumInfo.intro;
                }
                return ti;
            }
        }
        public videoDetail detail
        {
            get
            {
                videoDetail dtl = null;
                try
                {
                    List<videoDetail> items = null;
                    if (anyVideo.gidInformation.packerData.video != null)
                    {
                        items = anyVideo.gidInformation.packerData.video.videoResource.normal.video_list.Items;
                    }
                    else if (anyVideo.gidInformation.packerData.videoResource != null)
                    {
                        items = anyVideo.gidInformation.packerData.videoResource.normal.video_list.Items;
                    }
                    if (items == null || items.Count == 0)
                        return dtl;

                    var sel = Math.Ceiling(items.Count / 2M);//取中间清晰度视频
                    dtl = items[(int)sel - 1];
                }
                catch { }
                return dtl;
            }
        }
    }

    public class anyVideo
    {
        public gidInformation gidInformation { get; set; }
    }

    public class gidInformation
    {
        public packerData packerData { get; set; }
    }

    public class packerData
    {
        public video video { get; set; }
        public videoResource videoResource { get; set; }
        public albumInfo albumInfo { get; set; }
    }

    public class albumInfo
    {
        public string title { get; set; }
        public string intro { get; set; }
        public string subTitle { get; set; }
    }

    public class video
    {
        public string title { get; set; }
        public videoResource videoResource { get; set; }
    }

    public class videoResource
    {
        public normal normal { get; set; }
    }

    public class normal
    {
        public video_list video_list { get; set; }
    }

    public class video_list
    {
        public videoDetail video_1 { get; set; }
        public videoDetail video_2 { get; set; }
        public videoDetail video_3 { get; set; }
        public videoDetail video_4 { get; set; }
        public videoDetail video_5 { get; set; }
        public videoDetail video_6 { get; set; }
        public videoDetail video_7 { get; set; }

        public List<videoDetail> Items
        {
            get
            {
                var items = new List<videoDetail>
                {
                    video_1,
                    video_2,
                    video_3,
                    video_4,
                    video_5,
                    video_6,
                    video_7,
                };
                return items.Where(x => x != null).OrderBy(x => x.bitrate).ToList();
            }
        }
    }

    public class videoDetail
    {
        //v9-xg-web-pc 这个域名下的视频 需要Referer https://www.ixigua.com/
        public string definition { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(Base64EncodedStringConverter))]
        public string main_url { get; set; }
        public int bitrate { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(Base64EncodedStringConverter))]
        public string backup_url_1 { get; set; }
    }
}
