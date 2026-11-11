import { FRAME, INSTANCE, BATCH } from "./abi.js"

const SHADER_URL = "./shaders/default_shader.wgsl"

let device
let pipeline
let cameraBuffer
let cameraBindGroup
let samplers
let instanceBuffer
const textures = [] // bind groups, indexed by texture id
const gpuTextures = [] // GPUTexture per id (undefined for the shared fallback)
const sizes = [] // [width, height] per id, once known
const images = new Map() // url -> ImageBitmap, filled by preloadImages
const cameraData = new Float32Array(8)

async function init_renderer_2d(gpuDevice, format) {
  device = gpuDevice

  // Shader init
  const response = await fetch(SHADER_URL)
  if (!response.ok) throw new Error(`Failed to load ${SHADER_URL}: HTTP ${response.status}`)
  const module = device.createShaderModule({
    label: "default_shader",
    code: await response.text(),
  })
  module.getCompilationInfo().then((info) => {
    for (const m of info.messages) console.warn(`${SHADER_URL}:${m.lineNum}:${m.linePos} ${m.type}: ${m.message}`)
  })
  //----------------------

  // Pipeline init
  pipeline = device.createRenderPipeline({
    label: "sprites",
    layout: "auto",
    vertex: {
      module,
      entryPoint: "vs_main",
      buffers: [{
        arrayStride: INSTANCE.BYTES,
        stepMode: "instance",
        attributes: [
          { shaderLocation: 0, offset: INSTANCE.POS, format: "float32x2" },
          { shaderLocation: 1, offset: INSTANCE.SIZE, format: "float32x2" },
          { shaderLocation: 2, offset: INSTANCE.UV, format: "float32x4" },
          { shaderLocation: 3, offset: INSTANCE.ROTATION, format: "float32" },
          { shaderLocation: 4, offset: INSTANCE.COLOR, format: "unorm8x4" },
        ],
      }],
    },
    fragment: {
      module,
      entryPoint: "fs_main",
      targets: [{
        format,
        blend: {
          color: { srcFactor: "src-alpha", dstFactor: "one-minus-src-alpha" },
          alpha: { srcFactor: "one", dstFactor: "one-minus-src-alpha" },
        },
      }],
    },
    primitive: { topology: "triangle-strip" },
  })
  //----------------------

  //Uniform bufffers
  cameraBuffer = device.createBuffer({
    label: "camera",
    size: cameraData.byteLength,
    usage: GPUBufferUsage.UNIFORM | GPUBufferUsage.COPY_DST,
  })

  cameraBindGroup = device.createBindGroup({
    layout: pipeline.getBindGroupLayout(0),
    entries: [{ binding: 0, resource: { buffer: cameraBuffer } }],
  })

  samplers = [
    device.createSampler({ magFilter: "linear", minFilter: "linear" }),
    device.createSampler({ magFilter: "nearest", minFilter: "nearest" }),
  ]
  //----------------------

  // Texture 0 is a 1x1 white fallback
  const white = device.createTexture({
    label: "white",
    size: [1, 1],
    format: "rgba8unorm",
    usage: GPUTextureUsage.TEXTURE_BINDING | GPUTextureUsage.COPY_DST,
  })
  device.queue.writeTexture({ texture: white }, new Uint8Array([255, 255, 255, 255]), {}, [1, 1])
  textures.push(textureBindGroup(white, 0))
  sizes.push([1, 1])
  //----------------------
}

// Decodes images ahead of time so loadTexture_2d can create them synchronously
// and report their size at once (sprite constructors need it). `onProgress(done, total)`
// is called as images finish. A failed image is logged and skipped.
async function preloadImages(urls, onProgress = () => { }) {
  let done = 0
  await Promise.all(urls.map(async (url) => {
    try {
      images.set(url, await decodeImage(url))
    } catch (err) {
      console.error(`Failed to preload image "${url}":`, err)
    }
    onProgress(++done, urls.length)
  }))
}

// Returns a texture id right away. Preloaded images are uploaded immediately;
// anything else loads in the background and draws as white until it arrives.
function loadTexture_2d(url, filter = 0) {
  const id = textures.length
  const image = images.get(url)
  if (image) {
    setTexture(id, uploadBitmap(url, image), filter)
    sizes[id] = [image.width, image.height]
    return id
  }
  textures.push(textures[0])
  decodeImage(url).then(
    (bitmap) => {
      if (textures[id] === null) return // freed while loading
      setTexture(id, uploadBitmap(url, bitmap), filter)
      sizes[id] = [bitmap.width, bitmap.height]
    },
    (err) => console.error(`Failed to load texture "${url}":`, err),
  )
  return id
}

// A preloaded image, for drawing into a canvas texture. undefined if not preloaded.
function preloadedImage(url) {
  return images.get(url)
}

// [width, height] of a texture, or [0, 0] if unknown (missing or still loading).
function textureSize(id) {
  return sizes[id] ?? [0, 0]
}

// Creates an empty texture to be filled with updateTexture_2d (e.g. from a 2D canvas).
function createTexture_2d(width, height, filter = 0, label = "canvas") {
  const id = textures.length
  const texture = device.createTexture({
    label,
    size: [Math.max(1, width), Math.max(1, height)],
    format: "rgba8unorm",
    usage: GPUTextureUsage.TEXTURE_BINDING | GPUTextureUsage.COPY_DST | GPUTextureUsage.RENDER_ATTACHMENT,
  })
  setTexture(id, texture, filter)
  sizes[id] = [width, height]
  return id
}

// Copies an image source (canvas, OffscreenCanvas, ImageBitmap) into a texture made by createTexture_2d.
function updateTexture_2d(id, source) {
  const texture = gpuTextures[id]
  if (!texture) return
  device.queue.copyExternalImageToTexture(
    { source },
    { texture, premultipliedAlpha: false }, // the blend state expects straight alpha
    [Math.min(source.width, texture.width), Math.min(source.height, texture.height)],
  )
}

// Releases a texture's GPU memory. Its id is not reused; drawing it shows white.
function freeTexture_2d(id) {
  if (id <= 0 || id >= textures.length) return
  gpuTextures[id]?.destroy()
  gpuTextures[id] = undefined
  textures[id] = null
  sizes[id] = undefined
}

// Matches the canvas backing store to its displayed size. Returns [width, height].
function resize_2d(width, height) {
  cameraData[0] = width
  cameraData[1] = height
}

// Draws the Frame the wasm module just produced. Returns its stats.
// mem.u32 / mem.f32 must be fresh views (recreate them after memory.grow).
function render_2d(encoder, texture_view, mem, layout) {
  const u32 = mem.u32
  const f32 = mem.f32
  const frame = layout.frame >> 2
  const instanceCount = u32[frame + FRAME.INSTANCE_COUNT / 4]
  const batchCount = u32[frame + FRAME.BATCH_COUNT / 4]
  const clearColor = frame + FRAME.CLEAR_COLOR / 4

  cameraData.set(f32.subarray(frame + FRAME.CAMERA / 4, frame + FRAME.CAMERA / 4 + 6), 2)
  device.queue.writeBuffer(cameraBuffer, 0, cameraData)

  if (instanceCount > 0) {
    const bytes = instanceCount * INSTANCE.BYTES
    reserveInstances(bytes)
    device.queue.writeBuffer(instanceBuffer, 0, mem.buffer, layout.instances, bytes)
  }

  const pass = encoder.beginRenderPass({
    colorAttachments: [{
      view: texture_view,
      clearValue: f32.subarray(clearColor, clearColor + 4),
      loadOp: "clear",
      storeOp: "store",
    }],
  })

  if (instanceCount > 0) {
    pass.setPipeline(pipeline)
    pass.setBindGroup(0, cameraBindGroup)
    pass.setVertexBuffer(0, instanceBuffer)
    for (let i = 0; i < batchCount; i++) {
      const batch = (layout.batches + i * BATCH.BYTES) >> 2
      const texture = u32[batch + BATCH.TEXTURE / 4]
      const first = u32[batch + BATCH.FIRST / 4]
      const count = u32[batch + BATCH.COUNT / 4]
      pass.setBindGroup(1, textures[texture] ?? textures[0])
      pass.draw(4, count, 0, first)
    }
  }

  pass.end()
}

// Grows the GPU instance buffer to the next power of two when needed.
function reserveInstances(bytes) {
  if (instanceBuffer && instanceBuffer.size >= bytes) return
  instanceBuffer?.destroy()
  instanceBuffer = device.createBuffer({
    label: "instances",
    size: Math.max(1024 * INSTANCE.BYTES, 2 ** Math.ceil(Math.log2(bytes))),
    usage: GPUBufferUsage.VERTEX | GPUBufferUsage.COPY_DST,
  })
}

async function decodeImage(url) {
  const response = await fetch(url)
  if (!response.ok) throw new Error(`HTTP ${response.status}`)
  return createImageBitmap(await response.blob(), {
    colorSpaceConversion: "none",
    premultiplyAlpha: "none", // the blend state expects straight alpha
  })
}

function uploadBitmap(label, image) {
  const texture = device.createTexture({
    label,
    size: [image.width, image.height],
    format: "rgba8unorm",
    usage: GPUTextureUsage.TEXTURE_BINDING | GPUTextureUsage.COPY_DST | GPUTextureUsage.RENDER_ATTACHMENT,
  })
  device.queue.copyExternalImageToTexture({ source: image }, { texture }, [image.width, image.height])
  return texture
}

function setTexture(id, texture, filter) {
  gpuTextures[id] = texture
  textures[id] = textureBindGroup(texture, filter)
}

function textureBindGroup(texture, filter) {
  return device.createBindGroup({
    layout: pipeline.getBindGroupLayout(1),
    entries: [
      { binding: 0, resource: texture.createView() },
      { binding: 1, resource: samplers[filter] ?? samplers[0] },
    ],
  })
}

export {
  init_renderer_2d, preloadImages, preloadedImage, loadTexture_2d, textureSize,
  createTexture_2d, updateTexture_2d, freeTexture_2d, resize_2d, render_2d,
}
