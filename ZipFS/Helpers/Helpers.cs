using System;
using Owin;
using Microsoft.Owin;
using System.Threading.Tasks;
using WildHeart.Owin.Middleware;
using Microsoft.Owin.StaticFiles;
using System.Collections.Generic;
using Microsoft.Owin.Hosting;

namespace WildHeart.Owin
{
  public class SimpleConfiguration
  {		
    public object FS { get; set; }
    public string Root { get; set; }
    public string Url { get; set; }
    public string API { get; set; }
    public string PoolName { get; set; }

		public int Port { get; set; }

		public string ServerFactory { get; set; }

    public IDictionary<string,object> Settings { get; }
    public SimpleConfiguration()
    {
      Settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }
  }

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

    public static IAppBuilder Use(IAppBuilder app, Func<IOwinContext,object[], bool> callback,params object[] args)
		{
			return app.Use((ctx, next) => {
				bool res = callback(ctx,args);
				if (!res)
					return next();
				return Task.Delay(0);
			});
		}

    public static IAppBuilder Map(IAppBuilder app, SimpleConfiguration cfg, string path, Action<IAppBuilder, SimpleConfiguration> callback)
    {    
      return app.Map(path, sub => callback(sub, cfg));
    }


    public static IAppBuilder UseFromPool(IAppBuilder app, string pool, string fnname, Func<string, string, IOwinContext, bool> callback)
    {

      return app.Use((ctx, next) => {
        bool res = callback(pool, fnname, ctx);
        if (!res)
          return next();
        return Task.Delay(0);
      });
    }

    public static IDisposable Start(SimpleConfiguration cfg, Action<IAppBuilder, SimpleConfiguration> builder)
    {
			var start = new StartOptions() {
				Port = cfg.Port
			};

			if(cfg.Url != null) {
				start.Urls.Add(cfg.Url);
			}

			if(cfg.ServerFactory != null) {
				start.ServerFactory = cfg.ServerFactory;
			}
      return WebApp.Start(start, app => builder(app, cfg));
    }

    public static object Test(Func<object,object> callback)
    {
      return callback(null);
    }


  }
}
