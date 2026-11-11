// Minimal WASI Preview 2 host for a core wasm module built by .NET NativeAOT-LLVM
// (RuntimeIdentifier wasi-wasm, linked with wasm-ld so it is a plain module and
// not a component). The runtime imports the functions below with the component
// model's canonical ABI lowering, so results that don't fit a return value are
// written to a `retptr` in linear memory, and returned lists are allocated with
// the module's exported `cabi_realloc`.
//
// Only what a browser game needs is real: stdout/stderr go to the console,
// clocks, and randomness. There is no filesystem (no preopened directories),
// stdin is empty, and environment variables are empty.

// Thrown by wasi:cli/exit so the caller can tell a clean exit from a trap.
export class WasiExit extends Error {
  constructor(ok) {
    super(`wasm module exited (${ok ? "success" : "failure"})`)
    this.ok = ok
  }
}

// Resource handles. Any non-zero value works since nothing is ever dropped for real.
const STDIN = 1, STDOUT = 2, STDERR = 3, POLLABLE = 4

// Canonical ABI: result<T, E> stores its u8 tag at 0 and the payload at the
// larger alignment of T and E. Errors from the filesystem use error-code
// "bad-descriptor" (3), which is accurate: there are no descriptors.
const BAD_DESCRIPTOR = 3

// `mem` is the WasmMemory from wasm.js, `exports()` returns the instance's
// exports once instantiated (cabi_realloc is needed to return lists), and
// `onLine(fd, line)` receives each complete line written to stdout (1) or stderr (2).
export function wasiImports(mem, exports, onLine) {
  const pending = { [STDOUT]: "", [STDERR]: "" }
  const view = () => new DataView(mem.buffer)

  const alloc = (align, size) => exports().cabi_realloc(0, 0, align, size)

  const writeList = (retptr, ptr, len) => {
    const v = view()
    v.setUint32(retptr, ptr, true)
    v.setUint32(retptr + 4, len, true)
  }

  // result<_, error-code> style failure: tag 1, error code at `payloadOffset`.
  const fail = (retptr, payloadOffset) => {
    const v = view()
    v.setUint8(retptr, 1)
    v.setUint8(retptr + payloadOffset, BAD_DESCRIPTOR)
  }

  const noop = () => { }

  return {
    "wasi:io/error@0.2.0": {
      "[resource-drop]error": noop,
    },
    "wasi:io/poll@0.2.0": {
      "[resource-drop]pollable": noop,
      "[method]pollable.block": noop, // everything is always ready
      // poll(list<borrow<pollable>>) -> list<u32>: report every pollable as ready.
      poll(ptr, len, retptr) {
        const out = len > 0 ? alloc(4, len * 4) : 0
        const v = view()
        for (let i = 0; i < len; i++) v.setUint32(out + i * 4, i, true)
        writeList(retptr, out, len)
      },
    },
    "wasi:io/streams@0.2.0": {
      "[resource-drop]input-stream": noop,
      "[resource-drop]output-stream": noop,
      "[method]input-stream.subscribe": () => POLLABLE,
      // blocking-read(self, len) -> result<list<u8>, stream-error>: stdin is always
      // closed (stream-error "closed" is variant case 1, after the 4-byte-aligned tag).
      "[method]input-stream.blocking-read"(self, len, retptr) {
        const v = view()
        v.setUint8(retptr, 1)
        v.setUint8(retptr + 4, 1)
      },
      "[method]output-stream.subscribe": () => POLLABLE,
      // check-write(self) -> result<u64, stream-error>: always room for 1 MiB.
      "[method]output-stream.check-write"(self, retptr) {
        const v = view()
        v.setUint8(retptr, 0)
        v.setBigUint64(retptr + 8, 1n << 20n, true)
      },
      // write(self, list<u8>) -> result<_, stream-error>
      "[method]output-stream.write"(self, ptr, len, retptr) {
        if (self in pending) {
          const lines = (pending[self] + mem.string(ptr, len)).split("\n")
          pending[self] = lines.pop()
          for (const line of lines) onLine(self === STDERR ? 2 : 1, line)
        }
        view().setUint8(retptr, 0)
      },
      "[method]output-stream.blocking-flush"(self, retptr) {
        view().setUint8(retptr, 0)
      },
    },
    "wasi:cli/terminal-input@0.2.0": { "[resource-drop]terminal-input": noop },
    "wasi:cli/terminal-output@0.2.0": { "[resource-drop]terminal-output": noop },
    "wasi:cli/environment@0.2.0": {
      "get-environment": (retptr) => writeList(retptr, 0, 0),
    },
    "wasi:cli/exit@0.2.0": {
      exit(status) { throw new WasiExit(status === 0) },
    },
    "wasi:cli/stdin@0.2.0": { "get-stdin": () => STDIN },
    "wasi:cli/stdout@0.2.0": { "get-stdout": () => STDOUT },
    "wasi:cli/stderr@0.2.0": { "get-stderr": () => STDERR },
    // get-terminal-*() -> option<terminal-*>: none, these are not terminals.
    "wasi:cli/terminal-stdin@0.2.0": { "get-terminal-stdin": (retptr) => view().setUint8(retptr, 0) },
    "wasi:cli/terminal-stdout@0.2.0": { "get-terminal-stdout": (retptr) => view().setUint8(retptr, 0) },
    "wasi:cli/terminal-stderr@0.2.0": { "get-terminal-stderr": (retptr) => view().setUint8(retptr, 0) },
    "wasi:clocks/monotonic-clock@0.2.0": {
      now: () => BigInt(Math.round(performance.now() * 1e6)), // nanoseconds
      "subscribe-instant": () => POLLABLE,
      "subscribe-duration": () => POLLABLE,
    },
    "wasi:clocks/wall-clock@0.2.0": {
      // now() -> datetime { seconds: u64, nanoseconds: u32 }
      now(retptr) {
        const ms = Date.now()
        const v = view()
        v.setBigUint64(retptr, BigInt(Math.floor(ms / 1000)), true)
        v.setUint32(retptr + 8, (ms % 1000) * 1e6, true)
      },
    },
    "wasi:filesystem/types@0.2.0": {
      "[resource-drop]descriptor": noop,
      "[method]descriptor.read-via-stream": (self, offset, retptr) => fail(retptr, 4),
      "[method]descriptor.write-via-stream": (self, offset, retptr) => fail(retptr, 4),
      "[method]descriptor.append-via-stream": (self, retptr) => fail(retptr, 4),
      "[method]descriptor.get-flags": (self, retptr) => fail(retptr, 1),
      "[method]descriptor.advise": (self, offset, length, advice, retptr) => fail(retptr, 1),
      "[method]descriptor.read": (self, length, offset, retptr) => fail(retptr, 4),
      "[method]descriptor.write": (self, ptr, len, offset, retptr) => fail(retptr, 8),
      "[method]descriptor.sync": (self, retptr) => fail(retptr, 1),
      "[method]descriptor.open-at": (self, pathFlags, ptr, len, openFlags, descriptorFlags, retptr) => fail(retptr, 4),
      "[method]descriptor.stat": (self, retptr) => fail(retptr, 8),
      "[method]descriptor.stat-at": (self, flags, ptr, len, retptr) => fail(retptr, 8),
      "[method]descriptor.unlink-file-at": (self, ptr, len, retptr) => fail(retptr, 1),
      "[method]descriptor.metadata-hash": (self, retptr) => fail(retptr, 8),
      "[method]descriptor.metadata-hash-at": (self, flags, ptr, len, retptr) => fail(retptr, 8),
    },
    "wasi:filesystem/preopens@0.2.0": {
      "get-directories": (retptr) => writeList(retptr, 0, 0),
    },
    "wasi:random/random@0.2.0": {
      // get-random-bytes(len: u64) -> list<u8>
      "get-random-bytes"(len, retptr) {
        const n = Number(len)
        const ptr = n > 0 ? alloc(1, n) : 0
        for (let i = 0; i < n; i += 65536) { // getRandomValues' per-call limit
          crypto.getRandomValues(mem.u8.subarray(ptr + i, ptr + Math.min(n, i + 65536)))
        }
        writeList(retptr, ptr, n)
      },
    },
  }
}
