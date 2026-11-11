
import { Input } from './input.js'
import { readLayout } from "./abi.js"
import { loadWasm } from "./wasm.js"
import { init_renderer_2d, preloadImages, loadTexture_2d, textureSize, freeTexture_2d, resize_2d, render_2d } from './renderer_2d.js'
import { canvasImports, flush as flushCanvases, forget as forgetCanvas } from './canvas_2d.js'
import { Audio } from './audio.js'
import { initVolumeControl } from './volume_control.js'
import { initPage } from './page.js'
import {
  cubeVertexArray,
  cubeVertexSize,
  cubeUVOffset,
  cubePositionOffset,
  cubeVertexCount,
} from './cube.js'

//Page init (start screen, errors, fullscreen: see page.js and index.html)
const canvas = document.getElementById('gameCanvas')
const page = initPage(canvas)
const showError = (text) => page.error(text)

function unsupported(reason) {
  page.unsupported(`${reason}\n\nThis game needs WebGPU: a recent Chrome or Edge, Safari 26+, or Firefox 141+ on Windows.`)
  throw new Error(reason)
}
//----------------------

//Context + Device init
if (!navigator.gpu) unsupported("WebGPU is not available in this browser.")
const adapter = await navigator.gpu.requestAdapter({
  powerPreference: "high-performance",

})
if (!adapter) unsupported("No WebGPU graphics adapter was found (WebGPU may be disabled, or the GPU is not supported).")
const device = await adapter.requestDevice().catch((err) => unsupported(`The WebGPU device could not be created (${err.message}).`))
const context = canvas.getContext('webgpu')

const devicePixelRatio = window.devicePixelRatio
canvas.width = canvas.clientWidth * devicePixelRatio
canvas.height = canvas.clientHeight * devicePixelRatio

const presentationFormat = navigator.gpu.getPreferredCanvasFormat()

context.configure({
  device,
  format: presentationFormat,
})
//----------------------

//Renderer init
try {
  await init_renderer_2d(device, presentationFormat)
} catch (err) {
  console.error("Renderer failed to initialize:", err)
  showError(`The renderer failed to initialize (${err.message}).`)
  throw err
}
//----------------------


//Canvas resize callback
const observer = new ResizeObserver((entries) => {
  canvas.width = canvas.clientWidth * devicePixelRatio
  canvas.height = canvas.clientHeight * devicePixelRatio

  resize_2d(canvas.width, canvas.height)
})

observer.observe(canvas)
resize_2d(canvas.width, canvas.height)
//----------------------

//Asset preload
// The build writes assets/manifest.json listing every file under assets/. Images
// are decoded up front so the game knows their sizes the moment it creates a
// sprite, and fonts are registered under their file name ("Retro Gaming.ttf" ->
// font-family "Retro Gaming"). Sounds load on demand (see audio.js).
const ASSETS_URL = "assets/"
const manifest = await fetch(ASSETS_URL + "manifest.json").then((r) => (r.ok ? r.json() : []), () => [])
const assetUrls = (pattern) => manifest.filter((file) => pattern.test(file)).map((file) => ASSETS_URL + file)

// The portfolio build (dist/) may ship compressed audio: the game still asks for
// "x.wav", and gets "x.mp3" when that is what the manifest lists.
const manifestFiles = new Set(manifest)
function resolveAsset(url) {
  if (!url.startsWith(ASSETS_URL) || manifestFiles.size === 0) return url
  const file = url.slice(ASSETS_URL.length)
  if (manifestFiles.has(file)) return url
  const mp3 = file.replace(/\.wav$/i, ".mp3")
  return manifestFiles.has(mp3) ? ASSETS_URL + mp3 : url
}

await Promise.all([
  preloadImages(assetUrls(/\.(png|jpe?g|gif|webp|bmp)$/i), (done, total) => page.progress(`Loading ${Math.round(done / total * 100)}%`)),
  ...assetUrls(/\.(ttf|otf|woff2?)$/i).map(async (url) => {
    const family = decodeURIComponent(url.split("/").pop().replace(/\.[^.]+$/, ""))
    try {
      document.fonts.add(await new FontFace(family, `url("${encodeURI(url)}")`).load())
    } catch (err) {
      console.error(`Failed to load font "${url}":`, err)
    }
  }),
])
//----------------------

//WASM init
const WASM_URL = "wasm/game.wasm"
const audio = new Audio(resolveAsset)
initVolumeControl(audio, canvas)
const { exports, mem, recentOutput } = await loadWasm(WASM_URL, (mem) => ({
  // Functions the game imports from the "engine" module (GXPEngine/GXPEngine/Web/Host.cs).
  engine: {
    load_texture: (ptr, len, filter) => loadTexture_2d(mem.string(ptr, len), filter),
    texture_size(id, out) {
      const [width, height] = textureSize(id)
      mem.u32[out >> 2] = width
      mem.u32[(out >> 2) + 1] = height
    },
    texture_free(id) {
      forgetCanvas(id)
      freeTexture_2d(id)
    },
    ...canvasImports(mem),
    ...audio.imports(mem),
  },
}))
//----------------------

//Input init
const layout = readLayout(exports, mem)
const input = new Input(canvas, layout.keyNames)
//const input = new Input(canvas)
//----------------------


let lastTime;
let statTime = 0;

function frame(now) {
  // Clamp dt so a stalled or backgrounded tab doesn't produce one huge step.
  const dt = lastTime === undefined ? 0 : Math.min((now - lastTime) / 1000, 0.1);
  lastTime = now;
  statTime ||= now;

  input.update(mem.u8, layout.input)

  try {
    exports.engine_frame(dt);
  } catch (err) {
    console.error(err);
    showError(`The game crashed (${err.message}). Last output:\n\n${recentOutput.join("\n")}`);
    return; // stop the loop: the wasm state is not trustworthy after a trap
  }

  const texture_view = context.getCurrentTexture().createView()

  const encoder = device.createCommandEncoder()

  flushCanvases() // upload text and drawings made this frame
  render_2d(encoder, texture_view, mem, layout);

  device.queue.submit([encoder.finish()])

  requestAnimationFrame(frame);
}

//Main function
await page.waitForPlay() // the click also unlocks audio and focuses the canvas
try {
  exports._start()
  requestAnimationFrame(frame)
} catch (err) {
  console.error(err)
  showError(`The game failed to start (${err.message}). Last output:\n\n${recentOutput.join("\n")}`)
}
//----------------------