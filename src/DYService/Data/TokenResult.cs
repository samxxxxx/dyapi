using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService.Data
{
    public class TokenResult
    {
        public bool Success => Errors == null || !Errors.Any();
        public IEnumerable<string> Errors { get; set; }

        public string AccessToken { get; set; }
        public string TokenType { get; set; }
    }
}
