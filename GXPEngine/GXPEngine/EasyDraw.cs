using System;
using GXPEngine.Web;
using Color = System.Drawing.Color;
using PointF = System.Drawing.PointF;

namespace GXPEngine
{
	public enum CenterMode {Min, Center, Max}

	/// <summary>
	/// Creates an easy-to-use layer on top of .NET's System.Drawing methods.
	/// The settings are fill color, stroke color and text font and alignment, like
	/// Processing (www.processing.org).
	///
	/// In the browser build the drawing happens in a 2D canvas on the JS side
	/// (js/canvas_2d.js), which is uploaded to the GPU before the next frame.
	/// </summary>
	public class EasyDraw : Canvas
	{
		static Font defaultFont = new Font ("Noto Sans", 15);

		public CenterMode HorizontalTextAlign=CenterMode.Min;
		public CenterMode VerticalTextAlign=CenterMode.Max;
		public CenterMode HorizontalShapeAlign=CenterMode.Center;
		public CenterMode VerticalShapeAlign=CenterMode.Center;
		public Font font		{ get; protected set;}
		/// <summary>Stroke (outline) color, used by shapes and lines.</summary>
		public Color strokeColor	{ get; protected set;}
		/// <summary>Fill color, used by shapes and text.</summary>
		public Color fillColor	{ get; protected set;}
		public float strokeWeight	{ get; protected set;}
		protected bool _stroke=true;
		protected bool _fill=true;

		/// <summary>
		/// Creates a new EasyDraw canvas with the given width and height in pixels.
		/// </summary>
		public EasyDraw (int width, int height, bool addCollider=true) : base (width, height, addCollider)
		{
			Initialize ();
		}

		/// <summary>
		/// Creates a new EasyDraw canvas from an image file, which you can then draw on.
		/// </summary>
		public EasyDraw (string filename, bool addCollider=true) : base(filename,addCollider)
		{
			Initialize ();
		}

		void Initialize()
		{
			strokeColor = Color.White;
			fillColor = Color.White;
			strokeWeight = 1;
			font = defaultFont;
		}

		//////////// Setting Font

		/// <summary>
		/// Change the font that is used when rendering text (using the Text method).
		/// </summary>
		public void TextFont(Font newFont)
		{
			font = newFont;
		}

		/// <summary>
		/// Change the font that is used when rendering text (using the Text method).
		/// The name is a CSS font family: a system font, or a .ttf under Assets by its file name.
		/// </summary>
		public void TextFont(string fontName, float pointSize, FontStyle style = FontStyle.Regular)
		{
			font = new Font (fontName, pointSize, style);
		}

		/// <summary>
		/// Change the font size in points that is used when rendering text (using the Text method).
		/// </summary>
		public void TextSize(float pointSize)
		{
			font = new Font (font.Name, pointSize, font.Style, font.Unit);
		}

		//////////// Setting Alignment for text, ellipses and rects

		/// <summary>
		/// Sets the horizontal and vertical alignment of text, used by the Text methods.
		/// </summary>
		public void TextAlign(CenterMode horizontal, CenterMode vertical)
		{
			HorizontalTextAlign = horizontal;
			VerticalTextAlign = vertical;
		}

		/// <summary>
		/// Sets the horizontal and vertical alignment of shapes, used by Rect, Ellipse and Arc.
		/// </summary>
		public void ShapeAlign(CenterMode horizontal, CenterMode vertical)
		{
			HorizontalShapeAlign = horizontal;
			VerticalShapeAlign = vertical;
		}

		//////////// Setting Stroke

		public void NoStroke()
		{
			_stroke=false;
		}

		public void Stroke(Color newColor, int alpha=255)
		{
			strokeColor = Color.FromArgb (alpha, newColor);
			_stroke = true;
		}

		public void Stroke(int grayScale, int alpha=255)
		{
			strokeColor = Color.FromArgb (alpha, grayScale, grayScale, grayScale);
			_stroke = true;
		}

		public void Stroke(int red, int green, int blue, int alpha=255)
		{
			strokeColor = Color.FromArgb (alpha, red, green, blue);
			_stroke = true;
		}

		public void StrokeWeight(float width)
		{
			strokeWeight = width;
			_stroke = true;
		}

		//////////// Setting Fill

		public void NoFill()
		{
			_fill = false;
		}

		public void Fill(Color newColor, int alpha=255)
		{
			fillColor = Color.FromArgb (alpha, newColor);
			_fill = true;
		}

		public void Fill(int grayScale, int alpha=255)
		{
			fillColor = Color.FromArgb (alpha, grayScale, grayScale, grayScale);
			_fill = true;
		}

		public void Fill(int red, int green, int blue, int alpha=255)
		{
			fillColor = Color.FromArgb (alpha, red, green, blue);
			_fill = true;
		}

		//////////// Clear

		/// <summary>
		/// Clear the canvas with a single color.
		/// </summary>
		public void Clear(Color newColor)
		{
			Host.CanvasClear(canvasId, (uint)newColor.ToArgb());
		}

		public void Clear(int grayScale)
		{
			Clear(Color.FromArgb(255, grayScale, grayScale, grayScale));
		}

		public void Clear(int red, int green, int blue, int alpha=255)
		{
			Clear(Color.FromArgb (alpha, red, green, blue));
		}

		/// <summary>
		/// Clear the canvas with a transparent color.
		/// Note that this will clear the canvas, but that you might still not be able to see sprites that are behind this canvas!
		/// </summary>
		public void ClearTransparent() {
			Host.CanvasClear(canvasId, 0);
		}

		//////////// Draw & measure Text

		/// <summary>
		/// Draw text on the canvas, using the currently selected font, at position x,y.
		/// This uses the current TextAlign values (e.g. if both are CenterMode.Center, (x,y) will be at the center of the rendered text).
		/// The text is drawn with the fill color.
		/// </summary>
		public void Text(string text, float x, float y)
		{
			float twidth,theight;
			TextDimensions (text, out twidth, out theight);
			if (HorizontalTextAlign == CenterMode.Max)
			{
				x -= twidth;
			} else if (HorizontalTextAlign == CenterMode.Center)
			{
				x -= twidth / 2;
			}
			if (VerticalTextAlign == CenterMode.Max)
			{
				y -= theight;
			} else if (VerticalTextAlign == CenterMode.Center)
			{
				y -= theight / 2;
			}
			Host.CanvasText(canvasId, text, font.Name, font.SizeInPixels, font.Style, x, y, (uint)fillColor.ToArgb());
		}

		/// <summary>
		/// Draws text on the canvas, aligned to the canvas edges or center according to TextAlign.
		/// Optionally clears the canvas first (by default to transparent).
		/// </summary>
		public void Text(string text, bool clear=false, int clearAlpha=0, int clearRed=0, int clearGreen=0, int clearBlue=0) {
			if (clear) Clear(clearRed, clearGreen, clearBlue, clearAlpha);
			float tx = 0;
			float ty = 0;
			switch (HorizontalTextAlign) {
				case CenterMode.Center:
					tx = _texture.width/2;
					break;
				case CenterMode.Max:
					tx = _texture.width;
					break;
			}
			switch (VerticalTextAlign) {
				case CenterMode.Center:
					ty = _texture.height / 2;
					break;
				case CenterMode.Max:
					ty = _texture.height;
					break;
			}
			Text(text, tx, ty);
		}

		/// <summary>
		/// Returns the width in pixels of a string, when rendered with the current font.
		/// </summary>
		public float TextWidth(string text)
		{
			float width, height;
			TextDimensions(text, out width, out height);
			return width;
		}

		/// <summary>
		/// Returns the height in pixels of a string, when rendered with the current font.
		/// </summary>
		public float TextHeight(string text)
		{
			float width, height;
			TextDimensions(text, out width, out height);
			return height;
		}

		/// <summary>
		/// Returns the width and height in pixels of a string, when rendered with the current font.
		/// </summary>
		public void TextDimensions(string text, out float width, out float height)
		{
			Host.CanvasMeasureText(text, font.Name, font.SizeInPixels, font.Style, out width, out height);
		}

		//////////// Draw Shapes

		/// <summary>
		/// Draw an (axis aligned) rectangle with given width and height, using the current stroke and fill settings.
		/// Uses the current ShapeAlign values to position the rectangle relative to the point (x,y).
		/// </summary>
		public void Rect(float x, float y, float width, float height) {
			ShapeAlign (ref x, ref y, width, height);
			DrawShape(CanvasShape.Rect, x, y, width, height);
		}

		/// <summary>
		/// Draw an (axis aligned) ellipse with given width and height, using the current stroke and fill settings.
		/// Uses the current ShapeAlign values to position the ellipse relative to the point (x,y).
		/// </summary>
		public void Ellipse(float x, float y, float width, float height) {
			ShapeAlign (ref x, ref y, width, height);
			DrawShape(CanvasShape.Ellipse, x, y, width, height);
		}

		/// <summary>
		/// Draw a segment of an ellipse with given width and height (a pie when filled), using the current stroke and fill settings.
		/// Uses the current ShapeAlign values to position the arc relative to the point (x,y).
		/// Angles are in degrees, clockwise from the positive x-axis.
		/// </summary>
		public void Arc(float x, float y, float width, float height, float startAngleDegrees, float sweepAngleDegrees) {
			ShapeAlign (ref x, ref y, width, height);
			DrawShape(CanvasShape.Arc, x, y, width, height, startAngleDegrees, sweepAngleDegrees);
		}

		/// <summary>
		/// Draw a line segment between two points, using the current stroke settings.
		/// </summary>
		public void Line(float x1, float y1, float x2, float y2) {
			if (_stroke) {
				Host.CanvasShape(canvasId, CanvasShape.Line, new float[] { x1, y1, x2, y2 }, 0, (uint)strokeColor.ToArgb(), strokeWeight, false, true);
			}
		}

		/// <summary>
		/// Draw a triangle between three points, using the current stroke and fill settings.
		/// </summary>
		public void Triangle(float x1, float y1, float x2, float y2, float x3, float y3) {
			Polygon(x1,y1,x2,y2,x3,y3);
		}

		/// <summary>
		/// Draw a quad between four points, using the current stroke and fill settings.
		/// </summary>
		public void Quad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) {
			Polygon(x1,y1,x2,y2,x3,y3,x4,y4);
		}

		/// <summary>
		/// Draw a polygon shape between any number of points, using the current stroke and fill settings.
		/// This requires passing in an even number of float coordinates.
		/// </summary>
		public void Polygon(params float[] pt) {
			DrawShape(CanvasShape.Polygon, pt);
		}

		/// <summary>
		/// Draw a polygon shape between any number of points, using the current stroke and fill settings.
		/// </summary>
		public void Polygon(PointF[] pts) {
			float[] pt = new float[pts.Length * 2];
			for (int i = 0; i < pts.Length; i++) {
				pt[2 * i] = pts[i].X;
				pt[2 * i + 1] = pts[i].Y;
			}
			DrawShape(CanvasShape.Polygon, pt);
		}

		void DrawShape(CanvasShape kind, params float[] args) {
			if (!_fill && !_stroke) return;
			Host.CanvasShape(canvasId, kind, args, (uint)fillColor.ToArgb(), (uint)strokeColor.ToArgb(), strokeWeight, _fill, _stroke);
		}

		protected void ShapeAlign(ref float x, ref float y, float width, float height) {
			if (HorizontalShapeAlign == CenterMode.Max)
			{
				x -= width;
			} else if (HorizontalShapeAlign == CenterMode.Center)
			{
				x -= width / 2;
			}
			if (VerticalShapeAlign == CenterMode.Max)
			{
				y -= height;
			} else if (VerticalShapeAlign == CenterMode.Center)
			{
				y -= height / 2;
			}
		}
	}
}
