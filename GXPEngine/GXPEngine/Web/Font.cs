using System;

namespace GXPEngine
{
	/// <summary>Replaces System.Drawing.FontStyle (not available in the browser build).</summary>
	[Flags]
	public enum FontStyle
	{
		Regular = 0,
		Bold = 1,
		Italic = 2,
		Underline = 4,
		Strikeout = 8,
	}

	/// <summary>Replaces System.Drawing.GraphicsUnit for font sizes. Only Pixel and Point matter here.</summary>
	public enum GraphicsUnit
	{
		World,
		Display,
		Pixel,
		Point,
		Inch,
		Document,
		Millimeter,
	}

	/// <summary>
	/// A font description for EasyDraw, replacing System.Drawing.Font. The browser
	/// renders the text: Name is a CSS font family, either a system font or one of
	/// the .ttf files under Assets (the host registers each under its file name,
	/// so "Retro Gaming.ttf" becomes the family "Retro Gaming").
	/// </summary>
	public class Font
	{
		public readonly string Name;
		public readonly float Size;
		public readonly FontStyle Style;
		public readonly GraphicsUnit Unit;

		public Font(string familyName, float emSize, FontStyle style = FontStyle.Regular, GraphicsUnit unit = GraphicsUnit.Point) {
			Name = familyName;
			Size = emSize;
			Style = style;
			Unit = unit;
		}

		public Font(string familyName, float emSize, GraphicsUnit unit) : this(familyName, emSize, FontStyle.Regular, unit) {
		}

		public string OriginalFontName { get { return Name; } }
		public string FontFamily { get { return Name; } }

		/// <summary>The em size in pixels. Like System.Drawing, the default unit is points (1/72 inch at 96 DPI).</summary>
		public float SizeInPixels {
			get {
				switch (Unit) {
					case GraphicsUnit.Pixel:
					case GraphicsUnit.World:
					case GraphicsUnit.Display: return Size;
					case GraphicsUnit.Inch: return Size * 96f;
					case GraphicsUnit.Document: return Size * 96f / 300f;
					case GraphicsUnit.Millimeter: return Size * 96f / 25.4f;
					default: return Size * 96f / 72f;
				}
			}
		}

		public override string ToString() {
			return "[Font: Name=" + Name + ", Size=" + Size + ", Style=" + Style + ", Unit=" + Unit + "]";
		}
	}
}
