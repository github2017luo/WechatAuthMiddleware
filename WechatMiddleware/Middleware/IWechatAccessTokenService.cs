namespace WechatOAuth2Middleware
{
    public interface IWechatAccessTokenService
    {
         string GetUserToken(string code,string identity);
    }
}