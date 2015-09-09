using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin.StaticFiles.ContentTypes;

namespace OwinHostingSample
{
	public static class TupleListExtensions
	{
		public static void Add<T1, T2>(
						this IList<Tuple<T1, T2>> list, T1 item1, T2 item2)
		{
			list.Add(Tuple.Create(item1, item2));
		}

		public static void Add<T1, T2, T3>(
						this IList<Tuple<T1, T2, T3>> list, T1 item1, T2 item2, T3 item3)
		{
			list.Add(Tuple.Create(item1, item2, item3));
		}

	}



	static class Program
	{
		static void Main(string[] args)
		{
			var options = new StartOptions {
				// ServerFactory = "Nowin",
				Port = 8080
			};

			using (WebApp.Start<Startup>(options)) {
				Console.WriteLine("Running a http server on port 8080");
				Console.ReadKey();
			}
		}
	}

	public class TimerMiddleware : OwinMiddleware
	{
		readonly System.Diagnostics.Stopwatch _timer = new System.Diagnostics.Stopwatch();

		public TimerMiddleware(OwinMiddleware next)
				: base(next)
		{
		}

		public async override Task Invoke(IOwinContext context)
		{
			_timer.Restart();
			await Next.Invoke(context);
			context.TraceOutput.WriteLine($"{context.Request.Path}: {_timer.ElapsedMilliseconds}ms");
		}
	}
	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			app.Use((context, next) => {
				var req = context.Request;
				context.TraceOutput.WriteLine("{0} {1}{2} {3}", req.Method, req.PathBase, req.Path, req.QueryString);
				return next();
			});

			app.UseErrorPage(new Microsoft.Owin.Diagnostics.ErrorPageOptions { SourceCodeLineCount = 20 });
			app.Use<WildHeart.Owin.Middleware.HeaderMiddleware>("X-UA-Compatible","IE=Edge");
			app.UseSendFileFallback();
			app.Use<TimerMiddleware>();

			app.UseStaticCompression();

			app.UseFileServer(new FileServerOptions {
				FileSystem = new PhysicalFileSystem("."),
				RequestPath = new PathString("/files"),
				EnableDirectoryBrowsing = true
			});

			var zip1 = new WildHeart.Owin.FileSystems.ZipFileSystem("TestZip.zip");
			var zip2 = new WildHeart.Owin.FileSystems.ZipFileSystem("TestZip.zip");
			var zip3 = new WildHeart.Owin.FileSystems.ZipFileSystem(File.ReadAllBytes("TestZip.zip"));
			//var df1 = new PhysicalFileSystem(".");
			var df1 = new PhysicalFileSystem(@"k:\users\klaus\JS");

			var dic = new List<Tuple<string, IFileSystem>>() {
				{ "/", zip1 }, { "/sub", zip2 }, { "/inMemory", zip3 },
				{ "/secret/root", df1 }, { "/sub/root", df1 },
				{ "/SAME", zip1 }, {"/same", df1 }
			};

			//var dic = new List<Tuple<string,IFileSystem>> { 
			//	{ "/",zip1 }, { "/", df1 }
			//};

			var fs = new WildHeart.Owin.FileSystems.CompositeFileSystem(dic);

			var fso = new FileServerOptions {
				//FileSystem = new WildHeart.Owin.FileSystems.ZipFileSystem("TestZip.zip"),
				FileSystem = fs,
				RequestPath = new PathString("/composite"),
				EnableDirectoryBrowsing = true
			};

			fso.StaticFileOptions.ContentTypeProvider = new MyMimeSet();

			app.UseFileServer(fso);

			app.Run(context => {
				if (context.Request.Path.Value == "/") {
					context.Response.ContentType = "text/plain";
          var txt = "Hello World! It's now " + DateTime.Now;
          context.Response.ETag = DateTime.Now.Ticks.ToString("x");
          context.Response.Write(txt);
          return Task.Delay(0);
          //return context.Response.WriteAsync(txt);
				}

				context.Response.StatusCode = 404;
				return context.Response.WriteAsync($"<p><b>{context.Request.Path}</b>: these are not the droids you are looking for!</p>");
				//return Task.Delay(0);
			});
		}
	}

	internal class MyMimeSet : Microsoft.Owin.StaticFiles.ContentTypes.FileExtensionContentTypeProvider
	{
		public MyMimeSet()
		{
			Mappings[".json"] = "application/json";
			Mappings[".md"] = "text/plain";
		}
	}
}
