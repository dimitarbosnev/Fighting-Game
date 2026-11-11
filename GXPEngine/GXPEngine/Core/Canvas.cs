using System;
using GXPEngine.Core;

namespace GXPEngine
{
	/// <summary>
	/// A Sprite whose texture can be drawn into at runtime. In the browser build the
	/// pixels live in a 2D canvas on the JS side; use EasyDraw for the drawing API
	/// (System.Drawing.Graphics is not available in WebAssembly).
	/// </summary>
	public class Canvas : Sprite
	{
		/// <summary>Creates a transparent canvas of the given size.</summary>
		public Canvas (int width, int height, bool addCollider=true) : base(new Texture2D(width, height), addCollider)
		{
			name = width + "x" + height;
		}

		/// <summary>Creates a canvas initialized with a copy of an image file.</summary>
		public Canvas (string filename, bool addCollider=true) : base(Texture2D.CreateCanvasFromImage(filename), addCollider)
		{
			name = filename;
		}

		/// <summary>The id of the canvas the drawing calls go to.</summary>
		protected uint canvasId {
			get { return _texture.id; }
		}
	}
}
