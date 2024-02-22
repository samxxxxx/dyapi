using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace DYService
{
    public class ParseVideoInput
    {
        /// <summary>
        /// 用户输入的链接
        /// </summary>
        public required string Key { get; set; }

        /// <summary>
        /// 经urldecode解析的key
        /// </summary>
        public string DeKey => HttpUtility.UrlDecode(Key).Replace("\r", "").Replace("\n", "");

        /// <summary>
        /// 视频地址
        /// </summary>
        public string Url => GetUrl(DeKey).Replace("点击链接直接打开", "");

        /// <summary>
        /// 返回视频的短地址
        /// </summary>
        public string? ShortName => GetShortName(Url);

        /// <summary>
        /// 视频id
        /// </summary>
        public string? VideoId => GetVideoId(DeKey);

        //public string? CacheKey
        //{
        //    get
        //    {
        //        if (ParseType == ParseTypeEnum.douyin)
        //            return ShortName;
        //        return VideoId;
        //    }
        //}

        //public ParseTypeEnum ParseType
        //{
        //    get
        //    {
        //        if (DeKey.Contains("v.douyin.com"))
        //            return ParseTypeEnum.douyin;

        //        if (DeKey.Contains("iesdouyin.com"))
        //            return ParseTypeEnum.iesdouyin;

        //        if (DeKey.Contains("b23.tv"))
        //            return ParseTypeEnum.BILIBILI;

        //        if (DeKey.Contains("ixigua.com"))
        //            return ParseTypeEnum.XIGUA;

        //        return ParseTypeEnum.UNKNOWN;
        //    }
        //}

        private string GetShortName(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;

            var regex = new Regex("\\b(?<=com/)\\w+\\b");
            var match = regex.Match(key);
            var url = string.Empty;
            if (match.Success)
            {
                url = match.Value;
            }
            return url;
        }

        private string GetUrl(string key)
        {
            var regex = new Regex("[a-zA-z]+://[^\\s]*");
            var match = regex.Match(key);
            var url = string.Empty;
            if (match.Success)
            {
                url = match.Value;
            }
            return url;
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

    public class BiVideoData
    {
        public string bvid { get; set; }
        public long aid { get; set; }

        public VideoData videoData { get; set; }
    }

    public class VideoData
    {
        public string bvid { get; set; }
        public long aid { get; set; }
        public string title { get; set; }
        public string desc { get; set; }
        public long cid { get; set; }
    }

    public class BVideoPlayer
    {
        public BVideoPlayerData data { get; set; }
    }

    public class BVideoPlayerData
    {
        public Durl[] durl { get; set; }

    }

    public class Durl
    {
        public string url { get; set; }
    }
}
