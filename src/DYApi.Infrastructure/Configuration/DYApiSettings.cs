using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYApi.Infrastructure.Configuration
{
    public class DYApiSettings
    {
        /// <summary>
        /// 短地址域名
        /// </summary>
        public required string ShortUrlDomain { get; set; }

        public ShareApp? ShareApp { get; set; }
        /// <summary>
        /// 资源服务器url
        /// </summary>
        public string? ResDomain { get; set; }

        public DownPage? DownPage { get; set; }

        public ServicePage? ServicePage { get; set; }

        public QuestionPage? QuestionPage { get; set; }
    }

    public class ShareApp
    {
        public string? Title { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class DownPage
    {
        public string? Title { get; set; }
    }

    public class ServicePage
    {
        public List<string>? Text { get; set; }
    }

    public class QuestionPage
    {
        public List<QuestionItem>? Values { get; set; }
    }

    public class QuestionItem
    {
        public string? Title { get; set; }
        public string? Label { get; set; }
    }
}
