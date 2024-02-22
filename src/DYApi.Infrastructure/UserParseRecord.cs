using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYApi.Infrastructure
{
    /// <summary>
    /// 解析记录
    /// </summary>
    public class UserParseRecord
    {
        public int Id { get; set; }
        public string CreatorUserId { get; set; }
        public AppUser CreatorUser { get; set; }

        public string Key { get; set; }

        public bool Success { get; set; }

        public DateTime? CreatorTime { get; set; }

        public string? Message { get; set; }
    }
}
