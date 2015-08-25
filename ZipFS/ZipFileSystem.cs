using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;
using System.IO;
using System.IO.Compression;
using Microsoft.Owin;

namespace WildHeart.Owin.FileSystems
{
	public class ZipFileSystem : IFileSystem
	{
		string _filename;
		ZipArchive _zip;

		public ZipFileSystem(string fname)
		{
			_filename = GetFullName(fname);
			_zip = ZipFile.OpenRead(_filename);
		}

		public ZipFileSystem(byte[] content)
		{
			_zip = new ZipArchive(new MemoryStream(content));
		}

		public bool TryGetFileInfo(string subpath, out IFileInfo fileInfo)
		{
			fileInfo = null;

			if (subpath.StartsWith("/", StringComparison.Ordinal)) {
				subpath = subpath.Substring(1);
			}
			var entry = _zip.GetEntry(subpath);
			if (entry != null) {
				fileInfo = new ZipFileInfo(entry,subpath);
				return true;
			}
			return false;
		}

		private static string GetFullName(string root)
		{
			var applicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
			return Path.GetFullPath(Path.Combine(applicationBase, root));
		}

		public bool TryGetDirectoryContents(string subpath, out IEnumerable<IFileInfo> contents)
		{
			subpath = WildHeart.FileSystems.Util.CombinePath(subpath, "/");
			if (subpath.StartsWith("/", StringComparison.Ordinal)) {
				subpath = subpath.Substring(1);
			}

			contents = from entry in _zip.Entries
								 where entry.FullName.StartsWith(subpath, StringComparison.OrdinalIgnoreCase)
								 let name = entry.FullName.Substring(subpath.Length)
								 where !string.IsNullOrEmpty(name)
								 where WildHeart.FileSystems.Util.IsFolder(name) || !name.Contains('/')
								 select new ZipFileInfo(entry,subpath);

			var t = contents.ToArray();
			return t.Length > 0;
		}

		private class ZipFileInfo : IFileInfo
		{
			ZipArchiveEntry _entry;
			string _subpath;

			public ZipFileInfo(System.IO.Compression.ZipArchiveEntry entry, string subpath)
			{
				_entry = entry;
				_subpath = subpath;
			}

			public bool IsDirectory
			{
				get { return WildHeart.FileSystems.Util.IsFolder(_entry.FullName); }
			}

			public DateTime LastModified
			{
				get
				{
					return _entry.LastWriteTime.LocalDateTime;
				}
			}

			public long Length
			{
				get
				{
					return _entry.Length;
				}
			}

			public string Name
			{
				get
				{
					var n = _entry.FullName.Substring(_subpath.Length);
					if (IsDirectory)
						n = n.Substring(0, n.Length - 1);
					return n;
				}
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
				return _entry.Open();
			}
		}


	}

}
