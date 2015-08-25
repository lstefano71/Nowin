using Microsoft.Owin.FileSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WildHeart.FileSystems;

namespace WildHeart.Owin.FileSystems
{
	public class CompositeFileSystem : IFileSystem
	{
		readonly Dictionary<string, IFileInfo> _fs = new Dictionary<string, IFileInfo>(StringComparer.OrdinalIgnoreCase);
		readonly Dictionary<string, IList<IFileInfo>> _dir = new Dictionary<string, IList<IFileInfo>>(StringComparer.OrdinalIgnoreCase);

		IEnumerable<string> NewSegments(string path)
		{
			var dirs = path.Substring(0, path.LastIndexOf('/')).Split('/');
			var c = "";
			foreach (var cd in dirs) {
				c = Util.CombinePath(c, cd, "");
				c = c.Length > 0 ? c : "/";
				if (!_fs.ContainsKey(c))
					yield return c;
			}

		}

		public CompositeFileSystem(IList<Tuple<string, IFileSystem>> fss)
		{
			var ixs = fss.OrderBy(fs => fs.Item1,StringComparer.OrdinalIgnoreCase)
				.Zip(Enumerable.Range(0, fss.Count), (fs, ix) => ix);

			foreach (var ix in ixs) {
				var fs = fss[ix];
				var k = fs.Item1;
				var n = Util.CombinePath(k, "");
				ProcessPath(fs.Item2, "/", n);
			}

			foreach (var p in _fs.Keys.ToArray()) {
				foreach (var s in NewSegments(p)) {
					if (s.Equals("/"))
						continue;

					var name = s.Substring(s.TrimEnd('/').LastIndexOf('/')).Trim('/');
					_fs[s] = new FakeDir() { Name = name };
				}
			}

			var dirs = from nv in _fs
								 let k = nv.Key.TrimEnd('/')
								 let gk = k.Length > 0 ? k.Substring(0, k.LastIndexOf('/')) : ""
								 group nv by gk.ToLowerInvariant();

			foreach (var g in dirs) {
				var k = g.Key.Length > 0 ? Util.CombinePath(g.Key, "") : "/";
				_dir[k] = g.Select(v => v.Value).ToList();
			}
			return;
		}

		private void ProcessPath(IFileSystem fs, string curroot, string main)
		{
			IEnumerable<IFileInfo> dir;
			var res = fs.TryGetDirectoryContents(curroot, out dir);
			if (!res)
				return;
			var d = Util.CombinePath(main, curroot);

			foreach (var f in dir) {
				var key = f.IsDirectory ? Util.CombinePath(d, f.Name, "") :
					Util.CombinePath(d, f.Name);
				_fs[key] = f;
				if (f.IsDirectory)
					ProcessPath(fs, f.Name, d);
			}
		}

		public bool TryGetDirectoryContents(string subpath, out IEnumerable<IFileInfo> contents)
		{
			IList<IFileInfo> list;

			subpath = Util.CombinePath(subpath, "");

			contents = null;
			if (!_dir.TryGetValue(subpath, out list))
				return false;
			contents = list;
			return true;
		}

		public bool TryGetFileInfo(string subpath, out IFileInfo fileInfo)
		{
			return _fs.TryGetValue(subpath, out fileInfo);
		}

		class FakeDir : IFileInfo
		{
			public bool IsDirectory
			{
				get
				{
					return true;
				}
			}

			public DateTime LastModified
			{
				get
				{
					return DateTime.MinValue;
				}
			}

			public long Length
			{
				get
				{
					return 0;
				}
			}

			public string Name
			{
				get; internal set;
			}

			public string PhysicalPath
			{
				get
				{
					return null;
				}
			}

			public Stream CreateReadStream()
			{
				throw new NotImplementedException();
			}
		}
	}
}
