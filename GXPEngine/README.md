# GXPEngine for the browser

GXPEngine running in the browser. A game written for GXPEngine compiles, together
with the engine, into **one WebAssembly module** with .NET NativeAOT-LLVM; a small
JS host renders it with WebGPU and plays sound with Web Audio.

```
GXPEngine/
  GXPEngine/                 engine source (C#), compiled into each game
    Web/                     browser backend: shared-memory ABI, host imports, content files
  build/WebEntry.cs          the module's exports, compiled into each game
  host/                      the page: index.html (template), js/, shaders/
  GXPEngine.Web.targets      the one file a game imports
```

## Using it in a game

1. Put this `GXPEngine` folder in the game's folder (copy it, or add it as a git submodule).
2. Give the game a project file next to its code:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <AssemblyName>MyGame</AssemblyName>
       <GameTitle>My Game</GameTitle>
       <GameDescription>One line about the game</GameDescription>
       <GameCover>Assets/cover.png</GameCover>  <!-- optional, inside Content/ -->
       <GameIcon>Assets/icon.png</GameIcon>     <!-- optional, inside Content/ -->
     </PropertyGroup>
     <Import Project="GXPEngine\GXPEngine.Web.targets" />
   </Project>
   ```

3. Move what used to be next to the exe (`bin/Debug`) into `Content/`: images, sounds,
   fonts, `.tmx` levels, `settings.txt`. Paths in the code stay the same, because they are
   now relative to `Content/`.
4. Optionally add `controls.html` next to the project: an HTML fragment shown on the
   start screen under the Play button (see Kamisen's for an example).
5. Build:

   | Command | Output |
   |---|---|
   | `dotnet publish MyGame.csproj -c Release` | `web/`: serve it and open `index.html` to play |
   | `dotnet publish MyGame.csproj -c Release -p:WebDist=true` | `dist/`: smaller, self-contained, for a website |

The game's `static void Main()` (on its `Game` subclass) runs as before; `game.Start()`
returns at once and the page drives the frames. All the game's code, including code
that uses the engine's `internal` members, compiles unchanged because the engine is
part of the game's assembly, as in a desktop GXPEngine project.

Toolchain: the .NET 10 SDK, and WASI SDK 29 (found through `WASI_SDK_PATH`, or a
`.tools/wasi-sdk` folder in the game folder or above it). The NativeAOT-LLVM packages
come from the dotnet-experimental NuGet feed, which the targets add automatically.

## What works differently in the browser

| Desktop GXPEngine | Browser |
|---|---|
| Window of `width` x `height` | Scaled to fit the page and centered (black bars) |
| `game.targetFps` | Game logic steps at exactly this rate on any display |
| Sprites, animation, collisions, Tiled levels, `Sound`/`SoundChannel` | Same |
| `EasyDraw` (text and shapes) | Same API, drawn by the browser. Fonts: a `.ttf` anywhere in `Content/` is available by its file name (`Utils.LoadFont("Retro Gaming.ttf", 30)`) |
| `Camera` / `Window` (split screen) | Supported; viewports clip on the CPU (rotated sprites are kept whole if they touch the viewport) |
| `Gizmos`, `SpriteBatch` | Supported |
| `Settings.Load()` | Reads `Content/settings.txt` from the module. Use `Settings.Load(typeof(MySettings))` to keep your own fields in the game |
| `File.ReadAllText`, `StreamReader("file")` | No file system: use `GXPEngine.Web.ContentFiles` (`Exists`, `ReadAllText`, `ReadAllLines`, `OpenRead`) for files in `Content/` (`.tmx .tsx .txt .xml .json .csv` are embedded; add more with `<GameData Include="..." />`) |
| `Canvas.graphics`, `System.Drawing.Bitmap/Graphics/Font` | Not available: use `EasyDraw` methods |
| `BlendMode.ADDITIVE`, `MULTIPLY`, ... | Drawn with normal alpha blending |
| Threads, `SerialPort` (Arduino), writing files | Not available |
| Escape closes the game | No; the page stays open |

Sound only starts after the visitor presses Play (browsers require a click first).
