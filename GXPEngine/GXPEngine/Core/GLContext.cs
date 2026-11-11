using System;
using GXPEngine.Web;

namespace GXPEngine.Core {

	class WindowSize {
		public static WindowSize instance = new WindowSize();
		public int width, height;
	}

	/// <summary>
	/// The engine's link to the "window". In the browser build there is no window
	/// or OpenGL: the JS host owns the canvas and the frame loop, and this class
	///  - steps the game at a fixed rate (targetFps) from the host's frame callback,
	///  - turns the input the host writes into memory into GXPEngine key/mouse state, and
	///  - renders by converting every quad into a sprite instance for the WebGPU renderer.
	/// The game's logical size (width/height) is scaled uniformly to fit the canvas
	/// and centered, with black bars where the aspect ratios differ.
	/// Viewports (Game.SetViewport, used by Camera windows) clip quads on the CPU.
	/// </summary>
	public class GLContext {

		const int MAXKEYS = 65535;
		const int MAXBUTTONS = 255;

		private static bool[] keys = new bool[MAXKEYS+1];
		private static bool[] keydown = new bool[MAXKEYS+1];
		private static bool[] keyup = new bool[MAXKEYS+1];
		private static bool[] buttons = new bool[MAXBUTTONS+1];
		private static bool[] mousehits = new bool[MAXBUTTONS+1];
		private static bool[] mouseup = new bool[MAXBUTTONS+1];
		private static int keyPressedCount = 0;
		private static bool anyKeyDown = false;

		public static int mouseX = 0;
		public static int mouseY = 0;

		/// <summary>The context the host's frames are delivered to (set by Run).</summary>
		internal static GLContext current;

		private Game _owner;
		private static SoundSystem _soundSystem;

		private int _targetFrameRate = 60;
		private double _accumulatedMs = 0;
		private double _lastFPSTime = 0;
		private int _frameCount = 0;
		private int _lastFPS = 0;
		private bool _running = false;

		// Logical game pixels -> canvas pixels: canvas = logical * _screenScale + _screenOffset.
		private float _screenScale = 1;
		private float _screenOffsetX = 0;
		private float _screenOffsetY = 0;

		// Viewport clipping (SetScissor): logical rectangle (top-left origin) and its canvas-pixel bounds.
		private bool _scissorEnabled = false;
		private int _scissorX, _scissorY, _scissorWidth, _scissorHeight;
		private bool _clipping = false;
		private float _clipLeft, _clipTop, _clipRight, _clipBottom;

		// Render state: a 2D affine matrix stack (x' = a*x + c*y + tx, y' = b*x + d*y + ty).
		private float[] _stack = new float[6 * 64];
		private int _top = 0;
		private uint _color = 0xFFFFFFFF; // RGBA8, r in the lowest byte
		private uint _texture = 0;

		// GXPEngine mouse buttons follow GLFW (0 left, 1 right, 2 middle); the host
		// reports MouseEvent.button order (0 left, 1 middle, 2 right).
		static readonly int[] MouseButtonFromHost = { 0, 2, 1, 3, 4 };

		//------------------------------------------------------------------------------------------------------------------------
		//														GLContext()
		//------------------------------------------------------------------------------------------------------------------------
		public GLContext (Game owner) {
			_owner = owner;
			_lastFPS = _targetFrameRate;
		}

		public int width {
			get { return WindowSize.instance.width; }
		}

		public int height {
			get { return WindowSize.instance.height; }
		}

		public static SoundSystem soundSystem
		{
			get
			{
				if (_soundSystem == null) {
					InitializeSoundSystem ();
				}
				return _soundSystem;
			}
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														CreateWindow()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the logical size used by all game logic. The canvas size is decided by
		/// the page (index.html), so fullScreen, vSync and the real size are ignored.
		/// </summary>
		public void CreateWindow(int width, int height, bool fullScreen, bool vSync, int realWidth, int realHeight) {
			WindowSize.instance.width = width;
			WindowSize.instance.height = height;
			InitializeSoundSystem ();
		}

		private static void InitializeSoundSystem() {
			if (_soundSystem != null) return;
			_soundSystem = new WebSoundSystem();
			_soundSystem.Init();
		}

		/// <summary>The cursor belongs to the page; this has no effect in the browser.</summary>
		public void ShowCursor (bool enable) {
		}

		/// <summary>The browser always syncs to the display; the game still steps at targetFps.</summary>
		public void SetVSync(bool enableVSync) {
		}

		/// <summary>
		/// Restricts drawing to a rectangle, in logical pixels with OpenGL's bottom-left
		/// origin (Game.SetViewport converts for you). Covering the whole screen turns it off.
		/// Clipping happens per quad on the CPU, since the web renderer has no scissor test.
		/// </summary>
		public void SetScissor(int x, int y, int width, int height) {
			int screenWidth = WindowSize.instance.width, screenHeight = WindowSize.instance.height;
			_scissorEnabled = !(x <= 0 && y <= 0 && x + width >= screenWidth && y + height >= screenHeight);
			_scissorX = x;
			_scissorY = screenHeight - y - height; // to top-left origin
			_scissorWidth = width;
			_scissorHeight = height;
			UpdateClip();
		}

		// The scissor rectangle in canvas pixels, for EmitQuad.
		void UpdateClip() {
			_clipping = _scissorEnabled;
			_clipLeft = _scissorX * _screenScale + _screenOffsetX;
			_clipTop = _scissorY * _screenScale + _screenOffsetY;
			_clipRight = (_scissorX + _scissorWidth) * _screenScale + _screenOffsetX;
			_clipBottom = (_scissorY + _scissorHeight) * _screenScale + _screenOffsetY;
		}

		/// <summary>Stops the game: no more updates or rendering. The page stays open.</summary>
		public void Close() {
			_running = false;
			_soundSystem?.Deinit();
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Run()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Starts the game. Unlike the desktop version this returns immediately: the
		/// JS host calls engine_frame every animation frame, which ends up in Frame().
		/// </summary>
		public void Run() {
			_running = true;
			current = this;
			Time.Reset();
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Frame()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// One host frame: collect input, run as many fixed game steps as the elapsed
		/// time calls for (game logic counts frames, so it must run at targetFps no
		/// matter the display's refresh rate), then render once.
		/// </summary>
		internal void Frame(float dtSeconds) {
			PollInput();
			if (!_running) return;

			double stepMs = 1000.0 / _targetFrameRate;
			double dtMs = dtSeconds * 1000.0;
			// Absorb vsync jitter so a display at targetFps runs exactly one step per frame.
			if (Math.Abs(dtMs - stepMs) < 1.0) dtMs = stepMs;
			_accumulatedMs = Math.Min(_accumulatedMs + dtMs, stepMs * 8);

			while (_running && _accumulatedMs >= stepMs) {
				_accumulatedMs -= stepMs;
				Time.Advance(stepMs);

				_frameCount++;
				if (Time.time - _lastFPSTime > 1000) {
					_lastFPS = (int)(_frameCount / ((Time.time - _lastFPSTime) / 1000.0f));
					_lastFPSTime = Time.time;
					_frameCount = 0;
				}

				UpdateMouseInput();
				_owner.Step();
				_soundSystem.Step();

				ResetHitCounters();
				Time.newFrame();
			}

			if (_running) Display();
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Display()
		//------------------------------------------------------------------------------------------------------------------------
		private void Display () {
			float screenWidth = WebRuntime.ScreenWidth;
			float screenHeight = WebRuntime.ScreenHeight;
			if (screenWidth <= 0 || screenHeight <= 0) {
				screenWidth = WindowSize.instance.width;
				screenHeight = WindowSize.instance.height;
			}
			// Fit the logical screen inside the canvas without distorting it, centered.
			int gameWidth = WindowSize.instance.width, gameHeight = WindowSize.instance.height;
			_screenScale = Math.Min(screenWidth / gameWidth, screenHeight / gameHeight);
			_screenOffsetX = (screenWidth - gameWidth * _screenScale) * 0.5f;
			_screenOffsetY = (screenHeight - gameHeight * _screenScale) * 0.5f;
			UpdateClip();

			_top = 0;
			_stack[0] = _screenScale; _stack[1] = 0;
			_stack[2] = 0; _stack[3] = _screenScale;
			_stack[4] = _screenOffsetX; _stack[5] = _screenOffsetY;
			_color = 0xFFFFFFFF;
			_texture = 0;

			WebRuntime.SetClearColor(0, 0, 0, 1);
			_owner.Render(this);
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														SetColor()
		//------------------------------------------------------------------------------------------------------------------------
		public void SetColor (byte r, byte g, byte b, byte a) {
			_color = (uint)r | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24);
		}

		/// <summary>Selects the texture for the following DrawQuad calls (see Texture2D.Bind).</summary>
		internal void BindTexture(uint texture) {
			_texture = texture;
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														PushMatrix()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>Multiplies the current transform by a GXPEngine 4x4 (column-major) matrix and pushes it.</summary>
		public void PushMatrix(float[] matrix) {
			if (_top + 6 >= _stack.Length) Array.Resize(ref _stack, _stack.Length * 2);
			int p = _top, n = _top + 6;
			float a = _stack[p], b = _stack[p + 1], c = _stack[p + 2], d = _stack[p + 3], tx = _stack[p + 4], ty = _stack[p + 5];
			float m0 = matrix[0], m1 = matrix[1], m4 = matrix[4], m5 = matrix[5], m12 = matrix[12], m13 = matrix[13];
			_stack[n] = a * m0 + c * m1;
			_stack[n + 1] = b * m0 + d * m1;
			_stack[n + 2] = a * m4 + c * m5;
			_stack[n + 3] = b * m4 + d * m5;
			_stack[n + 4] = a * m12 + c * m13 + tx;
			_stack[n + 5] = b * m12 + d * m13 + ty;
			_top = n;
		}

		public void PopMatrix() {
			if (_top >= 6) _top -= 6;
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														DrawQuad()
		//------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Draws a quad given as 4 corners (top-left, top-right, bottom-right, bottom-left)
		/// in the current transform, with matching UVs, the bound texture and current color.
		/// The renderer draws rectangles, so the quad becomes center + size + rotation;
		/// a mirrored transform turns into a negative height. Skew (from rotating a
		/// non-uniformly scaled parent) cannot be represented and is dropped.
		/// </summary>
		public void DrawQuad(float[] vertices, float[] uv) {
			DrawQuad(vertices, 0, uv, 0);
		}

		/// <summary>
		/// DrawQuad for quads stored back to back in larger arrays (SpriteBatch):
		/// 8 floats from vertexOffset and 8 from uvOffset.
		/// </summary>
		public void DrawQuad(float[] vertices, int vertexOffset, float[] uv, int uvOffset) {
			int p = _top;
			float a = _stack[p], b = _stack[p + 1], c = _stack[p + 2], d = _stack[p + 3], tx = _stack[p + 4], ty = _stack[p + 5];
			int v = vertexOffset;

			// Corners 0 (top-left), 1 (top-right) and 3 (bottom-left), in canvas pixels.
			float x0 = a * vertices[v] + c * vertices[v + 1] + tx, y0 = b * vertices[v] + d * vertices[v + 1] + ty;
			float x1 = a * vertices[v + 2] + c * vertices[v + 3] + tx, y1 = b * vertices[v + 2] + d * vertices[v + 3] + ty;
			float x3 = a * vertices[v + 6] + c * vertices[v + 7] + tx, y3 = b * vertices[v + 6] + d * vertices[v + 7] + ty;

			EmitQuad(_texture, x0, y0, x1 - x0, y1 - y0, x3 - x0, y3 - y0,
				uv[uvOffset], uv[uvOffset + 1], uv[uvOffset + 4], uv[uvOffset + 5], _color);
		}

		/// <summary>
		/// Draws a solid line, like Gizmos does. With globalCoords the points are in screen
		/// space (logical game pixels); otherwise in the current transform. The width is in
		/// logical pixels, at least one canvas pixel. Color is 0xAARRGGBB.
		/// </summary>
		public void DrawLine(float x1, float y1, float x2, float y2, uint argb, float width, bool globalCoords = true) {
			float s = _screenScale;
			float ax, ay, bx, by;
			if (globalCoords) {
				ax = x1 * s + _screenOffsetX; ay = y1 * s + _screenOffsetY;
				bx = x2 * s + _screenOffsetX; by = y2 * s + _screenOffsetY;
			} else {
				int p = _top;
				float a = _stack[p], b = _stack[p + 1], c = _stack[p + 2], d = _stack[p + 3], tx = _stack[p + 4], ty = _stack[p + 5];
				ax = a * x1 + c * y1 + tx; ay = b * x1 + d * y1 + ty;
				bx = a * x2 + c * y2 + tx; by = b * x2 + d * y2 + ty;
			}
			float dx = bx - ax, dy = by - ay;
			float length = (float)Math.Sqrt(dx * dx + dy * dy);
			if (length == 0) return;
			// Perpendicular of half the line width (at least one canvas pixel, like GL lines).
			float half = Math.Max(width * s, 1) * 0.5f;
			float nx = -dy / length * half, ny = dx / length * half;
			EmitQuad(0, ax - nx, ay - ny, dx, dy, nx * 2, ny * 2, 0, 0, 1, 1, ArgbToRgba(argb));
		}

		/// <summary>
		/// Fills the current viewport (see SetScissor / Game.SetViewport), or the whole
		/// game screen, with black, like glClear did for camera windows.
		/// </summary>
		public void Clear() {
			float left = _clipping ? _clipLeft : _screenOffsetX;
			float top = _clipping ? _clipTop : _screenOffsetY;
			float right = _clipping ? _clipRight : _screenOffsetX + WindowSize.instance.width * _screenScale;
			float bottom = _clipping ? _clipBottom : _screenOffsetY + WindowSize.instance.height * _screenScale;
			if (right <= left || bottom <= top) return;
			WebRuntime.PushInstance(0, (left + right) * 0.5f, (top + bottom) * 0.5f, right - left, bottom - top,
				0, 0, 1, 1, 0, 0xFF000000);
		}

		static uint ArgbToRgba(uint argb) {
			uint a = (argb >> 24) & 0xFF, r = (argb >> 16) & 0xFF, g = (argb >> 8) & 0xFF, b = argb & 0xFF;
			return r | (g << 8) | (b << 16) | (a << 24);
		}

		/// <summary>
		/// Emits the parallelogram p0 + s*U + t*V (s, t in 0..1, canvas pixels) as one
		/// sprite instance, with UV (u0, v0) at s = t = 0 and (u1, v1) at s = t = 1.
		/// While a viewport is set, axis-aligned quads are cut to it (UVs adjusted),
		/// and rotated ones are only dropped when entirely outside.
		/// </summary>
		void EmitQuad(uint texture, float x0, float y0, float ux, float uy, float vx, float vy,
			float u0, float v0, float u1, float v1, uint rgba) {
			if (_clipping && !ClipQuad(ref x0, ref y0, ref ux, ref uy, ref vx, ref vy, ref u0, ref v0, ref u1, ref v1)) return;

			float w = (float)Math.Sqrt(ux * ux + uy * uy);
			float h = (float)Math.Sqrt(vx * vx + vy * vy);
			if (w == 0 || h == 0) return;
			float rotation = (float)Math.Atan2(uy, ux);
			if (ux * vy - uy * vx < 0) h = -h;

			WebRuntime.PushInstance(texture,
				x0 + (ux + vx) * 0.5f, y0 + (uy + vy) * 0.5f, w, h,
				u0, v0, u1, v1, rotation, rgba);
		}

		// Returns false if the quad is entirely outside the clip rectangle.
		bool ClipQuad(ref float x0, ref float y0, ref float ux, ref float uy, ref float vx, ref float vy,
			ref float u0, ref float v0, ref float u1, ref float v1) {
			const float eps = 0.001f;
			bool uHorizontal = Math.Abs(uy) <= eps * Math.Abs(ux), vVertical = Math.Abs(vx) <= eps * Math.Abs(vy);
			bool uVertical = Math.Abs(ux) <= eps * Math.Abs(uy), vHorizontal = Math.Abs(vy) <= eps * Math.Abs(vx);

			if (!((uHorizontal && vVertical) || (uVertical && vHorizontal))) {
				// Rotated: keep it if its bounding box touches the clip rectangle.
				float minX = x0 + Math.Min(0, ux) + Math.Min(0, vx), maxX = x0 + Math.Max(0, ux) + Math.Max(0, vx);
				float minY = y0 + Math.Min(0, uy) + Math.Min(0, vy), maxY = y0 + Math.Max(0, uy) + Math.Max(0, vy);
				return maxX > _clipLeft && minX < _clipRight && maxY > _clipTop && minY < _clipBottom;
			}

			// Axis-aligned: find the s and t ranges inside the clip rectangle.
			float sMin = 0, sMax = 1, tMin = 0, tMax = 1;
			if (uHorizontal) {
				if (!Range(x0, ux, _clipLeft, _clipRight, ref sMin, ref sMax)) return false;
				if (!Range(y0, vy, _clipTop, _clipBottom, ref tMin, ref tMax)) return false;
			} else {
				if (!Range(y0, uy, _clipTop, _clipBottom, ref sMin, ref sMax)) return false;
				if (!Range(x0, vx, _clipLeft, _clipRight, ref tMin, ref tMax)) return false;
			}
			if (sMin <= 0 && sMax >= 1 && tMin <= 0 && tMax >= 1) return true;

			x0 += sMin * ux + tMin * vx;
			y0 += sMin * uy + tMin * vy;
			ux *= sMax - sMin; uy *= sMax - sMin;
			vx *= tMax - tMin; vy *= tMax - tMin;
			float du = u1 - u0, dv = v1 - v0;
			u1 = u0 + du * sMax; u0 += du * sMin;
			v1 = v0 + dv * tMax; v0 += dv * tMin;
			return true;
		}

		// Narrows [min, max] to the parameters where start + t * delta lies in [low, high].
		static bool Range(float start, float delta, float low, float high, ref float min, ref float max) {
			if (delta == 0) return start >= low && start <= high;
			float a = (low - start) / delta, b = (high - start) / delta;
			min = Math.Max(min, Math.Min(a, b));
			max = Math.Min(max, Math.Max(a, b));
			return max > min;
		}

		//------------------------------------------------------------------------------------------------------------------------
		//														Input
		//------------------------------------------------------------------------------------------------------------------------
		public static bool GetKey(int key) {
			return keys[key];
		}

		public static bool GetKeyDown(int key) {
			return keydown[key];
		}

		public static bool GetKeyUp(int key) {
			return keyup[key];
		}

		public static bool AnyKey() {
			return keyPressedCount > 0;
		}

		public static bool AnyKeyDown() {
			return anyKeyDown;
		}

		public static bool GetMouseButton(int button) {
			return buttons[button];
		}

		public static bool GetMouseButtonDown(int button) {
			return mousehits[button];
		}

		public static bool GetMouseButtonUp(int button) {
			return mouseup[button];
		}

		/// <summary>
		/// Copies the host's Input_State into the GXPEngine arrays. Presses and releases
		/// are accumulated until a game step consumes them (ResetHitCounters), so a
		/// tap is not lost on a host frame that runs no step (e.g. on a 144 Hz display).
		/// </summary>
		static void PollInput() {
			int[] codes = KeyMap.Codes;
			int count = 0;
			for (int i = 1; i < codes.Length; i++) {
				byte state = WebRuntime.KeyState(i);
				int key = codes[i];
				if ((state & WebRuntime.PRESSED) != 0) { keydown[key] = true; anyKeyDown = true; }
				if ((state & WebRuntime.RELEASED) != 0) keyup[key] = true;
				keys[key] = (state & WebRuntime.DOWN) != 0;
				if (keys[key]) count++;
			}
			keyPressedCount = count;

			for (int i = 0; i < MouseButtonFromHost.Length; i++) {
				byte state = WebRuntime.MouseButtonState(i);
				int button = MouseButtonFromHost[i];
				if ((state & WebRuntime.PRESSED) != 0) mousehits[button] = true;
				if ((state & WebRuntime.RELEASED) != 0) mouseup[button] = true;
				buttons[button] = (state & WebRuntime.DOWN) != 0;
			}
		}

		public static void ResetHitCounters() {
			Array.Clear (keydown, 0, MAXKEYS);
			Array.Clear (keyup, 0, MAXKEYS);
			Array.Clear (mousehits, 0, MAXBUTTONS);
			Array.Clear (mouseup, 0, MAXBUTTONS);
			anyKeyDown = false;
		}

		/// <summary>Mouse position in logical game pixels.</summary>
		public static void UpdateMouseInput() {
			GLContext context = current;
			if (context == null || context._screenScale == 0) return;
			mouseX = (int)((WebRuntime.MouseX - context._screenOffsetX) / context._screenScale);
			mouseY = (int)((WebRuntime.MouseY - context._screenOffsetY) / context._screenScale);
		}

		public int currentFps {
			get { return _lastFPS; }
		}

		public int targetFps {
			get { return _targetFrameRate; }
			set {
				if (value < 1) {
					_targetFrameRate = 1;
				} else {
					_targetFrameRate = value;
				}
			}
		}
	}
}
