
import { Input } from './input.js'
import { readLayout } from "./abi.js"
import { loadWasm } from "./wasm.js"
import { init_renderer_2d, loadTexture_2d, resize_2d, render_2d } from './renderer_2d.js'
import {
  cubeVertexArray,
  cubeVertexSize,
  cubeUVOffset,
  cubePositionOffset,
  cubeVertexCount,
} from './cube.js'

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

//WASM init
const WASM_URL = "wasm/game.wasm"
const { exports, mem, recentOutput } = await loadWasm(WASM_URL, (mem) => ({
  // Functions the Odin engine package imports (foreign import "engine").
  engine: {
    load_texture: (ptr, len, filter) => loadTexture_2d(mem.string(ptr, len), filter),
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

  render_2d(encoder, texture_view, mem, layout);

  device.queue.submit([encoder.finish()])

  requestAnimationFrame(frame);
}

//Main function
canvas.focus()
exports._start()
requestAnimationFrame(frame)
//----------------------