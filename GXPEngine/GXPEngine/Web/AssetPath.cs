using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace GXPEngine.Web
{
	/// <summary>
	/// Turns the file paths the game uses (relative to the old exe folder, e.g.
	/// "Assets/Boxes/Box.png" or "Announcment Maps/../Assets/Text/x.jpg") into
	/// what the browser build provides:
	///  - media files are served from assets/ next to index.html (Url), and
	///  - Tiled levels (*.tmx) are embedded in the module (OpenLevel).
	/// </summary>
	public static class AssetPath
	{
		/// <summary>The web folder the build copies GXPEngine/Assets into, relative to index.html.</summary>
		public const string WebRoot = "assets/";

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

		/// <summary>
		/// URL of a game file, relative to index.html. The game's "Assets/" folder
		/// is published as assets/, so "Assets/Boxes/Box.png" becomes "assets/Boxes/Box.png".
		/// </summary>
		public static string Url(string path) {
			string normalized = Normalize(path);
			if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) {
				normalized = normalized.Substring("Assets/".Length);
			}
			return WebRoot + normalized;
		}

		static Dictionary<string, string> _levels;

		/// <summary>Opens a Tiled level, e.g. "StartScreen.tmx" or "Announcment Maps/Round1.tmx".</summary>
		public static Stream OpenLevel(string path) {
			Assembly assembly = typeof(AssetPath).Assembly;
			if (_levels == null) {
				// Logical names are "levels/<path>"; MSBuild may have written backslashes.
				_levels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				foreach (string name in assembly.GetManifestResourceNames()) {
					string key = Normalize(name);
					if (key.StartsWith("levels/", StringComparison.OrdinalIgnoreCase)) {
						_levels[key.Substring("levels/".Length)] = name;
					}
				}
			}
			string resource;
			if (!_levels.TryGetValue(Normalize(path), out resource)) {
				throw new FileNotFoundException("Level not found: " + path + " (levels are embedded from GXPEngine/Levels)");
			}
			return assembly.GetManifestResourceStream(resource);
		}
	}
}
