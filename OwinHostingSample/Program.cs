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

namespace OwinHostingSample
{
	static class Program
	{
		static void Main(string[] args)
		{
			var options = new StartOptions {
				ServerFactory = "Nowin",
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

	public class HeaderMiddleware : OwinMiddleware
	{
		readonly System.Net.WebHeaderCollection _add = new System.Net.WebHeaderCollection();
		readonly System.Net.WebHeaderCollection _del = new System.Net.WebHeaderCollection();

		public HeaderMiddleware(OwinMiddleware next)
				: base(next)
		{
		}

		public HeaderMiddleware(OwinMiddleware next, string name, string value)
				: base(next)
		{
			_add.Add(name, value);
		}


		public async override Task Invoke(IOwinContext context)
		{
			await Next.Invoke(context);
			var h = context.Response.Headers;
			foreach (var key in _add.AllKeys) {
				if (!h.ContainsKey(key)) {
					h.Add(key, new[] { _add[key] });
				}
			}
		}
	}

	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			app.Use((context, next) =>
			{
				var req = context.Request;
				context.TraceOutput.WriteLine("{0} {1}{2} {3}", req.Method, req.PathBase, req.Path, req.QueryString);
				return next();
			});

			app.UseErrorPage(new Microsoft.Owin.Diagnostics.ErrorPageOptions { SourceCodeLineCount = 20 });
			app.Use<HeaderMiddleware>("X-stef", "Edge");
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
			var zip3 = new WildHeart.Owin.FileSystems.ZipFileSystem("TestZip.zip");

			var dic = new SortedDictionary<string, IFileSystem>(StringComparer.OrdinalIgnoreCase) {
				{ "/", zip1 }, { "/sub", zip2 }, { "/sub1", zip3 }
			};

			var fs = new WildHeart.Owin.FileSystems.CompositeFileSystem(dic);

			app.UseFileServer(new FileServerOptions {
				//FileSystem = new WildHeart.Owin.FileSystems.ZipFileSystem("TestZip.zip"),
				FileSystem = fs,
				RequestPath = new PathString("/zip"),
				EnableDirectoryBrowsing = true
			});

			app.Run(context => {
				if (context.Request.Path.Value == "/") {
					context.Response.ContentType = "text/plain";
					return context.Response.WriteAsync("Hello World! It's now " + DateTime.Now);
				}

				context.Response.StatusCode = 404;
				return Task.Delay(0);
			});
		}
	}
}
