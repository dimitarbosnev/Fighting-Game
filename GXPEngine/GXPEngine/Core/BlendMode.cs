using System;

namespace GXPEngine
{
	/// <summary>
	/// Blend modes from the desktop engine. The web renderer (js/renderer_2d.js) has
	/// a single straight-alpha blend pipeline, so every mode currently renders as
	/// NORMAL; the API is kept so existing code compiles unchanged.
	/// </summary>
	public class BlendMode
	{
		public static readonly BlendMode NORMAL = new BlendMode ("Normal", () => { });
		public static readonly BlendMode PREMULTIPLIED = new BlendMode ("Premultiplied", () => { });
		public static readonly BlendMode MULTIPLY = new BlendMode ("Multiply", () => { });
		public static readonly BlendMode LIGHTING = new BlendMode ("Lighting", () => { });
		public static readonly BlendMode ADDITIVE = new BlendMode ("Additive", () => { });
		public static readonly BlendMode FILLEMPTY = new BlendMode ("Fill", () => { });

		public delegate void Action();

		/// <summary>Called before a sprite using this mode is drawn.</summary>
		public readonly Action enable;

		public readonly string label;

		public BlendMode (string pLabel, Action pEnable)
		{
			if (pEnable == null) {
				throw new Exception ("Enabled action cannot be null");
			}
			enable = pEnable;
			label = pLabel;
		}

		public override string ToString ()
		{
			return label;
		}
	}
}
