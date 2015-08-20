using System;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin;

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

	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			app.UseFileServer(new FileServerOptions {
				FileSystem = new PhysicalFileSystem("."),
				RequestPath = new PathString("/files"),
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
