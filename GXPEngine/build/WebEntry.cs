// Compiled into the game (not the engine) by GXPEngine.Web.targets.
//
// These are the module's exports the JS host calls (see host/js/engine.js). They
// live in the game assembly so the engine knows which assembly holds the game:
// its static Main(), the classes Tiled levels create by name, and its embedded content.
using System.Runtime.InteropServices;

namespace GXPEngine.Web.Generated
{
	internal static class WebEntry
	{
		[UnmanagedCallersOnly(EntryPoint = "engine_layout")]
		static uint Layout() {
			return WebRuntime.GetLayout();
		}

		[UnmanagedCallersOnly(EntryPoint = "_start")]
		static void Start() {
			WebRuntime.Start(typeof(WebEntry).Assembly);
		}

		[UnmanagedCallersOnly(EntryPoint = "engine_frame")]
		static void Frame(float dt) {
			WebRuntime.RunFrame(dt);
		}
	}
}
