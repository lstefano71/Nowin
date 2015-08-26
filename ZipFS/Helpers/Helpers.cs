using System;
using Owin;
using Microsoft.Owin;
using System.Threading.Tasks;
using WildHeart.Owin.Middleware;
using Microsoft.Owin.StaticFiles;
using System.Collections.Generic;

namespace WildHeart.Owin
{
	public static class APLHelper
	{
    public static void AddMimeTypes(FileServerOptions opts, string def)
    {
      AddMimeTypes(opts, new string[] { def });
    }

    public static void AddMimeTypes(FileServerOptions opts, string[] tdefs)
    {
      AddMimeTypes(opts, tdefs as IList<string>);
    }

    public static void AddMimeTypes(FileServerOptions opts,IList<string> tdefs)
    {
      var ctp = new Microsoft.Owin.StaticFiles.ContentTypes.FileExtensionContentTypeProvider();
      foreach (var def in tdefs) {
        var d = def.Split(';');
        ctp.Mappings["." + d[0]] = d[1];
      }
      opts.StaticFileOptions.ContentTypeProvider = ctp;
    }

    public static IAppBuilder UseCompression(IAppBuilder app)
    {
      app.UseSendFileFallback();
      return app.UseStaticCompression();
    }

    public static IAppBuilder UseErrorPage(IAppBuilder app)
    {
      return app.UseErrorPage(new Microsoft.Owin.Diagnostics.ErrorPageOptions {
        SourceCodeLineCount = 20,
        ShowExceptionDetails = true,
        ShowCookies = true,
        ShowEnvironment = true,
        ShowHeaders = true,
        ShowQuery = true,
        ShowSourceCode = true
      });
    }

    public static IAppBuilder Use(IAppBuilder app, Func<IOwinContext,bool> callback)
		{
			//app.Use<HeaderMiddleware>("X-UA-Compatible", "IE=Edge");
			return app.Use((ctx, next) => {
				bool res = callback(ctx);
				if (!res)
					return next();
				return Task.Delay(0);
			});
		}
	}
}
