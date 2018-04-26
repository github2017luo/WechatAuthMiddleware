using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

namespace WechatOAuth2Middleware
{
    public class WechatMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache = null;
        private readonly IWechatAccessTokenService _tokenService = null;

        public WechatMiddleware(RequestDelegate next, IMemoryCache cache, IWechatAccessTokenService tokenService)
        {
            _next = next;
            _cache = cache;
            _tokenService = tokenService;
        }
        private string MD5(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentNullException(nameof(input));
            }
            byte[] result = Encoding.Default.GetBytes(input);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(result);
            return BitConverter.ToString(output).Replace("-", "");
        }
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/wechat/code")
            {
                var appId = context.Request.Query["appid"].FirstOrDefault();
                var userIdentity = context.Request.Query["identity"].FirstOrDefault();
                var returnUrl = context.Request.Query["return_url"].FirstOrDefault();

                var returnKey = MD5(returnUrl);
                _cache.Set(returnKey, returnUrl, TimeSpan.FromMinutes(3));

                var redirect_url = $"{context.Request.Scheme}://{context.Request.Host}/wechat/callback/{appId}/{returnKey}";
                redirect_url = HttpUtility.UrlEncode(redirect_url);

                var redirect = $"https://open.weixin.qq.com/connect/oauth2/authorize" +
               $"?appid={appId}&redirect_uri={redirect_url}&response_type=code&scope=snsapi_base&state={userIdentity}";
                context.Response.Redirect(redirect);
                return;

            }
            if (context.Request.Path.Value.Contains("/wechat/callback"))
            {
                var state = context.Request.Query["state"].FirstOrDefault();
                var code = context.Request.Query["code"].FirstOrDefault();
                var appId = context.Request.Path.Value.Replace("/wechat/callback/", "").Split('/')[0];
                var returnKey = context.Request.Path.Value.Replace("/wechat/callback/", "").Split('/')[1];

                var returnUrl = _cache.Get<string>(returnKey);
                if (string.IsNullOrWhiteSpace(returnUrl) || string.IsNullOrWhiteSpace(state)|| string.IsNullOrWhiteSpace(code))
                {
                    
                    var resp = new
                    {
                        Code = 0,
                        Message =
                        $"失败;state:{state ?? string.Empty};returnUrl:{returnUrl??string.Empty};code:{code??string.Empty};" +
                        $"returnKey:{returnKey??string.Empty};appId:{appId??string.Empty}"
                    };
                    await context.Response.WriteAsync(Newtonsoft.Json.JsonConvert.SerializeObject(resp),Encoding.UTF8);
                    return;
                }
                var codeKey = MD5(returnUrl + code + state);
                var codeValue = _cache.Get<string>(codeKey);
                if (!string.IsNullOrWhiteSpace(codeValue))
                {
                    context.Response.Redirect(codeValue);
                    return;
                }
                string access_token = string.Empty;
                try
                {
                    access_token = _tokenService.GetUserToken(code, state, appId);
                    if (string.IsNullOrWhiteSpace(access_token))
                    {
                        var resp = new
                        {
                            Code = 0,
                            Message = "获取access_token失败"
                        };
                        await context.Response.WriteAsync(Newtonsoft.Json.JsonConvert.SerializeObject(resp), Encoding.UTF8);
                        return;
                    }
                    else
                    {
                        if (returnUrl.Contains('?'))
                        {
                            returnUrl = returnUrl + "&token=" + access_token;
                        }
                        else
                        {
                            returnUrl = returnUrl + "?token=" + access_token;
                        }
                        _cache.Set(codeKey, returnUrl, TimeSpan.FromHours(3));
                        context.Response.Redirect(returnUrl);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return;
            }

            //if (context.Request.Path.Value.Contains("/setcache"))
            //{
            //    var value = DateTime.Now.ToLongTimeString();
            //    _cache.Set<string>("time", value);
            //    await context.Response.WriteAsync(value);
            //    return;
            //}
            //if (context.Request.Path.Value.Contains("/getcache"))
            //{
            //    var value = _cache.Get<string>("time");
            //    await context.Response.WriteAsync(value);
            //    return;
            //}
            //if (context.Request.Path.Value.Contains("/redirect"))
            //{
            //    context.Response.Redirect("http://www.baidu.com");
            //    return;
            //}
            await _next(context);
        }
    }
}

