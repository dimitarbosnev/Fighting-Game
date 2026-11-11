// Drawable textures for the game's EasyDraw / Canvas classes (GXPEngine/EasyDraw.cs).
//
// Each canvas is a 2D canvas plus a GPU texture with the same id. Draw calls
// from the wasm module mark it dirty; flush() re-uploads dirty canvases before
// the frame is rendered. Text uses the browser's fonts, including the .ttf
// files the engine registers from the asset manifest (see loadFonts in engine.js).

import { createTexture_2d, updateTexture_2d, preloadedImage } from "./renderer_2d.js"

const SHAPE = { RECT: 0, ELLIPSE: 1, LINE: 2, POLYGON: 3, ARC: 4 }
const STYLE = { BOLD: 1, ITALIC: 2, UNDERLINE: 4, STRIKEOUT: 8 }

const canvases = new Map() // texture id -> { canvas, ctx }
const dirty = new Set()

function makeCanvas(width, height) {
  if (typeof OffscreenCanvas !== "undefined") return new OffscreenCanvas(width, height)
  const canvas = document.createElement("canvas")
  canvas.width = width
  canvas.height = height
  return canvas
}

const measureContext = makeCanvas(1, 1).getContext("2d")

// System.Drawing's Color.ToArgb() packing: 0xAARRGGBB.
function css(argb) {
  argb >>>= 0
  return `rgba(${(argb >>> 16) & 255},${(argb >>> 8) & 255},${argb & 255},${((argb >>> 24) & 255) / 255})`
}

function fontString(family, sizePx, style) {
  const italic = style & STYLE.ITALIC ? "italic " : ""
  const bold = style & STYLE.BOLD ? "bold " : ""
  return `${italic}${bold}${sizePx}px "${family}", sans-serif`
}

// Ascent and line height for a font, from the font's own metrics when available.
function metrics(ctx, text) {
  const m = ctx.measureText(text)
  const size = parseFloat(ctx.font.match(/([\d.]+)px/)?.[1] ?? "16")
  const ascent = m.fontBoundingBoxAscent ?? size * 0.8
  const descent = m.fontBoundingBoxDescent ?? size * 0.2
  return { width: m.width, ascent, height: ascent + descent }
}

function get(id) {
  return canvases.get(id)
}

function createCanvas(width, height, filter) {
  const id = createTexture_2d(width, height, filter)
  const canvas = makeCanvas(Math.max(1, width), Math.max(1, height))
  canvases.set(id, { canvas, ctx: canvas.getContext("2d") })
  dirty.add(id) // upload the transparent canvas so it doesn't show stale GPU memory
  return id
}

// Uploads every canvas drawn into since the last call. Call once per frame, before rendering.
export function flush() {
  for (const id of dirty) {
    const c = get(id)
    if (c) updateTexture_2d(id, c.canvas)
  }
  dirty.clear()
}

// Forgets a canvas when its texture is freed.
export function forget(id) {
  canvases.delete(id)
  dirty.delete(id)
}

// The functions the wasm module imports (merged into the "engine" import module).
export function canvasImports(mem) {
  const str = (ptr, len) => mem.string(ptr, len)

  return {
    canvas_create: (width, height, filter) => createCanvas(width, height, filter),

    canvas_create_from_image(urlPtr, urlLen, filter) {
      const url = str(urlPtr, urlLen)
      const image = preloadedImage(url)
      if (!image) {
        console.error(`canvas_create_from_image: "${url}" is not a preloaded image`)
        return createCanvas(0, 0, filter) // size 0x0: the engine reports the image as missing
      }
      const id = createCanvas(image.width, image.height, filter)
      get(id).ctx.drawImage(image, 0, 0)
      return id
    },

    // Replaces every pixel with one color, like Graphics.Clear.
    canvas_clear(id, argb) {
      const c = get(id)
      if (!c) return
      c.ctx.clearRect(0, 0, c.canvas.width, c.canvas.height)
      if (argb >>> 24) {
        c.ctx.fillStyle = css(argb)
        c.ctx.fillRect(0, 0, c.canvas.width, c.canvas.height)
      }
      dirty.add(id)
    },

    // Draws text with (x, y) at the top-left of its line box.
    canvas_text(id, textPtr, textLen, fontPtr, fontLen, sizePx, style, x, y, argb) {
      const c = get(id)
      if (!c) return
      const text = str(textPtr, textLen)
      const ctx = c.ctx
      ctx.font = fontString(str(fontPtr, fontLen), sizePx, style)
      ctx.textBaseline = "alphabetic"
      ctx.textAlign = "left"
      ctx.fillStyle = css(argb)
      const m = metrics(ctx, text)
      ctx.fillText(text, x, y + m.ascent)
      if (style & (STYLE.UNDERLINE | STYLE.STRIKEOUT)) {
        const thickness = Math.max(1, sizePx / 16)
        if (style & STYLE.UNDERLINE) ctx.fillRect(x, y + m.ascent + thickness * 2, m.width, thickness)
        if (style & STYLE.STRIKEOUT) ctx.fillRect(x, y + m.ascent * 0.65, m.width, thickness)
      }
      dirty.add(id)
    },

    // Writes [width, height] (f32) of the text's line box to `out`.
    canvas_measure_text(textPtr, textLen, fontPtr, fontLen, sizePx, style, out) {
      measureContext.font = fontString(str(fontPtr, fontLen), sizePx, style)
      const m = metrics(measureContext, str(textPtr, textLen))
      const f32 = mem.f32
      f32[out >> 2] = m.width
      f32[(out >> 2) + 1] = m.height
    },

    // flags: 1 fill, 2 stroke. Colors are 0xAARRGGBB.
    canvas_shape(id, kind, argsPtr, argCount, fillArgb, strokeArgb, strokeWidth, flags) {
      const c = get(id)
      if (!c) return
      const a = Array.from(mem.f32.subarray(argsPtr >> 2, (argsPtr >> 2) + argCount))
      const ctx = c.ctx
      ctx.beginPath()
      switch (kind) {
        case SHAPE.RECT: ctx.rect(a[0], a[1], a[2], a[3]); break
        case SHAPE.ELLIPSE: ctx.ellipse(a[0] + a[2] / 2, a[1] + a[3] / 2, Math.abs(a[2] / 2), Math.abs(a[3] / 2), 0, 0, Math.PI * 2); break
        case SHAPE.LINE: ctx.moveTo(a[0], a[1]); ctx.lineTo(a[2], a[3]); break
        case SHAPE.POLYGON:
          for (let i = 0; i + 1 < a.length; i += 2) i === 0 ? ctx.moveTo(a[i], a[i + 1]) : ctx.lineTo(a[i], a[i + 1])
          ctx.closePath()
          break
        case SHAPE.ARC: {
          const [x, y, w, h, start, sweep] = a
          const cx = x + w / 2, cy = y + h / 2, rad = Math.PI / 180
          if (flags & 1) ctx.moveTo(cx, cy) // filled arcs are pies, like Graphics.FillPie
          ctx.ellipse(cx, cy, Math.abs(w / 2), Math.abs(h / 2), 0, start * rad, (start + sweep) * rad, sweep < 0)
          if (flags & 1) ctx.closePath()
          break
        }
      }
      if (flags & 1 && kind !== SHAPE.LINE) {
        ctx.fillStyle = css(fillArgb)
        ctx.fill()
      }
      if (flags & 2) {
        ctx.strokeStyle = css(strokeArgb)
        ctx.lineWidth = strokeWidth
        ctx.stroke()
      }
      dirty.add(id)
    },
  }
}
