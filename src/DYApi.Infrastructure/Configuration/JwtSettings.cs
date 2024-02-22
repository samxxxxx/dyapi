using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYApi.Infrastructure.Configuration
{
    public class JwtSettings
    {
        public required string SecurityKey { get; set; }
        public TimeSpan ExpiresIn { get; set; }
    }
}
