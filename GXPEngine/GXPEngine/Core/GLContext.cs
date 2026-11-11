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

		/// <summary>Scissoring is not supported by the web renderer; rendering always covers the canvas.</summary>
		public void SetScissor(int x, int y, int width, int height) {
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
			int p = _top;
			float a = _stack[p], b = _stack[p + 1], c = _stack[p + 2], d = _stack[p + 3], tx = _stack[p + 4], ty = _stack[p + 5];

			// Corners 0 (top-left), 1 (top-right) and 3 (bottom-left), in canvas pixels.
			float x0 = a * vertices[0] + c * vertices[1] + tx, y0 = b * vertices[0] + d * vertices[1] + ty;
			float x1 = a * vertices[2] + c * vertices[3] + tx, y1 = b * vertices[2] + d * vertices[3] + ty;
			float x3 = a * vertices[6] + c * vertices[7] + tx, y3 = b * vertices[6] + d * vertices[7] + ty;

			float ux = x1 - x0, uy = y1 - y0; // along the quad's top edge
			float vx = x3 - x0, vy = y3 - y0; // along the quad's left edge
			float w = (float)Math.Sqrt(ux * ux + uy * uy);
			float h = (float)Math.Sqrt(vx * vx + vy * vy);
			if (w == 0 || h == 0) return;
			float rotation = (float)Math.Atan2(uy, ux);
			if (ux * vy - uy * vx < 0) h = -h;

			WebRuntime.PushInstance(_texture,
				x0 + (ux + vx) * 0.5f, y0 + (uy + vy) * 0.5f, w, h,
				uv[0], uv[1], uv[4], uv[5], rotation, _color);
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
