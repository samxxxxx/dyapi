using DYApi.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService
{
    public interface IWebShortUrlService
    {
        /// <summary>
        /// 获取网址
        /// </summary>
        /// <param name="shortUrl">短网址</param>
        /// <returns></returns>
        Task<string> GetUrlAsync(string shortUrl);

        /// <summary>
        /// 批量生成空闲短网址，并返回第一个地址
        /// </summary>
        /// <param name="url"></param>
        /// <returns>短网址</returns>
        Task<WebUrl> BuildAsync();

        /// <summary>
        /// 创建短网址url
        /// </summary>
        /// <param name="url">实际地址</param>
        /// <param name="userName">创建用户</param>
        /// <returns></returns>
        Task<WebUrl> CreateAsync(string url, string userName);
    }
}
