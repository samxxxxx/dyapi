using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYApi.Infrastructure.Configuration
{
    public class WechatMiniSettings
    {
        public required string AppId { get; set; }

        public required string AppSecret { get; set; }
    }
}
