using DYApi.Infrastructure.Configuration;

namespace DYApi.Models.Dtos
{
    public class DYAPISettingsDto
    {
        // <summary>
        /// 短地址域名
        /// </summary>
        public required string Domain { get; set; }

        public ShareApp? ShareApp { get; set; }

        public DownPage? DownPage { get; set; }
        public ServicePage? ServicePage { get; set; }

        public QuestionPage? QuestionPage { get; set; }
    }
}
