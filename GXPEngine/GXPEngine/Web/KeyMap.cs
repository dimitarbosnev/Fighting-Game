namespace GXPEngine.Web
{
	/// <summary>
	/// Maps browser keys to GXPEngine's key codes (the GLFW 2 values in Utils/Key.cs).
	///
	/// The host reads <see cref="Names"/> at startup (engine_layout's key_names) and
	/// reports each KeyboardEvent.code at the index of its name, so index i in the
	/// shared Input_State is the key whose GXPEngine code is <see cref="Codes"/>[i].
	/// Codes are physical key positions, so WASD stays WASD on AZERTY or Dvorak.
	/// </summary>
	internal static class KeyMap
	{
		static readonly (string name, int code)[] Table = {
			("None", 0), // index 0 is reserved; JS drops it
			("KeyA", Key.A), ("KeyB", Key.B), ("KeyC", Key.C), ("KeyD", Key.D), ("KeyE", Key.E),
			("KeyF", Key.F), ("KeyG", Key.G), ("KeyH", Key.H), ("KeyI", Key.I), ("KeyJ", Key.J),
			("KeyK", Key.K), ("KeyL", Key.L), ("KeyM", Key.M), ("KeyN", Key.N), ("KeyO", Key.O),
			("KeyP", Key.P), ("KeyQ", Key.Q), ("KeyR", Key.R), ("KeyS", Key.S), ("KeyT", Key.T),
			("KeyU", Key.U), ("KeyV", Key.V), ("KeyW", Key.W), ("KeyX", Key.X), ("KeyY", Key.Y),
			("KeyZ", Key.Z),
			("Digit0", Key.ZERO), ("Digit1", Key.ONE), ("Digit2", Key.TWO), ("Digit3", Key.THREE),
			("Digit4", Key.FOUR), ("Digit5", Key.FIVE), ("Digit6", Key.SIX), ("Digit7", Key.SEVEN),
			("Digit8", Key.EIGHT), ("Digit9", Key.NINE),
			("F1", Key.F1), ("F2", Key.F2), ("F3", Key.F3), ("F4", Key.F4), ("F5", Key.F5), ("F6", Key.F6),
			("F7", Key.F7), ("F8", Key.F8), ("F9", Key.F9), ("F10", Key.F10), ("F11", Key.F11), ("F12", Key.F12),
			("ArrowUp", Key.UP), ("ArrowDown", Key.DOWN), ("ArrowLeft", Key.LEFT), ("ArrowRight", Key.RIGHT),
			("Space", Key.SPACE), ("Enter", Key.ENTER), ("Escape", Key.ESCAPE), ("Backspace", Key.BACKSPACE),
			("Tab", Key.TAB), ("CapsLock", Key.CAPS_LOCK), ("NumLock", Key.NUM_LOCK),
			("ShiftLeft", Key.LEFT_SHIFT), ("ShiftRight", Key.RIGHT_SHIFT),
			("ControlLeft", Key.LEFT_CTRL), ("ControlRight", Key.RIGHT_CTRL),
			("AltLeft", Key.LEFT_ALT), ("AltRight", Key.RIGHT_ALT),
			("Minus", Key.MINUS_UNDERSCORE), ("Equal", Key.EQUALS),
			("BracketLeft", Key.SQ_BRACKET_1), ("BracketRight", Key.SQ_BRACKET_2), ("Backslash", Key.BACKSLASH),
			("Semicolon", Key.SEMICOLON), ("Quote", Key.APOSTROPHE), ("Backquote", Key.TILDE),
			("Comma", Key.COMMA), ("Period", Key.DOT), ("Slash", Key.QUESTION_MARK),
			("Insert", Key.INSERT), ("Delete", Key.DELETE), ("Home", Key.HOME), ("End", Key.END),
			("PageUp", Key.PAGE_UP), ("PageDown", Key.PAGE_DOWN),
			("Numpad1", Key.NUMPAD_1), ("Numpad2", Key.NUMPAD_2), ("Numpad3", Key.NUMPAD_3),
			("Numpad4", Key.NUMPAD_4), ("Numpad5", Key.NUMPAD_5), ("Numpad6", Key.NUMPAD_6),
			("Numpad7", Key.NUMPAD_7), ("Numpad8", Key.NUMPAD_8), ("Numpad9", Key.NUMPAD_9),
			("NumpadDivide", Key.NUMPAD_DIV), ("NumpadMultiply", Key.NUMPAD_TIMES),
			("NumpadSubtract", Key.MINUS), ("NumpadAdd", Key.PLUS),
			("NumpadDecimal", Key.NUMPAD_DELETE), ("NumpadEnter", Key.NUMPAD_ENTER),
		};

		public static readonly string[] Names = BuildNames();
		public static readonly int[] Codes = BuildCodes();

		static string[] BuildNames() {
			string[] names = new string[Table.Length];
			for (int i = 0; i < Table.Length; i++) names[i] = Table[i].name;
			return names;
		}

		static int[] BuildCodes() {
			int[] codes = new int[Table.Length];
			for (int i = 0; i < Table.Length; i++) codes[i] = Table[i].code;
			return codes;
		}
	}
}
