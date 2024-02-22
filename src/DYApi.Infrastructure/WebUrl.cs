using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYApi.Infrastructure
{
    /// <summary>
    /// 短网址
    /// </summary>
    public class WebUrl
    {
        public WebUrl()
        {

        }

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 实际地址
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// 短地址
        /// </summary>
        public required string ShortUri { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public required string CreatorUser { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 使用时间
        /// </summary>
        public DateTime? UseTime { get; set; }

        /// <summary>
        /// 是否删除
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 删除时间
        /// </summary>
        public DateTime? DeletionTime { get; set; }
    }
}
