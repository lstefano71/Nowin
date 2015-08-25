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
		readonly Dictionary<string,IFileInfo>_fs = new Dictionary<string,IFileInfo>(StringComparer.OrdinalIgnoreCase);
		readonly Dictionary<string, IList<IFileInfo>> _dir = new Dictionary<string, IList<IFileInfo>>(StringComparer.OrdinalIgnoreCase);

		public CompositeFileSystem(SortedDictionary<string,IFileSystem> fss)
		{			
			foreach(var k in fss.Keys) {
				var n = Util.CombinePath(k, "");
        ProcessPath(fss[k], "/", n);
			}

			var dirs = from nv in _fs
								 let k = nv.Key.TrimEnd('/')
								 group nv by k.Length > 0 ? k.Substring(0, k.LastIndexOf('/')) : "";
								 
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
			var dirs = d.Split('/');
			var c = "";
      foreach (var cd in dirs) {
				c = Util.CombinePath(c, cd,"");
				c = c.Length > 0 ? c : "/";
				if (!_fs.ContainsKey(c)) {
					_fs[c] = new FakeDir() { Name = cd };
				}
			}

			foreach (var f in dir) {
				var key = f.IsDirectory ? Util.CombinePath(main, curroot, f.Name,"") : 
					Util.CombinePath(main, curroot, f.Name);
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
