using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService.Data
{
    public class ParseResult : ParseResult<string>
    {

    }

    public class ParseResult<T>
    {
        public IEnumerable<string>? Error { get; set; }
        public T Data { get; set; }
        public bool Success => Error == null || !Error.Any();
    }

    public class VideoInfoData
    {
        public VideoDetail detail { get; set; }
    }

    public class VideoDetail
    {
        public string desc { get; set; }
        public string shortUri { get; set; }
        public string time { get; set; }
        public string url { get; set; }
    }
}
