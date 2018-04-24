using Microsoft.AspNetCore.Builder;
namespace  WechatOAuth2Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseWechatMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WechatMiddleware>();
        }
 
    }
}
