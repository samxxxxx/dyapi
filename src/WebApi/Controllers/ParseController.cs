using AutoMapper;
using DYApi.Infrastructure.Configuration;
using DYApi.Models;
using DYApi.Models.Dtos;
using DYService;
using DYService.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;
using System.Diagnostics;
//using AutoMapper.Configuration.Annotations;
//using Jering.Javascript.NodeJS;

namespace DYApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParseController : ControllerBase
    {
        private readonly IEnumerable<VideoParseService> _videoParseService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly DYApiSettings _dYApiSetting;

        public ParseController(IOptions<DYApiSettings> dyApiOptions,
            IEnumerable<VideoParseService> videoParseService,
            IUserService userService,
            IMapper mapper)
        {
            this._videoParseService = videoParseService;
            this._userService = userService;
            this._mapper = mapper;
            this._dYApiSetting = dyApiOptions.Value;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Index([FromBody] ParseDataInput input)
        {
            foreach (var service in _videoParseService)
            {
                var sucess = await service.Parse(new ParseVideoInput { Key = input.Key });
                if (sucess)
                {
                    Log.Information("解析成功 {type}. {@data}", service.GetType(), service.Data);
                    await _userService.AddRecordAsync(input.Key, "解析成功", sucess, HttpContext.User.Identity.Name);
                    return Ok(service.Data);
                }
                Log.Warning("失败 {type}. {@data}", service.GetType(), service.Data);
            }

            await _userService.AddRecordAsync(input.Key, "失败", false, HttpContext.User.Identity.Name);

            Log.Error("这个视频类型不支持解析 {@input}", input);
            return Ok(new ResultDataResponse
            {
                Error = new[] { "不支持的链接" }
            });
        }

        [Authorize]
        [HttpGet]
        [Route("record")]
        public async Task<ActionResult> Record()
        {
            var list = await _userService.GetParseRecordAsync(User.Identity.Name);
            var dtos = list.Select(x => _mapper.Map<UserParseRecordDto>(x)).ToList();

            return Ok(new ResultDataResponse<List<UserParseRecordDto>>
            {
                Data = dtos
            });
        }
    }
}
