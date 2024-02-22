using DYApi.EntityframeworkCore;
using DYApi.Infrastructure;
using DYApi.Infrastructure.Configuration;
using DYService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DYService.Users
{
    public class UserService : IUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _appDbContext;
        private readonly JwtSettings _jwtSettings;

        public UserService(UserManager<AppUser> userManager, IOptions<JwtSettings> jwtSettings, AppDbContext appDbContext)
        {
            this._userManager = userManager;
            this._appDbContext = appDbContext;
            this._jwtSettings = jwtSettings.Value;
        }

        public async Task<LogonTokenResult> LoginWithRegisterAsync(string openId, string passWord)
        {
            var userName = Convert.ToBase64String(Encoding.UTF8.GetBytes(Util.Helpers.Encrypt.Md5By16(openId)));
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                //注册
                return await RegisterAsync(new RegisterModel { OpenId = openId, Password = passWord, UserName = userName });
            }
            else
            {
                //存在用户时，就直接登录
                return await LoginAsync(userName, passWord);
            }
        }


        public async Task<LogonTokenResult> LoginAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return new LogonTokenResult
                {
                    Errors = new[] { "用户不存在" },
                };
            }

            var ok = await _userManager.CheckPasswordAsync(user, password);
            if (!ok)
            {
                return new LogonTokenResult
                {
                    Errors = new[] { "密码不正确" },
                };
            }

            return GenerateJwtToken(user);
        }

        public async Task<LogonTokenResult> RegisterAsync(RegisterModel registerModel)
        {
            var user = await _userManager.FindByNameAsync(registerModel.UserName);
            if (user != null)
            {
                return new LogonTokenResult
                {
                    Errors = new[] { "用户已存在" },
                };
            }

            user = new AppUser { UserName = registerModel.UserName, OpenId = registerModel.OpenId, CreationTime = DateTime.Now };
            var result = await _userManager.CreateAsync(user, registerModel.Password);
            if (!result.Succeeded)
            {
                return new LogonTokenResult()
                {
                    Errors = result.Errors.Select(p => p.Description)
                };
            }

            return GenerateJwtToken(user);
        }

        private LogonTokenResult GenerateJwtToken(AppUser user)
        {
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecurityKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),//this.User.Identity.Name才能拿到用户名
                }),
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.Add(_jwtSettings.ExpiresIn),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var securityToken = jwtTokenHandler.CreateToken(tokenDescriptor);
            var token = jwtTokenHandler.WriteToken(securityToken);
            return new LogonTokenResult()
            {
                AccessToken = token,
                TokenType = "Bearer",
                UserName = user.UserName
            };
        }

        public async Task SetFromShareAsync(string userName, string shareUser)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user.FromUser.IsNullOrEmpty())
            {
                var fromUser = await _userManager.FindByNameAsync(shareUser);
                if (fromUser == null)
                {
                    Log.Error("分享用户不存在");
                    return;
                }
                user.FromUser = fromUser.Id;
                user.FromShareTime = DateTime.Now;
                await _userManager.UpdateAsync(user);
            }
        }

        public async Task AddRecordAsync(string key, string message, bool success, string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            var record = new UserParseRecord
            {
                CreatorTime = DateTime.Now,
                CreatorUser = user,
                CreatorUserId = user.Id,
                Key = key,
                Message = message,
                Success = success
            };

            await _appDbContext.UserParseRecords.AddAsync(record);
            await _appDbContext.SaveChangesAsync();
        }

        public async Task<List<UserParseRecord>> GetParseRecordAsync(string userName)
        {
            var list = await _appDbContext.UserParseRecords
                .Include(x => x.CreatorUser)
                .Where(x => x.CreatorUser.UserName == userName)
                .OrderByDescending(x => x.CreatorTime)
                .ToListAsync();
            return list;
        }
    }
}
