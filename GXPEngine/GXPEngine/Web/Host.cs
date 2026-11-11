using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GXPEngine.Web
{
	/// <summary>
	/// Functions the JS host provides to the wasm module (the "engine" import module).
	/// Implemented in host/js/engine.js, renderer_2d.js, canvas_2d.js and audio.js.
	/// Strings cross the boundary as UTF-8 (pointer, byte length) pairs.
	/// </summary>
	internal static unsafe class Host
	{
		const string Module = "engine";

		// ---------------------------------------------------------------- textures
		// Returns a texture id at once. Images listed in content/manifest.json are
		// preloaded, so their size is known immediately (see TextureSize).
		[WasmImportLinkage, DllImport(Module, EntryPoint = "load_texture")]
		static extern uint load_texture(byte* url, int urlLength, uint filter);

		// Writes the texture's width and height (two i32) to `size`; 0x0 if unknown.
		[WasmImportLinkage, DllImport(Module, EntryPoint = "texture_size")]
		static extern void texture_size(uint texture, int* size);

		[WasmImportLinkage, DllImport(Module, EntryPoint = "texture_free")]
		public static extern void TextureFree(uint texture);

		// ---------------------------------------------------------------- canvases (EasyDraw)
		// A canvas is a texture backed by a 2D canvas on the JS side; draw calls
		// mark it dirty and it is re-uploaded before the next render.
		[WasmImportLinkage, DllImport(Module, EntryPoint = "canvas_create")]
		public static extern uint CanvasCreate(int width, int height, uint filter);

		[WasmImportLinkage, DllImport(Module, EntryPoint = "canvas_create_from_image")]
		static extern uint canvas_create_from_image(byte* url, int urlLength, uint filter);

		[WasmImportLinkage, DllImport(Module, EntryPoint = "canvas_clear")]
		public static extern void CanvasClear(uint canvas, uint argb);

		[WasmImportLinkage, DllImport(Module, EntryPoint = "canvas_text")]
		static extern void canvas_text(uint canvas, byte* text, int textLength, byte* font, int fontLength,
			float sizePx, int style, float x, float y, uint argb);

		// Writes the text's width and line height (two f32) to `size`.
		[WasmImportLinkage, DllImport(Module, EntryPoint = "canvas_measure_text")]
		static extern void canvas_measure_text(byte* text, int textLength, byte* font, int fontLength,
			float sizePx, int style, float* size);

		// kind: see CanvasShape. `args` holds kind-specific floats.
		[WasmImportLinkage, DllImport(Module, EntryPoint = "canvas_shape")]
		static extern void canvas_shape(uint canvas, int kind, float* args, int argCount,
			uint fillArgb, uint strokeArgb, float strokeWidth, int flags);

		// ---------------------------------------------------------------- audio
		[WasmImportLinkage, DllImport(Module, EntryPoint = "sound_load")]
		static extern uint sound_load(byte* url, int urlLength, int looping, int streaming);

		[WasmImportLinkage, DllImport(Module, EntryPoint = "sound_play")]
		public static extern uint SoundPlay(uint sound, int paused, float volume, float pan);

		[WasmImportLinkage, DllImport(Module, EntryPoint = "channel_get")]
		public static extern float ChannelGet(uint channel, int property);

		[WasmImportLinkage, DllImport(Module, EntryPoint = "channel_set")]
		public static extern void ChannelSet(uint channel, int property, float value);

		[WasmImportLinkage, DllImport(Module, EntryPoint = "channel_stop")]
		public static extern void ChannelStop(uint channel);

		// ---------------------------------------------------------------- managed wrappers

		public static uint LoadTexture(string url, uint filter) {
			Utf8 u = new Utf8(url);
			fixed (byte* p = u.Bytes) return load_texture(p, u.Length, filter);
		}

		public static void TextureSize(uint texture, out int width, out int height) {
			int* size = stackalloc int[2];
			texture_size(texture, size);
			width = size[0];
			height = size[1];
		}

		public static uint CanvasCreateFromImage(string url, uint filter) {
			Utf8 u = new Utf8(url);
			fixed (byte* p = u.Bytes) return canvas_create_from_image(p, u.Length, filter);
		}

		public static void CanvasText(uint canvas, string text, string font, float sizePx, FontStyle style, float x, float y, uint argb) {
			Utf8 t = new Utf8(text), f = new Utf8(font);
			fixed (byte* pt = t.Bytes, pf = f.Bytes) {
				canvas_text(canvas, pt, t.Length, pf, f.Length, sizePx, (int)style, x, y, argb);
			}
		}

		public static void CanvasMeasureText(string text, string font, float sizePx, FontStyle style, out float width, out float height) {
			Utf8 t = new Utf8(text), f = new Utf8(font);
			float* size = stackalloc float[2];
			fixed (byte* pt = t.Bytes, pf = f.Bytes) {
				canvas_measure_text(pt, t.Length, pf, f.Length, sizePx, (int)style, size);
			}
			width = size[0];
			height = size[1];
		}

		public static void CanvasShape(uint canvas, CanvasShape kind, float[] args, uint fillArgb, uint strokeArgb, float strokeWidth, bool fill, bool stroke) {
			fixed (float* p = args) {
				canvas_shape(canvas, (int)kind, p, args.Length, fillArgb, strokeArgb, strokeWidth, (fill ? 1 : 0) | (stroke ? 2 : 0));
			}
		}

		public static uint SoundLoad(string url, bool looping, bool streaming) {
			Utf8 u = new Utf8(url);
			fixed (byte* p = u.Bytes) return sound_load(p, u.Length, looping ? 1 : 0, streaming ? 1 : 0);
		}

		// UTF-8 bytes of a string; `fixed` on an empty array yields null, which JS reads as "".
		readonly struct Utf8
		{
			public readonly byte[] Bytes;
			public readonly int Length;
			public Utf8(string s) {
				Bytes = Encoding.UTF8.GetBytes(s ?? "");
				Length = Bytes.Length;
			}
		}
	}

	/// <summary>Shapes for Host.CanvasShape, with the floats each one expects.</summary>
	internal enum CanvasShape
	{
		Rect = 0,     // x, y, width, height
		Ellipse = 1,  // x, y, width, height (bounding box)
		Line = 2,     // x1, y1, x2, y2
		Polygon = 3,  // x1, y1, x2, y2, ...
		Arc = 4,      // x, y, width, height, startDegrees, sweepDegrees (a pie when filled)
	}

	/// <summary>Properties for Host.ChannelGet / ChannelSet.</summary>
	internal enum ChannelProperty
	{
		Volume = 0,
		Pan = 1,
		Frequency = 2,
		Paused = 3,   // 0 or 1
		Playing = 4,  // read-only, 0 or 1
	}
}
