using DYApi.EntityframeworkCore;
using DYApi.Infrastructure;
using DYApi.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService
{
    public class WebShortUrlService : IWebShortUrlService
    {
        private readonly AppDbContext _appDbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WebShortUrlService(AppDbContext appDbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            this._appDbContext = appDbContext;
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 生成数据
        /// </summary>
        /// <param name="n">位数</param>
        /// <param name="total">共生成多少个</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private Dictionary<string, string> GenerateRandomString(int n, int total)
        {
            if (n <= 0)
                throw new ArgumentException(nameof(n));

            //（不包含数字 0 和大写字母 I、 O 以及小写字母 l）
            const string characters = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ123456789";
            Random random = new();

            Dictionary<string, string> dic = new();

            while (true)
            {
                // 从字符集中随机选择 n 个字符并连接成字符串
                string randomString = new string(Enumerable.Repeat(characters, n)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                if (!dic.ContainsKey(randomString))
                    dic.Add(randomString, randomString);

                if (dic.Count >= total)
                    break;
            }

            return dic;
        }

        public async Task<WebUrl> BuildAsync()
        {
            var dic = GenerateRandomString(5, 1000);

            var items = dic.Select(x => new WebUrl { CreatorUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name!, ShortUri = x.Key });

            await _appDbContext.WebUrls.AddRangeAsync(items);
            await _appDbContext.SaveChangesAsync();
            return items.First();
        }

        private async Task<WebUrl> GetFree()
        {
        GOTFREE:
            var find = await _appDbContext.WebUrls
                .Where(x => string.IsNullOrEmpty(x.Url))
                .FirstOrDefaultAsync();

            if (find != null)
                return find;

            await BuildAsync();
            goto GOTFREE;
        }

        public async Task<WebUrl> CreateAsync(string url, string userName)
        {
            var find = await GetFree();

            find.Url = url;
            find.CreatorUser = userName;
            find.UseTime = DateTime.Now;

            var @new = _appDbContext.WebUrls.Update(find);
            await _appDbContext.SaveChangesAsync();
            return @new.Entity;
        }

        public async Task<string> GetUrlAsync(string shortUri)
        {
            var find = await _appDbContext.WebUrls
                .FirstOrDefaultAsync(x => x.ShortUri.Equals(shortUri, StringComparison.Ordinal) && x.IsDeleted == false && !string.IsNullOrEmpty(x.Url));
            if (find == null)
            {
                Log.Error("地址不存在 {shortUri}", shortUri);
                throw new Exception("地址不存在");
            }

            return find?.Url ?? "";
        }
    }
}
