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
			Console.WriteLine($"{context.Request.Path}: {_timer.ElapsedMilliseconds}ms");
		}
	}

	public sealed class GZipMiddleware
	{
		private readonly Func<IDictionary<string, object>, Task> next;

		public GZipMiddleware(Func<IDictionary<string, object>, Task> next)
		{
			this.next = next;
		}

		public async Task Invoke(IDictionary<string, object> environment)
		{
			var context = new OwinContext(environment);

			// Verifies that the calling client supports gzip encoding.
			//if (!(from encoding in context.Request.Headers.GetValues("Accept-Encoding") ?? Enumerable.Empty<string>()
			if (!(from encoding in context.Request.Headers["Accept-Encoding"].Split(',') ?? Enumerable.Empty<string>()
						where String.Equals(encoding.Trim(), "gzip", StringComparison.Ordinal)
						select encoding).Any()) {
				await next(environment);
				return;
			}

			// Replaces the response stream by a memory stream
			// and keeps track of the real response stream.
			var body = context.Response.Body;
			context.Response.Body = new MemoryStream();

			try {
				await next(environment);

				// Verifies that the response stream is still a readable and seekable stream.
				if (!context.Response.Body.CanSeek || !context.Response.Body.CanRead) {
					throw new InvalidOperationException("The response stream has been replaced by an unreadable or unseekable stream.");
				}

				// Determines if the response stream meets the length requirements to be gzipped.
				if (context.Response.Body.Length >= 4096) {
					context.Response.Headers["Content-Encoding"] = "gzip";

					// Determines if chunking can be safely used.
					if (String.Equals(context.Request.Protocol, "HTTP/1.1", StringComparison.Ordinal)) {
						context.Response.Headers["Transfer-Encoding"] = "chunked";

						// Opens a new GZip stream pointing directly to the real response stream.
						using (var gzip = new GZipStream(body, CompressionMode.Compress, leaveOpen: true)) {
							// Rewinds the memory stream and copies it to the GZip stream.
							context.Response.Body.Seek(0, SeekOrigin.Begin);
							await context.Response.Body.CopyToAsync(gzip, 81920, context.Request.CallCancelled);
						}

						return;
					}

					// Opens a new buffer to determine the gzipped response stream length.
					using (var buffer = new MemoryStream()) {
						// Opens a new GZip stream pointing to the buffer stream.
						using (var gzip = new GZipStream(buffer, CompressionMode.Compress, leaveOpen: true)) {
							// Rewinds the memory stream and copies it to the GZip stream.
							context.Response.Body.Seek(0, SeekOrigin.Begin);
							await context.Response.Body.CopyToAsync(gzip, 81920, context.Request.CallCancelled);
						}

						// Rewinds the buffer stream and copies it to the real stream.
						// See http://blogs.msdn.com/b/bclteam/archive/2006/05/10/592551.aspx
						// to see why the buffer is only read after the GZip stream has been disposed.
						buffer.Seek(0, SeekOrigin.Begin);
						context.Response.ContentLength = buffer.Length;
						await buffer.CopyToAsync(body, 81920, context.Request.CallCancelled);
					}

					return;
				}

				// Rewinds the memory stream and copies it to the real response stream.
				context.Response.Body.Seek(0, SeekOrigin.Begin);
				context.Response.ContentLength = context.Response.Body.Length;
				await context.Response.Body.CopyToAsync(body, 81920, context.Request.CallCancelled);
			} finally {
				// Restores the real stream in the environment dictionary.
				context.Response.Body = body;
			}
		}
	}
	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			app.UseErrorPage();
			app.UseSendFileFallback();
			app.Use<TimerMiddleware>();

			app.UseStaticCompression();

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
