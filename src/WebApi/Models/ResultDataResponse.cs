using OxetekWeChatSDK.Response;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;

namespace DYApi.Models
{
    public class ResultDataResponse : ResultDataResponse<string>
    {

    }

    public class ResultDataResponse<T>
    {
        public IEnumerable<string> Error { get; set; }
        public T Data { get; set; }
        public bool Success => Error == null || !Error.Any();
    }
    
    public class ParseDataInput
    {
        /// <summary>
        /// 用户输入的链接
        /// </summary>
        public required string Key { get; set; }
    }
}
