# WechatAuthMiddleware
WechatAuthMiddleware for Asp.Net with Vue.js 

# How to use?

## Install from Nuget.org

```
Install-Package WechatMiddleware
```

```
dotnet add package WechatMiddleware
```

## Implement of your wechat token service.

```
public class WechatService:IWechatAccessTokenService
{
   public string GetUserToken(string code,string identity)
   {
       //get tokens with your own ways or the wechat offical way.
   }
}
```

##  Startup.cs file.

```
public void ConfigureServices(IServiceCollection services)
{   
    //MC
    services.AddMemoryCache();
    //IMPLE
    services.AddSingleton<IWechatAccessTokenService>(typeof(WechatService));
}

// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    //WARNING: THIS WILL RESPONSE `WECHAT/*` REQUESTS
    app.UseWechatMiddleware();

    app.Run(async (context) =>
    {
        await context.Response.WriteAsync("Hello World!");
    });
}
```

# How to use in front side ?

### if your server side url is such as: ```http://services.xxx.com``` and your browser side url is such as :```http://app.xxx.com/index#login?return_url=/user/index```

### Step1 :



- UrlEncode your browser url :

     urlencode : ```http%3a%2f%2fapp.xxx.com%2findex%23login%3freturn_url%3d%2fuser%2findex```

- appid : ``` appid of wechat ```

### Step2 :

Redirect to :

```
http://services.xxx.com/wechat/code?returl_url={urlencode}&appid=123456&identity={yourname}
```

the server will redirect to your browser side url back with a access_token:

```
http://app.xxx.com/index#login?return_url=/user/index&access_token=XXXXXXXX
```

then you can do something with this token to login.

