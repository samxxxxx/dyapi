using DYApi.Infrastructure;

namespace DYApi.Models.Dtos
{
    public class UserParseRecordDto
    {
        public string Key { get; set; }

        public bool Success { get; set; }

        public DateTime? CreatorTime { get; set; }
    }
}
