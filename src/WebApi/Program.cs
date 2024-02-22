using DYApi.EntityframeworkCore;
using DYApi.Infrastructure;
using DYApi.Infrastructure.Configuration;
using DYApi.Models;
using DYApi.Services;
using DYService;
using DYService.DiagnosticListeners;
using DYService.Extensions;
using DYService.Users;
using DYService.VideoPlatform;
using Flurl.Http;
using Flurl.Http.Configuration;
//using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Org.BouncyCastle.Utilities.Encoders;
using Serilog;
using Serilog.Events;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using Util.Applications;

namespace DYApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //HttpDiagnosticListener.CreateListener();

            //使用serilog记录请求日志
            builder.Host.UseSerilog();

            // Add services to the container.

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add(typeof(ExceptionActionFilter));
            })
                .ConfigureApiBehaviorOptions(options =>
                {
                    //options.SuppressMapClientErrors = false;

                    //对使用 ApiControllerAttribute 进行批注的操作调用的委托，以将无效 ModelStateDictionary 转换为 IActionResult
                    //https://learn.microsoft.com/zh-cn/aspnet/core/web-api/handle-errors?view=aspnetcore-7.0#pds7
                    options.InvalidModelStateResponseFactory = (context) =>
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(context.ModelState.Values.Select(x => x.Errors).ToList());
                        Log.Logger.Error("参数验证错误 {json}", json);

                        return new ObjectResult(new ResultDataResponse
                        {
                            Error = new[] { "参数验证错误" }
                        });
                    };
                })
                .AddJsonOptions(options =>
                {
                    //options.JsonSerializerOptions.Converters.Add(new Base64EncodedStringConverter());
                });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "MyOpenAPI",
                    Description = "TestAPI",
                    Version = "v1"
                });
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "Input the JWT like: Bearer {your token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });


            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("Default");
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), optionBuilder => optionBuilder.EnableStringComparisonTranslations(true));
                //options.UseSqlServer(connectionString);
            });
            builder.Services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+=";
            }).AddEntityFrameworkStores<AppDbContext>();

            //配置
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(nameof(JwtSettings)));
            builder.Services.Configure<MemorySettings>(builder.Configuration.GetSection(nameof(MemorySettings)));
            builder.Services.Configure<WechatMiniSettings>(builder.Configuration.GetSection(nameof(WechatMiniSettings)));
            builder.Services.Configure<DYApiSettings>(builder.Configuration.GetSection("App"));

            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IWebShortUrlService, WebShortUrlService>();
            builder.Services.AddScoped<VideoParseService, BiVideoService>();
            builder.Services.AddScoped<VideoParseService, XiGuaVideoService>();
            builder.Services.AddScoped<VideoParseService, DouyinMobileService>();
            builder.Services.AddScoped<VideoParseService, DouyinService>();
            builder.Services.AddScoped<VideoParseService, KuaishouVideoService>();
            builder.Services.AddScoped<VideoParseService, ToutiaoVideoService>();
            builder.Services.AddScoped<VideoParseService, XiaomiVideoService>();            

            //flurl
            builder.Services.AddSingleton<IFlurlClientCache>(sp =>
            {
                var client = new FlurlClientCache();
                client.Add("vdy", "https://v.douyin.com", builder =>
                {
                    builder.WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                    builder.WithHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                    builder.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0");
                    builder.WithHeader("Upgrade-Insecure-Requests", "1");
                    builder.WithHeader("Sec-Fetch-User", "?1");
                    builder.WithHeader("sec-ch-ua-mobile", "?0");
                    builder.WithHeader("Sec-Ch-Ua-Platform", "\"Windows\"");
                    builder.WithHeader("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Microsoft Edge\";v=\"120\"");
                    builder.WithHeader("Sec-Fetch-Dest", "document");
                    builder.WithHeader("Accept-Encoding", "gzip, deflate, br");
                });
                client.Add("dy", "https://www.douyin.com", builder =>
                {
                    builder.WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                    builder.WithHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                    builder.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0");
                    builder.WithHeader("Upgrade-Insecure-Requests", "1");
                    builder.WithHeader("Sec-Fetch-User", "?1");
                    builder.WithHeader("sec-ch-ua-mobile", "?0");
                    builder.WithHeader("Sec-Ch-Ua-Platform", "\"Windows\"");
                    builder.WithHeader("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Microsoft Edge\";v=\"120\"");
                    builder.WithHeader("Sec-Fetch-Dest", "document");
                    builder.WithHeader("Accept-Encoding", "gzip, deflate, br");
                });
                client.Add("ies", "https://www.iesdouyin.com", builder =>
                {
                    builder.WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                    builder.WithHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                    builder.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0");
                    builder.WithHeader("Upgrade-Insecure-Requests", "1");
                    builder.WithHeader("Sec-Fetch-User", "?1");
                    builder.WithHeader("sec-ch-ua-mobile", "?0");
                    builder.WithHeader("Sec-Ch-Ua-Platform", "\"Windows\"");
                    builder.WithHeader("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Microsoft Edge\";v=\"120\"");
                    builder.WithHeader("Sec-Fetch-Dest", "document");
                    builder.WithHeader("Accept-Encoding", "gzip, deflate, br");
                });
                client.Add("bilibili", "https://www.bilibili.com", builder =>
                {

                });
                client.Add("apibilibili", "https://api.bilibili.com", builder =>
                {
                    //builder.WithHeader("", "");
                });
                client.Add("xigua", "https://www.ixigua.com", builder =>
                {
                    builder.WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                    builder.WithHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                    builder.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0");
                    builder.WithHeader("Upgrade-Insecure-Requests", "1");
                    builder.WithHeader("Sec-Fetch-User", "?1");
                    builder.WithHeader("sec-ch-ua-mobile", "?0");
                    builder.WithHeader("Sec-Ch-Ua-Platform", "\"Windows\"");
                    builder.WithHeader("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Microsoft Edge\";v=\"120\"");
                    builder.WithHeader("Sec-Fetch-Dest", "document");
                    builder.WithHeader("Accept-Encoding", "gzip, deflate, br");
                });

                client.Add("kuaishou", "https://v.kuaishou.com", builder =>
                {
                    builder.WithHeader("Accept", "*/*");
                    builder.WithHeader("Cache-Control", "no-cache");
                    builder.WithHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                    builder.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
                    builder.WithHeader("Accept-Encoding", "gzip, deflate, br");
                });
                client.Add("toutiao", "toutiao.com", builder =>
                {
                    builder.WithHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                    builder.WithHeader("Cache-Control", "no-cache");
                    builder.WithHeader("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                    builder.WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
                    builder.WithHeader("Accept-Encoding", "gzip, deflate, br");
                });
                return client;
            });

            builder.Services.AddHttpClient();
            builder.Services.AddRequestDecompression();

            //nodejs call javascript
            //builder.Services.AddNodeJS();

            //使用缓存
            builder.Services.AddMemoryCache(options =>
            {
                //options.ExpirationScanFrequency 
            });

            var provider = builder.Services.BuildServiceProvider();
            var jwtSettings = provider.GetRequiredService<IOptions<JwtSettings>>().Value;

            var tokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.SecurityKey)),
                ClockSkew = TimeSpan.Zero,

            };
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => { options.TokenValidationParameters = tokenValidationParameters; }); ;

            //automapper
            builder.Services.AddAutoMapper(typeof(MapperProfile));

            //服务器健康检查
            builder.Services.AddHealthChecks();

            //service层能访问httpcontext
            builder.Services.AddHttpContextAccessor();

            //https://www.cnblogs.com/dudu/p/11088645.html
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            var app = builder.Build();

            app.UseForwardedHeaders();
            app.UseSeriLog();
            //使用serilog记录请求日志
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, context) =>
                {
                    /*
                    //日志MessageTemplate 额外信息设置
                    var host = context.Request.Host.ToString();
                    diagnosticContext.Set("Host", host);
                    */
                };
                //设置serilog日志消息模板
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} Referer {Referer} RemoteIpPort {RemoteIpPort} responded {StatusCode} user {UserName} in {Elapsed:0.0000} ms";
                options.GetMessageTemplateProperties = (httpContext, requestPath, elapsedMs, statusCode) =>
                {
                    var requestIpPort = $"{httpContext.Connection.RemoteIpAddress?.MapToIPv4()}:{httpContext.Connection.RemotePort}";
                    return new[]
                    {
                        new LogEventProperty("RequestMethod", new ScalarValue(httpContext.Request.Method)),
                        new LogEventProperty("RequestPath", new ScalarValue(requestPath)),
                        new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
                        new LogEventProperty("Elapsed", new ScalarValue(elapsedMs)),
                        new LogEventProperty("Referer", new ScalarValue(httpContext.Request.Headers.Referer)),
                        new LogEventProperty("UserName", new ScalarValue(httpContext.User?.Identity?.Name)),
                        new LogEventProperty("RemoteIpPort", new ScalarValue(requestIpPort)),
                    };
                };
            });

            //配置静态首页
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = new[] { "index.htm" },
            });
            app.UseStaticFiles();

            app.UseRequestDecompression();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseStatusCodePages(async (statusContext) =>
                {
                    if (statusContext.HttpContext.Response.StatusCode == 404)
                    {
                        //重写404页面
                        var ipaddressPort = $"{statusContext.HttpContext.Connection.RemoteIpAddress?.MapToIPv4()}:{statusContext.HttpContext.Connection.RemotePort}";
                        var id = $"{statusContext.HttpContext.Connection.Id}";

                        statusContext.HttpContext.Response.ContentType = $"{System.Net.Mime.MediaTypeNames.Text.Html}; charset=utf-8";
                        await statusContext.HttpContext.Response.WriteAsync(@$"
<h1>页面不存在！</h1>
<br/>
<hr>
已记录：{ipaddressPort} ，request id：{id}
", Encoding.UTF8);
                    }
                });
            }

            app.UseHttpsRedirection();

            //增加验证
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            //使用健康检查 需要登录
            app.MapHealthChecks("/api/healthz").RequireAuthorization();

            //短地址服务
            app.MapGet("/s/{code}", async ([FromServices] IWebShortUrlService webShortService, HttpContext context, string code) =>
            {
                var Url = await webShortService.GetUrlAsync(code);
                context.Response.Redirect(Url);
            });

            app.Run();
        }
    }
}