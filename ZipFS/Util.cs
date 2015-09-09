using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildHeart.FileSystems
{
	public static class Util
	{
		static char _sep = '/';
		public static bool IsFolder(string name)
		{
			return name[name.Length - 1] == _sep;
		}

		public static string CombinePath(params string[] paths)
		{
			var r = string.Empty;
			foreach(var s in paths) {
				if (string.IsNullOrEmpty(r))
					r = s;
				else {
					r = r.TrimEnd(_sep) + _sep + s.TrimStart(_sep);
				}
			}
			return r;
		}
	}
}
