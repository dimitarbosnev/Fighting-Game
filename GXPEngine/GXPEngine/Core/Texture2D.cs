using System;
using System.Collections.Generic;
using GXPEngine.Web;

namespace GXPEngine.Core
{
	/// <summary>
	/// A texture owned by the JS renderer, referred to by id. Image files are
	/// preloaded by the host (from content/manifest.json), so width and height are
	/// known as soon as the texture is created. Blank textures (new Texture2D(w, h))
	/// are canvases that EasyDraw draws into.
	/// </summary>
	public class Texture2D
	{
		private static Dictionary<string, Texture2D> LoadCache = new Dictionary<string, Texture2D>();
		private static Texture2D lastBound = null;

		private uint _id;
		private int _width;
		private int _height;
		private string _filename = "";
		private bool _isCanvas;
		private bool _freed;
		private int count = 0;
		private bool stayInCache = false;

		//------------------------------------------------------------------------------------------------------------------------
		//														Texture2D()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>Creates a blank (transparent) canvas texture.</summary>
		public Texture2D (int width, int height) {
			if (width == 0) if (height == 0) return;
			_id = Host.CanvasCreate(width, height, Filter);
			_width = width;
			_height = height;
			_isCanvas = true;
		}

		public Texture2D (string filename) {
			Load (filename);
		}

		/// <summary>Creates a canvas texture initialized with a copy of an image file.</summary>
		public static Texture2D CreateCanvasFromImage(string filename) {
			Texture2D texture = new Texture2D(0, 0);
			texture._id = Host.CanvasCreateFromImage(AssetPath.Url(filename), Filter);
			Host.TextureSize(texture._id, out texture._width, out texture._height);
			if (texture._width <= 0 || texture._height <= 0) {
				throw new Exception("Image " + filename + " cannot be found.");
			}
			texture._isCanvas = true;
			return texture;
		}

		static uint Filter {
			get { return Game.main != null && Game.main.PixelArt ? 1u : 0u; } // 0 linear, 1 nearest
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														GetInstance()
		//------------------------------------------------------------------------------------------------------------------------
		public static Texture2D GetInstance (string filename, bool keepInCache=false) {
			Texture2D tex2d;
			if (!LoadCache.TryGetValue(filename, out tex2d)) {
				tex2d = new Texture2D(filename);
				LoadCache[filename] = tex2d;
			}
			tex2d.stayInCache |= keepInCache; // setting it once to true keeps it in cache
			tex2d.count ++;
			return tex2d;
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														RemoveInstance()
		//------------------------------------------------------------------------------------------------------------------------
		public static void RemoveInstance (string filename)
		{
			Texture2D tex2D;
			if (LoadCache.TryGetValue(filename, out tex2D)) {
				tex2D.count --;
				if (tex2D.count == 0 && !tex2D.stayInCache) {
					LoadCache.Remove (filename);
					tex2D.Free();
				}
			}
		}

		public void Dispose () {
			if (_filename != "") {
				Texture2D.RemoveInstance (_filename);
			} else if (_isCanvas) {
				Free();
			}
		}

		void Free() {
			if (_freed || _id == 0) return;
			_freed = true;
			Host.TextureFree(_id);
			if (lastBound == this) lastBound = null;
		}

		/// <summary>The renderer's id for this texture.</summary>
		public uint id {
			get { return _id; }
		}

		public string filename {
			get { return _filename; }
		}

		public int width {
			get { return _width; }
		}

		public int height {
			get { return _height; }
		}

		/// <summary>True for blank and EasyDraw textures, which can be drawn into.</summary>
		public bool isCanvas {
			get { return _isCanvas; }
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Bind()
		//------------------------------------------------------------------------------------------------------------------------
		public void Bind() {
			GLContext.current?.BindTexture(_id);
			lastBound = this;
		}

		public void Unbind() {
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Load()
		//------------------------------------------------------------------------------------------------------------------------
		private void Load(string filename) {
			_filename = filename;
			_id = Host.LoadTexture(AssetPath.Url(filename), Filter);
			Host.TextureSize(_id, out _width, out _height);
			if (_width <= 0 || _height <= 0) {
				throw new Exception("Image " + filename + " cannot be found. (Expected at " + AssetPath.Url(filename) + ")");
			}
		}

		/// <summary>No-op: the browser build keeps pixel data on the JS side.</summary>
		public void UpdateGLTexture() {
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Clone()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// A shallow clone shares the same texture. Deep copies need pixel access,
		/// which the browser build does not have.
		/// </summary>
		public Texture2D Clone (bool deepCopy=false) {
			if (deepCopy) throw new NotSupportedException("Texture2D.Clone(deepCopy: true) is not supported in the browser build.");
			Texture2D newTexture = new Texture2D(0, 0);
			newTexture._id = _id;
			newTexture._width = _width;
			newTexture._height = _height;
			return newTexture;
		}

		/// <summary>Texture wrapping is not supported by the web renderer (it always clamps).</summary>
		public bool wrap {
			set { }
		}

		public static string GetDiagnostics() {
			string output = "";
			output += "Number of textures in cache: " + LoadCache.Keys.Count+'\n';
			return output;
		}
	}
}
