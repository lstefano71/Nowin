using Microsoft.Owin;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildHeart.Owin.Middleware
{
	public class HeaderMiddleware : OwinMiddleware
	{
		readonly WebHeaderCollection _add = new WebHeaderCollection();
		readonly WebHeaderCollection _del = new WebHeaderCollection();

		public HeaderMiddleware(OwinMiddleware next)
				: base(next)
		{
		}

		public HeaderMiddleware(OwinMiddleware next, string name, string value)
				: base(next)
		{
			Add(name, value);
		}

		public void Add(string name, string value)
		{
			_add.Add(name, value);
		}

		public void Del(string name)
		{
			_del.Add(name);
		}

		public async override Task Invoke(IOwinContext context)
		{
			await Next.Invoke(context);
			var h = context.Response.Headers;

			foreach (var key in _add.AllKeys) {
				h[key] = _add[key];
			}

			foreach (var key in _del.AllKeys) {
				if (h.ContainsKey(key)) {
					h.Remove(key);
				}
			}

		}
	}
}
