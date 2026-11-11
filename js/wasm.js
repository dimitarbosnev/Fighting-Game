// Odin's js_wasm32 target imports a few runtime helpers from "odin_env"
// (console output, time, math, randomness). This is a minimal ES-module
// replacement for that part of Odin's core/sys/wasm/js/odin.js. Only the
// functions a program actually uses become imports, so if a future Odin
// version needs a new one, instantiation fails with a LinkError naming it.

const decoder = new TextDecoder();

// Typed-array views over wasm memory. Growing the memory detaches the old
// ArrayBuffer, so the getters re-create the views whenever it has changed.
export class WasmMemory {
  #memory = null;
  #buffer = null;
  #u8;
  #u32;
  #f32;

  attach(memory) {
    this.#memory = memory;
  }

  get buffer() {
    const buffer = this.#memory.buffer;
    if (buffer !== this.#buffer) {
      this.#buffer = buffer;
      this.#u8 = new Uint8Array(buffer);
      this.#u32 = new Uint32Array(buffer);
      this.#f32 = new Float32Array(buffer);
    }
    return buffer;
  }

  get u8() { this.buffer; return this.#u8; }
  get u32() { this.buffer; return this.#u32; }
  get f32() { this.buffer; return this.#f32; }

  string(ptr, len) {
    return decoder.decode(this.u8.subarray(ptr, ptr + len));
  }
}

function odinEnv(mem, recentOutput) {
  const pending = { 1: "", 2: "" }; // fmt writes lines in pieces; log whole lines
  return {
    write(fd, ptr, len) {
      const lines = (pending[fd] + mem.string(ptr, len)).split("\n");
      pending[fd] = lines.pop();
      for (const line of lines) {
        (fd === 2 ? console.error : console.log)(line);
        recentOutput.push(line);
        if (recentOutput.length > 10) recentOutput.shift();
      }
    },
    trap() { throw new Error("wasm trap"); },
    abort() { throw new Error("wasm abort"); },
    alert(ptr, len) { alert(mem.string(ptr, len)); },
    time_now: () => BigInt(Date.now()),
    tick_now: () => performance.now(),
    time_sleep: () => { },
    sqrt: Math.sqrt,
    sin: Math.sin,
    cos: Math.cos,
    pow: Math.pow,
    fmuladd: (x, y, z) => x * y + z,
    ln: Math.log,
    exp: Math.exp,
    ldexp: (x, exp) => x * Math.pow(2, exp),
    rand_bytes(ptr, len) {
      for (let i = 0; i < len; i += 65536) { // getRandomValues' per-call limit
        crypto.getRandomValues(mem.u8.subarray(ptr + i, ptr + Math.min(len, i + 65536)));
      }
    },
  };
}

// `hostImports(mem)` returns extra import modules, e.g. { engine: { ... } }.
// The module is instantiated but not started: call exports._start() to run main().
// `recentOutput` holds the last lines the program printed. Odin writes panic
// messages to stdout on this target, so this is where a crash explains itself.
export async function loadWasm(url, hostImports = () => ({})) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`Could not load ${url} (HTTP ${response.status}). Has the wasm module been built?`);
  }
  const mem = new WasmMemory();
  const recentOutput = [];
  const imports = { odin_env: odinEnv(mem, recentOutput), ...hostImports(mem) };
  const { instance } = await WebAssembly.instantiate(await response.arrayBuffer(), imports);
  mem.attach(instance.exports.memory);
  return { exports: instance.exports, mem, recentOutput };
}
