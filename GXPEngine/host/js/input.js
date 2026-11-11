import { DOWN, PRESSED, RELEASED, INPUT, GAMEPAD } from "./abi.js"


const MOUSE_BUTTON_BITS = [1, 4, 2, 8, 16]
const padBase = (slot) => INPUT.GAMEPADS + slot * GAMEPAD.BYTES

export class Input {
  #canvas
  #keys // KeyboardEvent.code -> Key value
  #fixedKeys // indices come from the wasm module's Key enum
  #hovering = false // MOUSE_POS holds a position to measure movement from
  #events = new AbortController()

  #pending = new ArrayBuffer(INPUT.BYTES)
  #p8 = new Uint8Array(this.#pending)
  #pf = new Float32Array(this.#pending)

  // This frame's state in the Input_State layout, plus views into it.
  bytes = new Uint8Array(INPUT.BYTES)
  #sf = new Float32Array(this.bytes.buffer)
  mousePos = new Float32Array(this.bytes.buffer, INPUT.MOUSE_POS, 2)
  mouseDelta = new Float32Array(this.bytes.buffer, INPUT.MOUSE_DELTA, 2)
  mouseWheel = new Float32Array(this.bytes.buffer, INPUT.MOUSE_WHEEL, 2) // in notches, +y = up
  screenSize = new Float32Array(this.bytes.buffer, INPUT.SCREEN_SIZE, 2)

  constructor(canvas, keyNames) {
    if ((keyNames?.length ?? 0) > INPUT.KEY_CAPACITY) {
      throw new Error(`The Key enum has ${keyNames.length} values but Input_State holds ${INPUT.KEY_CAPACITY}.`)
    }
    this.#canvas = canvas
    this.#fixedKeys = keyNames !== undefined
    this.#keys = new Map(keyNames?.map((name, index) => [name, index]))
    this.#keys.delete("None")

    canvas.style.touchAction = "none" // touch drags would scroll the page and cancel the pointer
    if (canvas.tabIndex < 0) canvas.tabIndex = 0 // a canvas only gets key events if it can take focus
    const on = (target, type, fn, options) =>
      target.addEventListener(type, fn, { ...options, signal: this.#events.signal })
    on(canvas, "keydown", this.#onKey)
    on(canvas, "keyup", this.#onKey)
    on(canvas, "blur", this.#releaseAll) // also fires when the whole window loses focus
    on(document, "visibilitychange", this.#releaseAll)
    on(canvas, "pointerdown", this.#onPointer)
    on(canvas, "pointermove", this.#onPointer)
    on(canvas, "pointerup", this.#onPointer)
    on(canvas, "pointerleave", this.#onPointerGone)
    on(canvas, "pointercancel", this.#onPointerGone)
    on(canvas, "wheel", this.#onWheel, { passive: false })
    on(canvas, "contextmenu", (e) => e.preventDefault())
  }

  destroy() {
    this.#events.abort()
  }

  // Call once per frame before the game reads input. dest is a Uint8Array over
  // wasm memory (fetch mem.u8 fresh each frame) and ptr the Input_State address.
  update(dest, ptr) {
    this.#pollGamepads()
    this.#setVec2(INPUT.SCREEN_SIZE, this.#canvas.width, this.#canvas.height)
    this.bytes.set(this.#p8)
    dest?.set(this.bytes, ptr)

    // Edges and motion last one frame.
    this.#clearEdges(INPUT.KEYS, INPUT.KEY_CAPACITY)
    this.#clearEdges(INPUT.MOUSE_BUTTONS, INPUT.MOUSE_BUTTON_CAPACITY)
    for (let slot = 0; slot < INPUT.GAMEPAD_CAPACITY; slot++) {
      this.#clearEdges(padBase(slot) + GAMEPAD.BUTTONS, GAMEPAD.BUTTON_CAPACITY)
    }
    this.#setVec2(INPUT.MOUSE_DELTA, 0, 0)
    this.#setVec2(INPUT.MOUSE_WHEEL, 0, 0)
  }

  // Readers return Button_State flags test them with & DOWN, PRESSED or RELEASED.

  // code is a KeyboardEvent.code, such as "KeyW", "Space" or "ArrowLeft".
  key(code) {
    const k = this.#keys.get(code)
    return k === undefined ? 0 : this.bytes[INPUT.KEYS + k]
  }

  // button is a MouseEvent.button: 0 left, 1 middle, 2 right, 3 back, 4 forward.
  mouseButton(button) {
    return this.bytes[INPUT.MOUSE_BUTTONS + button]
  }

  // Gamepad buttons and axes follow the browser's "standard" mapping.
  padConnected(slot) {
    return this.bytes[padBase(slot) + GAMEPAD.CONNECTED] !== 0
  }
  padButton(slot, i) {
    return this.bytes[padBase(slot) + GAMEPAD.BUTTONS + i]
  }
  padValue(slot, i) {
    return this.#sf[((padBase(slot) + GAMEPAD.BUTTON_VALUES) >> 2) + i]
  }
  padAxis(slot, i) {
    return this.#sf[((padBase(slot) + GAMEPAD.AXES) >> 2) + i]
  }

  // Shorthands. Down: held this frame. Pressed: went down since last frame.
  // Released: went up since last frame. A quick tap is both pressed and released.
  isKeyDown(code) { return (this.key(code) & DOWN) !== 0 }
  isKeyPressed(code) { return (this.key(code) & PRESSED) !== 0 }
  isKeyReleased(code) { return (this.key(code) & RELEASED) !== 0 }

  isMouseDown(button = 0) { return (this.mouseButton(button) & DOWN) !== 0 }
  isMousePressed(button = 0) { return (this.mouseButton(button) & PRESSED) !== 0 }
  isMouseReleased(button = 0) { return (this.mouseButton(button) & RELEASED) !== 0 }

  isPadDown(slot, i) { return (this.padButton(slot, i) & DOWN) !== 0 }
  isPadPressed(slot, i) { return (this.padButton(slot, i) & PRESSED) !== 0 }
  isPadReleased(slot, i) { return (this.padButton(slot, i) & RELEASED) !== 0 }

  #onKey = (e) => {
    const down = e.type === "keydown"
    // macOS sends no keyup for other keys while Cmd is held, so let go of them all.
    if (!down && e.key === "Meta") this.#release(INPUT.KEYS, INPUT.KEY_CAPACITY)
    const key = this.#keyFor(e.code)
    if (key === undefined) return
    // Keep game keys from scrolling or moving focus, but leave browser
    // shortcuts (Ctrl/Cmd + ...) and function keys (F5, F12) working.
    if (!e.ctrlKey && !e.metaKey && !/^F\d/.test(e.code)) e.preventDefault()
    this.#setButton(INPUT.KEYS + key, down) // key repeat is ignored
  }

  // Pointer events cover mouse, pen and touch only the primary pointer (the
  // mouse, or the first finger) drives the mouse state. With several mouse
  // buttons held, pressing another only fires pointermove, so sync from e.buttons.
  #onPointer = (e) => {
    if (!e.isPrimary) return
    if (e.type === "pointerdown") {
      this.#canvas.setPointerCapture(e.pointerId) // keep receiving events outside the canvas
      e.preventDefault() // no text selection or middle-click autoscroll
      this.#canvas.focus({ preventScroll: true }) // preventDefault can stop the click from focusing it
    }
    const rect = this.#canvas.getBoundingClientRect()
    const sx = this.#canvas.width / rect.width
    const sy = this.#canvas.height / rect.height
    const x = (e.clientX - rect.left) * sx
    const y = (e.clientY - rect.top) * sy
    const f = this.#pf
    const pos = INPUT.MOUSE_POS >> 2
    const delta = INPUT.MOUSE_DELTA >> 2
    if (document.pointerLockElement === this.#canvas) {
      // The cursor is frozen under pointer lock, so only movementX/Y carry motion.
      f[delta] += e.movementX * sx
      f[delta + 1] += e.movementY * sy
    } else if (this.#hovering) {
      f[delta] += x - f[pos]
      f[delta + 1] += y - f[pos + 1]
    }
    f[pos] = x
    f[pos + 1] = y
    this.#hovering = true
    MOUSE_BUTTON_BITS.forEach((bit, i) => this.#setButton(INPUT.MOUSE_BUTTONS + i, (e.buttons & bit) !== 0))
  }

  #onPointerGone = (e) => {
    if (!e.isPrimary) return
    if (e.type === "pointercancel") this.#release(INPUT.MOUSE_BUTTONS, INPUT.MOUSE_BUTTON_CAPACITY)
    this.#hovering = false // the next position starts fresh, without a jump in delta
  }

  #onWheel = (e) => {
    e.preventDefault()
    // Normalize to notches: deltaMode 0 is pixels (~100 per notch), 1 lines (~3), 2 pages.
    const scale = [1 / 100, 1 / 3, 1][e.deltaMode]
    const wheel = INPUT.MOUSE_WHEEL >> 2
    this.#pf[wheel] += e.deltaX * scale
    this.#pf[wheel + 1] -= e.deltaY * scale // +y = scrolling up, away from the user
  }

  // Keyup events never arrive once the canvas loses focus, so let go of everything.
  #releaseAll = () => {
    this.#release(INPUT.KEYS, INPUT.KEY_CAPACITY)
    this.#release(INPUT.MOUSE_BUTTONS, INPUT.MOUSE_BUTTON_CAPACITY)
    this.#hovering = false
  }

  // The Gamepad API has no button events, so sample it each frame.
  #pollGamepads() {
    const pads = navigator.getGamepads?.() ?? []
    for (let slot = 0; slot < INPUT.GAMEPAD_CAPACITY; slot++) {
      const pad = pads[slot]?.connected ? pads[slot] : null
      const base = padBase(slot)
      this.#p8[base + GAMEPAD.CONNECTED] = pad ? 1 : 0 // b32: its upper three bytes stay zero
      const values = (base + GAMEPAD.BUTTON_VALUES) >> 2
      for (let i = 0; i < GAMEPAD.BUTTON_CAPACITY; i++) {
        const button = pad?.buttons[i]
        this.#setButton(base + GAMEPAD.BUTTONS + i, button?.pressed ?? false)
        this.#pf[values + i] = button?.value ?? 0
      }
      const axes = (base + GAMEPAD.AXES) >> 2
      for (let i = 0; i < GAMEPAD.AXIS_CAPACITY; i++) this.#pf[axes + i] = pad?.axes[i] ?? 0
    }
  }

  #keyFor(code) {
    if (code && !this.#fixedKeys && !this.#keys.has(code) && this.#keys.size < INPUT.KEY_CAPACITY) {
      this.#keys.set(code, this.#keys.size)
    }
    return this.#keys.get(code)
  }

  // A press and release within one frame leave PRESSED | RELEASED without DOWN,
  // so quick taps still reach the game.
  #setButton(offset, down) {
    const b = this.#p8
    const wasDown = (b[offset] & DOWN) !== 0
    if (down && !wasDown) b[offset] |= DOWN | PRESSED
    else if (!down && wasDown) b[offset] = (b[offset] & ~DOWN) | RELEASED
  }

  #release(offset, count) {
    for (let i = 0; i < count; i++) this.#setButton(offset + i, false)
  }

  #clearEdges(offset, count) {
    for (let i = offset, end = offset + count; i < end; i++) this.#p8[i] &= DOWN
  }

  #setVec2(offset, x, y) {
    this.#pf[offset >> 2] = x
    this.#pf[(offset >> 2) + 1] = y
  }
}
