
import { Input } from './input.js'
import { readLayout } from "./abi.js"
import { loadWasm } from "./wasm.js"
import { init_renderer_2d, preloadImages, loadTexture_2d, textureSize, freeTexture_2d, resize_2d, render_2d } from './renderer_2d.js'
import { canvasImports, flush as flushCanvases, forget as forgetCanvas } from './canvas_2d.js'
import { Audio } from './audio.js'
import { initVolumeControl } from './volume_control.js'
import {
  cubeVertexArray,
  cubeVertexSize,
  cubeUVOffset,
  cubePositionOffset,
  cubeVertexCount,
} from './cube.js'

// Status / error text over the canvas (loading progress, crash reports).
const status = document.createElement('pre')
status.style.cssText = 'position:fixed;inset:0;margin:0;padding:16px;display:flex;align-items:center;' +
  'justify-content:center;color:#ddd;font:14px/1.4 monospace;white-space:pre-wrap;pointer-events:none'
document.body.append(status)
const showStatus = (text) => { status.textContent = text }
function showError(text) {
  status.style.color = '#f66'
  status.style.background = 'rgba(0,0,0,0.85)'
  status.style.pointerEvents = 'auto'
  showStatus(text)
}

//Context + Device init
const canvas = document.getElementById('gameCanvas')
if (!navigator.gpu) throw new Error("WebGPU is not supported in this browser")
const adapter = await navigator.gpu.requestAdapter({
  powerPreference: "high-performance",

})
if (!adapter) throw new Error("No suitable GPU adapter found")
const device = await adapter.requestDevice()
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
await Promise.all([
  preloadImages(assetUrls(/\.(png|jpe?g|gif|webp|bmp)$/i), (done, total) => showStatus(`Loading images ${done}/${total}`)),
  ...assetUrls(/\.(ttf|otf|woff2?)$/i).map(async (url) => {
    const family = decodeURIComponent(url.split("/").pop().replace(/\.[^.]+$/, ""))
    try {
      document.fonts.add(await new FontFace(family, `url("${encodeURI(url)}")`).load())
    } catch (err) {
      console.error(`Failed to load font "${url}":`, err)
    }
  }),
])
showStatus("")
//----------------------

//WASM init
const WASM_URL = "wasm/game.wasm"
const audio = new Audio()
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
canvas.focus()
try {
  exports._start()
  requestAnimationFrame(frame)
} catch (err) {
  console.error(err)
  showError(`The game failed to start (${err.message}). Last output:\n\n${recentOutput.join("\n")}`)
}
//----------------------