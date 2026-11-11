# Kamisen

A two-player pixel-art fighting game written in C# on GXPEngine, compiled to
WebAssembly with .NET NativeAOT-LLVM and rendered with WebGPU.

## Layout

| Path | What |
|---|---|
| `Kamisen.csproj` | the game project; imports the engine with one line |
| `MyGame.cs`, `Game Files/` | game code |
| `Content/` | what used to sit next to the exe: `Assets/`, the `.tmx` levels |
| `controls.html` | controls table on the start screen |
| `GXPEngine/` | the engine, reusable by other games (see [GXPEngine/README.md](GXPEngine/README.md)) |

## Build

Requires the repo-local toolchain in `.tools/` (gitignored): the .NET 10 SDK in
`.tools/dotnet` and WASI SDK 29 in `.tools/wasi-sdk`.

| VS Code task | Command | Output |
|---|---|---|
| Build WASM (release) | `.tools/dotnet/dotnet publish Kamisen.csproj -c Release` | `web/`, for local testing |
| Build portfolio (dist) | `.tools/dotnet/dotnet publish Kamisen.csproj -c Release -p:WebDist=true` | `dist/`, a self-contained copy of the site |

Serve `web/` (or `dist/`) with any static web server; opening `index.html` from
the file system does not work, because the page fetches its files.

## Adding it to a website

1. Run **Build portfolio (dist)**.
2. Copy the whole `dist/` folder to your site, e.g. as `/projects/kamisen/`.
   All paths inside are relative, so any folder name works.
3. Embed it:

```html
<iframe
  src="/projects/kamisen/index.html"
  title="Kamisen, a two-player fighting game"
  width="960" height="540"
  style="border: 0; aspect-ratio: 16 / 9; width: 100%; max-width: 960px; height: auto;"
  allow="autoplay; fullscreen; gamepad"
  allowfullscreen
  loading="lazy"></iframe>
```

`allow="fullscreen"` enables the page's fullscreen button; without it the button is hidden.

Notes for hosting:

- The game only starts when the visitor presses **Play**. That click unlocks audio and
  gives the game keyboard focus inside the iframe.
- It needs WebGPU (current Chrome/Edge, Safari 26+, Firefox 141+ on Windows). Other
  browsers see a short explanation instead of the Play button.
- Most hosts (GitHub Pages, Netlify, Vercel, Cloudflare Pages) gzip files automatically;
  `game.wasm` is about 4.5 MB, 1.4 MB gzipped.
- Music is the bulk of the download. With [ffmpeg](https://ffmpeg.org/) installed
  (e.g. `winget install Gyan.FFmpeg`), the dist build converts music and voice lines
  to MP3, which shrinks `dist/` from about 84 MB to roughly 15–20 MB. Without it, the
  build warns and keeps the WAV files.
