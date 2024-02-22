using DYApi.Infrastructure.Configuration;
using DYApi.Infrastructure.Extensions;
using DYApi.Models;
using DYApi.Request;
using DYService.Data;
using DYService.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;
using System.Diagnostics.CodeAnalysis;

namespace DYApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly WechatMiniSettings _wechatMiniSettings;
        public UserController(IUserService userService, IOptionsSnapshot<WechatMiniSettings> wechatMiniSettings)
        {
            this._userService = userService;
            this._wechatMiniSettings = wechatMiniSettings.Value;

            /*
             Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiI0MjUzZWM5OTgwN2Q0OWE2OGE1YzY1NzJkZWUyY2NmYSIsInN1YiI6IjhkODk1YjVmLTA2NzctNDlmMi05ODc3LWI2YjE0ZTE1NmMzMyIsIm5iZiI6MTcwNDUxMjA1NiwiZXhwIjoxNzA3MTA0MDU2LCJpYXQiOjE3MDQ1MTIwNTZ9.Dc6cbGGIhyZc796NwEk-J0uyfEti1bOBuLvOglL6Wgk
             */
        }

        private async Task<ResultDataResponse<LoginTokenResponse>> LoginAccount(string username, string password)
        {
            var res = await _userService.LoginAsync(username, password);
            if (!res.Success)
            {
                return new ResultDataResponse<LoginTokenResponse>
                {
                    Error = res.Errors
                };
            }
            var data = new ResultDataResponse<LoginTokenResponse>
            {
                Data = new LoginTokenResponse
                {
                    AccessToken = res.AccessToken,
                    TokenType = res.TokenType,
                    User = new UserData
                    {
                        UserId = res.UserName,
                        SessionKey = string.Empty
                    },
                }
            };

            return data;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Login(WechatMiniProRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code) && !string.IsNullOrWhiteSpace(request.UserName))
            {
                //没有微信code使用用户名密码登录
                return Ok(await LoginAccount(request.UserName, request.Password));
            }

            var sessionResponse = await OxetekWeChatSDK.MiniPro.OpenApi.Code2Session(new OxetekWeChatSDK.Request.JsCode2SessionRequest
            {
                AppId = _wechatMiniSettings.AppId,
                AppSecret = _wechatMiniSettings.AppSecret,
                Code = request.Code
            });

            try
            {
                //用code请求微信，获取到openid
                //用openid作为用户名，密码使用openid前面8位
                var openId = sessionResponse.openid;
                var password = sessionResponse.openid.Substring(0, 8);
                var res = await _userService.LoginWithRegisterAsync(openId, password) as LogonTokenResult;

                if (res.Success)
                {

                }

                return Ok(new ResultDataResponse<LoginTokenResponse>
                {
                    Data = new LoginTokenResponse
                    {
                        AccessToken = res.AccessToken,
                        TokenType = res.TokenType,
                        User = new UserData
                        {
                            UserId = res.UserName,
                            SessionKey = sessionResponse.session_key
                        },
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.ToString());
                return Ok(new ResultDataResponse
                {
                    Error = new[] { "授权码错误" }
                });
            }
        }

        /// <summary>
        /// 用户注册，注册成功后自动生成登录token
        /// </summary>
        /// <param name="registerRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Register(RegisterRequest registerRequest)
        {
            var res = await _userService.RegisterAsync(new RegisterModel
            {
                Password = registerRequest.Password,
                OpenId = "",
                UserName = registerRequest.UserName
            });
            if (!res.Success)
            {
                return Ok(new ResultDataResponse
                {
                    Error = res.Errors
                });
            }

            return Ok(new ResultDataResponse<LoginTokenResponse>
            {
                Data = new LoginTokenResponse
                {
                    AccessToken = res.AccessToken,
                    TokenType = res.TokenType,
                    User = new UserData
                    {
                        UserId = res.UserName,
                        SessionKey = string.Empty
                    },
                }
            });
        }

        /// <summary>
        /// 用户通过分享注册，设置登录用户的分享码
        /// </summary>
        /// <param name="input">分享用户</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Share([FromBody] UserShareInput input)
        {
            try
            {
                Log.Information("用户分享 {@input}", input);

                await _userService.SetFromShareAsync(HttpContext.User.Identity.Name, input.FromUser);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "更新用户分享码错误");
            }
            return Ok(new ResultDataResponse
            {

            });
        }
    }
}
