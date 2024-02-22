using DYApi.Infrastructure;
using DYService.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYService.Users
{
    public interface IUserService
    {
        Task<LogonTokenResult> RegisterAsync(RegisterModel registerModel);

        Task<LogonTokenResult> LoginAsync(string username, string password);

        Task<LogonTokenResult> LoginWithRegisterAsync(string openId, string passWord);

        /// <summary>
        /// 设置用户分享码
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="shareUser"></param>
        /// <returns></returns>
        Task SetFromShareAsync(string userName, string shareUser);

        /// <summary>
        /// 增加用户解析记录
        /// </summary>
        /// <param name="key"></param>
        /// <param name="message"></param>
        /// <param name="success"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task AddRecordAsync(string key, string message, bool success, string userName);

        /// <summary>
        /// 返回解析记录
        /// </summary>
        /// <returns></returns>
        Task<List<UserParseRecord>> GetParseRecordAsync(string userName);
    }
}
