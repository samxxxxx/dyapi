using AutoMapper;
using DYApi.Infrastructure.Configuration;
using DYApi.Models;
using DYApi.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DYApi.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private readonly DYApiSettings _dyApiOptions;
        private readonly IMapper _mapper;

        public ConfigurationController(IOptions<DYApiSettings> dyApiOptions, IMapper mapper)
        {
            this._dyApiOptions = dyApiOptions.Value;
            this._mapper = mapper;
        }

        [HttpGet]
        [Authorize]
        public ActionResult Wechat()
        {
            var config = _mapper.Map<DYAPISettingsDto>(_dyApiOptions);
            return Ok(new ResultDataResponse<DYAPISettingsDto>
            {
                Data = config
            });
        }
    }
}
