using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DYService.Extensions
{
    /// <summary>
    /// 从base64解析url
    /// </summary>
    public class Base64EncodedStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(reader.TryGetBytesFromBase64(out byte[] bytes))
            {
                return Encoding.UTF8.GetString(bytes);
            }
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {

        }
    }
}
