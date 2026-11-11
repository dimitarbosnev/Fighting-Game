using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace GXPEngine.Web
{
	/// <summary>
	/// Maps the file paths the game uses to what the browser build provides.
	///
	/// A game's Content/ folder stands in for the old exe folder (bin/Debug): paths
	/// like "Assets/Boxes/Box.png" or "Announcment Maps/../Assets/Text/x.jpg" are
	/// relative to it, exactly as before. GXPEngine.Web.targets then
	///  - publishes media (images, sounds, fonts) to content/ next to index.html, and
	///  - compiles data files (*.tmx, *.tsx, *.txt, *.xml, *.json, *.csv) into the module,
	///    where ContentFiles reads them synchronously.
	/// </summary>
	public static class AssetPath
	{
		/// <summary>The web folder that mirrors the game's Content/ folder, relative to index.html.</summary>
		public const string WebRoot = "content/";

		/// <summary>Forward slashes, no "." segments, ".." resolved.</summary>
		public static string Normalize(string path) {
			if (string.IsNullOrEmpty(path)) return "";
			List<string> parts = new List<string>();
			foreach (string part in path.Replace('\\', '/').Split('/')) {
				if (part.Length == 0 || part == ".") continue;
				if (part == ".." && parts.Count > 0 && parts[parts.Count - 1] != "..") {
					parts.RemoveAt(parts.Count - 1);
				} else {
					parts.Add(part);
				}
			}
			return string.Join("/", parts);
		}

		/// <summary>URL of a published content file, relative to index.html.</summary>
		public static string Url(string path) {
			return WebRoot + Normalize(path);
		}
	}

	/// <summary>
	/// Read-only access to the game's embedded data files (see AssetPath), for code
	/// that used System.IO.File on files next to the exe. The browser build has no
	/// file system: use these instead of File.Exists / File.ReadAllText / new StreamReader.
	/// Paths are relative to Content/, like before they were relative to bin/Debug.
	/// </summary>
	public static class ContentFiles
	{
		const string Prefix = "content/";

		static Dictionary<string, (Assembly assembly, string resource)> _files;

		static Dictionary<string, (Assembly, string)> Files {
			get {
				if (_files != null) return _files;
				_files = new Dictionary<string, (Assembly, string)>(StringComparer.OrdinalIgnoreCase);
				foreach (Assembly assembly in new[] { WebRuntime.GameAssembly, typeof(ContentFiles).Assembly }) {
					if (assembly == null) continue;
					foreach (string name in assembly.GetManifestResourceNames()) {
						// Logical names are "content/<path>"; MSBuild may have written backslashes.
						string key = AssetPath.Normalize(name);
						if (key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) {
							key = key.Substring(Prefix.Length);
							if (!_files.ContainsKey(key)) _files[key] = (assembly, name);
						}
					}
				}
				return _files;
			}
		}

		public static bool Exists(string path) {
			return Files.ContainsKey(AssetPath.Normalize(path));
		}

		/// <summary>Opens an embedded content file. Throws FileNotFoundException if it is not embedded.</summary>
		public static Stream OpenRead(string path) {
			(Assembly assembly, string resource) file;
			if (!Files.TryGetValue(AssetPath.Normalize(path), out file)) {
				throw new FileNotFoundException("Content file not found: " + path +
					" (data files are embedded from the game's Content folder; see GXPEngine.Web.targets)", path);
			}
			return file.assembly.GetManifestResourceStream(file.resource);
		}

		public static string ReadAllText(string path) {
			using (StreamReader reader = new StreamReader(OpenRead(path), Encoding.UTF8)) {
				return reader.ReadToEnd();
			}
		}

		public static string[] ReadAllLines(string path) {
			List<string> lines = new List<string>();
			using (StreamReader reader = new StreamReader(OpenRead(path), Encoding.UTF8)) {
				string line;
				while ((line = reader.ReadLine()) != null) lines.Add(line);
			}
			return lines.ToArray();
		}

		public static byte[] ReadAllBytes(string path) {
			using (Stream stream = OpenRead(path))
			using (MemoryStream copy = new MemoryStream()) {
				stream.CopyTo(copy);
				return copy.ToArray();
			}
		}

		/// <summary>Every embedded content file, as a path relative to Content/.</summary>
		public static IEnumerable<string> All {
			get { return Files.Keys; }
		}
	}
}
