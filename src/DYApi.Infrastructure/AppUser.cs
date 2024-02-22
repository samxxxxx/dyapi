using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DYApi.Infrastructure
{
    public class AppUser : IdentityUser
    {
        [StringLength(128)]
        public string? Address { get; set; }

        public string? OpenId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreationTime { get; set; }

        /// <summary>
        /// 删除时间
        /// </summary>
        public DateTime? DeletionTime { get; set; }

        /// <summary>
        /// 分享用户
        /// </summary>
        public string? FromUser { get; set; }

        /// <summary>
        /// 接收邀请时间
        /// </summary>
        public DateTime? FromShareTime { get; set; }
    }
}
