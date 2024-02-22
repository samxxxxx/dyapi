using DYApi.Infrastructure.Extensions;
using DYService.Data;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService
{
    /// <summary>
    /// 解析服务基类
    /// </summary>
    public abstract class VideoParseService
    {
        private readonly IWebShortUrlService _webShortUrlService;

        /// <summary>
        /// 需要 builder.Services.AddHttpContextAccessor();
        /// </summary>
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VideoParseService(IWebShortUrlService webShortUrlService,
            IHttpContextAccessor httpContextAccessor)
        {
            this._webShortUrlService = webShortUrlService;
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 解析后的数据
        /// </summary>
        public ParseResult<VideoInfoData>? Data { get; private set; }

        public async Task<bool> Parse(ParseVideoInput input)
        {
            if (input.DeKey.IsEmpty())
                return false;

            if (!CanParse(input))
                return false;

            Data = await Request(input.Url);

            if (Data != null && Data.Success)
            {
                await FillShortUrl();
            }
            return Data != null && Data.Success;
        }

        /// <summary>
        /// 是否可处理解析
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        protected abstract bool CanParse(ParseVideoInput input);

        /// <summary>
        /// 解析Url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected abstract Task<ParseResult<VideoInfoData>> Request(string url);

        /// <summary>
        /// 创建短网址
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<DYApi.Infrastructure.WebUrl> FillShortUrl()
        {
            var result = await _webShortUrlService.CreateAsync(Data.Data.detail.url, _httpContextAccessor.HttpContext?.User?.Identity?.Name!);
            Data.Data.detail.shortUri = result.ShortUri;
            return result;
        }
    }
}
