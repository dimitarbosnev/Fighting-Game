// Byte layout of the memory shared with the wasm module.
//
// Mirrors game/engine/abi.odin, which is the source of truth. Offsets are in
// bytes from the start of each struct. The Odin side #asserts the same numbers,
// so a struct change fails the Odin build until both files agree, and
// readLayout() refuses a module whose ABI_VERSION or struct sizes differ.

// Button_State flags: one byte per key, mouse button and gamepad button.
const DOWN = 1 << 0
const PRESSED = 1 << 1 // went down since the last frame
const RELEASED = 1 << 2 // went up since the last frame

const INPUT = {
  BYTES: 776,
  KEYS: 0, KEY_CAPACITY: 256,
  MOUSE_BUTTONS: 256, MOUSE_BUTTON_CAPACITY: 8,
  MOUSE_POS: 264, // [2]f32
  MOUSE_DELTA: 272, // [2]f32
  MOUSE_WHEEL: 280, // [2]f32
  SCREEN_SIZE: 288, // [2]f32
  GAMEPADS: 296, GAMEPAD_CAPACITY: 4,
}

const GAMEPAD = {
  BYTES: 120,
  CONNECTED: 0, // b32
  BUTTONS: 4, BUTTON_CAPACITY: 20,
  BUTTON_VALUES: 24, // [20]f32
  AXES: 104, AXIS_CAPACITY: 4,
}

const FRAME = {
  BYTES: 48,
  CAMERA: 0, // offset.xy, target.xy, rotation, zoom: 6 x f32
  CLEAR_COLOR: 24, // [4]f32
  INSTANCE_COUNT: 40,
  BATCH_COUNT: 44,
}

const INSTANCE = { BYTES: 40, POS: 0, SIZE: 8, UV: 16, ROTATION: 32, COLOR: 36 }

const BATCH = { BYTES: 12, TEXTURE: 0, FIRST: 4, COUNT: 8 }

// The Layout struct returned by engine_layout(), as u32 word indices.
const LAYOUT = {
  INPUT: 0, INPUT_BYTES: 1,
  FRAME: 2, FRAME_BYTES: 3,
  INSTANCES: 4, INSTANCE_BYTES: 5, INSTANCE_CAPACITY: 6,
  BATCHES: 7, BATCH_BYTES: 8, BATCH_CAPACITY: 9,
  KEY_NAMES: 10, KEY_NAME_COUNT: 11,
}

// Asks the module where the shared structs live and checks it agrees with this file.
function readLayout(exports, mem) {
  const u32 = mem.u32
  const base = exports.engine_layout() >> 2
  const word = (index) => u32[base + index]

  for (const [name, index, bytes] of [
    ["Input_State", LAYOUT.INPUT_BYTES, INPUT.BYTES],
    ["Frame", LAYOUT.FRAME_BYTES, FRAME.BYTES],
    ["Instance", LAYOUT.INSTANCE_BYTES, INSTANCE.BYTES],
    ["Batch", LAYOUT.BATCH_BYTES, BATCH.BYTES],
  ]) {
    if (word(index) !== bytes) {
      throw new Error(`ABI mismatch: size_of(${name}) is ${word(index)} in the wasm module but ${bytes} in src/engine/abi.js.`)
    }
  }

  // key_names is an Odin []string: a (ptr, len) pair pointing at (ptr, len) pairs.
  const keyNames = []
  const names = word(LAYOUT.KEY_NAMES) >> 2
  for (let i = 0; i < word(LAYOUT.KEY_NAME_COUNT); i++) {
    keyNames.push(mem.string(u32[names + 2 * i], u32[names + 2 * i + 1]))
  }

  return {
    input: word(LAYOUT.INPUT),
    frame: word(LAYOUT.FRAME),
    instances: word(LAYOUT.INSTANCES),
    instanceCapacity: word(LAYOUT.INSTANCE_CAPACITY),
    batches: word(LAYOUT.BATCHES),
    batchCapacity: word(LAYOUT.BATCH_CAPACITY),
    keyNames,
  }
}

export { DOWN, PRESSED, RELEASED, INPUT, GAMEPAD, FRAME, INSTANCE, BATCH, LAYOUT, readLayout }