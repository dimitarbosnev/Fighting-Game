using System;

namespace GXPEngine
{
	/// <summary>
	/// Contains various time related functions.
	/// In the browser build game time advances by exactly one step (1000 / targetFps ms)
	/// per game update, so deltaTime is steady even when the display rate differs.
	/// </summary>
	public class Time
	{
		private static double _time;
		private static int previousTime;
		private static int previousFrameTime;

		/// <summary>
		/// Returns the current system time in milliseconds
		/// </summary>
		public static int now {
			get { return System.Environment.TickCount; }
		}

		/// <summary>
		/// Returns this time in milliseconds since the program started
		/// </summary>
		public static int time {
			get { return (int)_time; }
		}

		/// <summary>
		/// Returns the time in milliseconds that has passed since the previous frame
		/// </summary>
		public static int deltaTime {
			get { return previousFrameTime; }
		}

		internal static void Reset() {
			_time = 0;
			previousTime = 0;
			previousFrameTime = 0;
		}

		internal static void Advance(double milliseconds) {
			_time += milliseconds;
		}

		internal static void newFrame() {
			previousFrameTime = time - previousTime;
			previousTime = time;
		}
	}
}
