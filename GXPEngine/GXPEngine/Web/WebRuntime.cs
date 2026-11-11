using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using GXPEngine.Core;

namespace GXPEngine.Web
{
	/// <summary>
	/// The wasm side of the shared-memory ABI described in js/abi.js.
	///
	/// JS writes this frame's Input_State into wasm memory, calls engine_frame(dt),
	/// then reads the Frame header plus the Instance[] and Batch[] arrays the game
	/// produced. All four live in unmanaged memory so their addresses never move.
	/// Byte offsets here must match js/abi.js; it checks the struct sizes at startup.
	/// </summary>
	public static unsafe class WebRuntime
	{
		// ------------------------------------------------------------ layout (see js/abi.js)
		public const int KEY_CAPACITY = 256;
		public const int MOUSE_BUTTON_CAPACITY = 8;
		public const int GAMEPAD_CAPACITY = 4;
		public const int GAMEPAD_BUTTON_CAPACITY = 20;
		public const int GAMEPAD_AXIS_CAPACITY = 4;
		public const int MAX_INSTANCES = 65536;
		public const int MAX_BATCHES = 1024;

		// Button_State flags, one byte per key / button.
		public const byte DOWN = 1 << 0;
		public const byte PRESSED = 1 << 1; // went down since the last frame
		public const byte RELEASED = 1 << 2; // went up since the last frame

		// Input_State
		const int INPUT_BYTES = 776;
		const int INPUT_KEYS = 0;
		const int INPUT_MOUSE_BUTTONS = 256;
		const int INPUT_MOUSE_POS = 264;
		const int INPUT_SCREEN_SIZE = 288;
		const int INPUT_GAMEPADS = 296;
		// Gamepad_State
		const int GAMEPAD_BYTES = 120;
		const int GAMEPAD_CONNECTED = 0;
		const int GAMEPAD_BUTTONS = 4;
		const int GAMEPAD_AXES = 104;

		[StructLayout(LayoutKind.Sequential)]
		struct Frame
		{
			public float offsetX, offsetY, targetX, targetY, rotation, zoom; // Camera
			public float clearR, clearG, clearB, clearA;
			public uint instanceCount;
			public uint batchCount;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Instance
		{
			public float x, y; // center
			public float width, height;
			public float u0, v0, u1, v1;
			public float rotation; // radians
			public uint color; // RGBA8: r in the lowest byte
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Batch
		{
			public uint texture;
			public uint first;
			public uint count;
		}

		// The Layout struct engine_layout returns, as u32 words.
		[StructLayout(LayoutKind.Sequential)]
		struct Layout
		{
			public uint input, inputSize;
			public uint frame, frameSize;
			public uint instances, instanceSize, instanceCapacity;
			public uint batches, batchSize, batchCapacity;
			public uint keyNames, keyNameCount; // an Odin-style []string: (ptr, len) pairs
		}

		static byte* _input;
		static Frame* _frame;
		static Instance* _instances;
		static Batch* _batches;
		static Layout* _layout;
		static bool _warnedCapacity;
		static bool _crashed;

		static void Allocate() {
			if (_layout != null) return;
			if (sizeof(Frame) != 48 || sizeof(Instance) != 40 || sizeof(Batch) != 12) {
				throw new Exception("WebRuntime: struct layout does not match js/abi.js");
			}
			_input = (byte*)NativeMemory.AllocZeroed(INPUT_BYTES);
			_frame = (Frame*)NativeMemory.AllocZeroed((nuint)sizeof(Frame));
			_instances = (Instance*)NativeMemory.AllocZeroed((nuint)(sizeof(Instance) * MAX_INSTANCES));
			_batches = (Batch*)NativeMemory.AllocZeroed((nuint)(sizeof(Batch) * MAX_BATCHES));
			_layout = (Layout*)NativeMemory.AllocZeroed((nuint)sizeof(Layout));

			_frame->zoom = 1;
			_frame->clearA = 1;

			// Key names as UTF-8 strings plus a (ptr, len) table, indexed by key value.
			string[] names = KeyMap.Names;
			uint* table = (uint*)NativeMemory.AllocZeroed((nuint)(names.Length * 8));
			for (int i = 0; i < names.Length; i++) {
				byte[] bytes = Encoding.UTF8.GetBytes(names[i]);
				byte* str = (byte*)NativeMemory.Alloc((nuint)Math.Max(bytes.Length, 1));
				Marshal.Copy(bytes, 0, (IntPtr)str, bytes.Length);
				table[2 * i] = (uint)str;
				table[2 * i + 1] = (uint)bytes.Length;
			}

			*_layout = new Layout {
				input = (uint)_input, inputSize = INPUT_BYTES,
				frame = (uint)_frame, frameSize = (uint)sizeof(Frame),
				instances = (uint)_instances, instanceSize = (uint)sizeof(Instance), instanceCapacity = MAX_INSTANCES,
				batches = (uint)_batches, batchSize = (uint)sizeof(Batch), batchCapacity = MAX_BATCHES,
				keyNames = (uint)table, keyNameCount = (uint)names.Length,
			};
		}

		/// <summary>The game's assembly, set by Start (from the game's generated WebEntry).</summary>
		public static Assembly GameAssembly { get; private set; }

		// ------------------------------------------------------------ exports
		// The wasm exports themselves are in build/WebEntry.cs, compiled into the game.

		/// <summary>Where the shared structs live. JS calls this once (engine_layout), before _start.</summary>
		public static uint GetLayout() {
			try {
				Allocate();
				return (uint)_layout;
			} catch (Exception e) {
				Crash(e);
				return 0;
			}
		}

		/// <summary>
		/// Runs the game's static Main(), which creates the Game and calls Start() (_start).
		/// Start() returns immediately: from then on the JS host drives frames.
		/// </summary>
		public static void Start(Assembly gameAssembly) {
			try {
				GameAssembly = gameAssembly;
				Allocate();
				MethodInfo main = FindMain(gameAssembly);
				if (main == null) throw new Exception("No static Main() found on a class deriving from GXPEngine.Game.");
				main.Invoke(null, main.GetParameters().Length == 0 ? null : new object[] { new string[0] });
			} catch (Exception e) {
				Crash(e);
			}
		}

		/// <summary>Called by JS once per animation frame (engine_frame), after it has written this frame's input.</summary>
		public static void RunFrame(float dt) {
			if (_crashed) return;
			try {
				_frame->instanceCount = 0;
				_frame->batchCount = 0;
				GLContext.current?.Frame(dt);
			} catch (Exception e) {
				Crash(e);
			}
		}

		// Prints the managed exception (JS shows the last output lines) and traps,
		// so the host's frame loop stops instead of running on broken state.
		static void Crash(Exception e) {
			_crashed = true;
			if (e is TargetInvocationException && e.InnerException != null) e = e.InnerException;
			Console.Error.WriteLine("Unhandled exception: " + e);
			Environment.FailFast(e.GetType().Name + ": " + e.Message);
		}

		static MethodInfo FindMain(Assembly gameAssembly) {
			const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			foreach (Type type in gameAssembly.GetTypes()) {
				if (!type.IsSubclassOf(typeof(Game))) continue;
				MethodInfo main = type.GetMethod("Main", flags);
				if (main != null) return main;
			}
			return null;
		}

		// ------------------------------------------------------------ input

		public static byte KeyState(int key) {
			return key >= 0 && key < KEY_CAPACITY ? _input[INPUT_KEYS + key] : (byte)0;
		}

		/// <summary>Index follows MouseEvent.button: 0 left, 1 middle, 2 right.</summary>
		public static byte MouseButtonState(int button) {
			return button >= 0 && button < MOUSE_BUTTON_CAPACITY ? _input[INPUT_MOUSE_BUTTONS + button] : (byte)0;
		}

		public static float MouseX { get { return *(float*)(_input + INPUT_MOUSE_POS); } }
		public static float MouseY { get { return *(float*)(_input + INPUT_MOUSE_POS + 4); } }
		public static float ScreenWidth { get { return *(float*)(_input + INPUT_SCREEN_SIZE); } }
		public static float ScreenHeight { get { return *(float*)(_input + INPUT_SCREEN_SIZE + 4); } }

		public static bool GamepadConnected(int pad) {
			return pad >= 0 && pad < GAMEPAD_CAPACITY && *(int*)(_input + INPUT_GAMEPADS + pad * GAMEPAD_BYTES + GAMEPAD_CONNECTED) != 0;
		}

		/// <summary>Buttons follow the browser's "standard" gamepad mapping.</summary>
		public static byte GamepadButtonState(int pad, int button) {
			if (pad < 0 || pad >= GAMEPAD_CAPACITY || button < 0 || button >= GAMEPAD_BUTTON_CAPACITY) return 0;
			return _input[INPUT_GAMEPADS + pad * GAMEPAD_BYTES + GAMEPAD_BUTTONS + button];
		}

		public static float GamepadAxis(int pad, int axis) {
			if (pad < 0 || pad >= GAMEPAD_CAPACITY || axis < 0 || axis >= GAMEPAD_AXIS_CAPACITY) return 0;
			return *(float*)(_input + INPUT_GAMEPADS + pad * GAMEPAD_BYTES + GAMEPAD_AXES + axis * 4);
		}

		// ------------------------------------------------------------ output

		public static void SetClearColor(float r, float g, float b, float a) {
			_frame->clearR = r;
			_frame->clearG = g;
			_frame->clearB = b;
			_frame->clearA = a;
		}

		/// <summary>
		/// Appends one sprite (a textured quad given by its center, size and rotation),
		/// starting a new batch whenever the texture changes. Draw order is submission order.
		/// </summary>
		public static void PushInstance(uint texture, float x, float y, float width, float height,
			float u0, float v0, float u1, float v1, float rotation, uint rgba) {
			Frame* f = _frame;
			bool newBatch = f->batchCount == 0 || _batches[f->batchCount - 1].texture != texture;
			if (f->instanceCount >= MAX_INSTANCES || (newBatch && f->batchCount >= MAX_BATCHES)) {
				if (!_warnedCapacity) {
					Console.Error.WriteLine("engine: MAX_INSTANCES or MAX_BATCHES reached, extra draws are dropped");
					_warnedCapacity = true;
				}
				return;
			}
			if (newBatch) {
				_batches[f->batchCount] = new Batch { texture = texture, first = f->instanceCount, count = 0 };
				f->batchCount++;
			}
			_instances[f->instanceCount] = new Instance {
				x = x, y = y, width = width, height = height,
				u0 = u0, v0 = v0, u1 = u1, v1 = v1,
				rotation = rotation, color = rgba,
			};
			f->instanceCount++;
			_batches[f->batchCount - 1].count++;
		}
	}
}
