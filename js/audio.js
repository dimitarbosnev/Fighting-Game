// Web Audio backend for the game's Sound / SoundChannel classes
// (GXPEngine/GXPEngine/Web/WebSoundSystem.cs).
//
// A sound is a URL whose file is fetched and decoded once, in the background.
// Playing it returns a channel id at once; if the sound is still loading, the
// channel starts as soon as it is ready (unless it was stopped meanwhile).
// Browsers keep the AudioContext suspended until the user interacts with the
// page, so nothing is heard before the first key press or click.

// Matches ChannelProperty in Host.cs.
const PROP = { VOLUME: 0, PAN: 1, FREQUENCY: 2, PAUSED: 3, PLAYING: 4 }

const DEFAULT_SAMPLE_RATE = 44100

export class Audio {
  #context = new AudioContext()
  #master = this.#context.createGain() // every channel plays through this
  #volume = 1
  #muted = false
  #buffers = new Map() // url -> Promise<AudioBuffer | null>
  #sounds = [null] // id -> { url, loop, buffer }; id 0 is "no sound"
  #channels = new Map() // id -> Channel
  #nextChannel = 1

  constructor() {
    this.#master.connect(this.#context.destination)

    // Autoplay policy: resume on the first user gesture anywhere on the page.
    const resume = () => {
      if (this.#context.state !== "suspended") return
      this.#context.resume().then(() => {
        for (const type of ["keydown", "pointerdown", "touchend"]) window.removeEventListener(type, resume, true)
      })
    }
    for (const type of ["keydown", "pointerdown", "touchend"]) window.addEventListener(type, resume, true)
  }

  // Master volume, 0..1, applied on top of each sound's own volume. The slider
  // value is squared so it follows loudness more evenly than a linear gain.
  get volume() { return this.#volume }
  set volume(value) {
    this.#volume = Math.max(0, Math.min(1, value))
    this.#applyMaster()
  }

  get muted() { return this.#muted }
  set muted(value) {
    this.#muted = !!value
    this.#applyMaster()
  }

  #applyMaster() {
    const gain = this.#muted ? 0 : this.#volume * this.#volume
    // A short ramp avoids clicks while dragging the slider.
    this.#master.gain.setTargetAtTime(gain, this.#context.currentTime, 0.015)
  }

  // The functions the wasm module imports (merged into the "engine" import module).
  imports(mem) {
    return {
      sound_load: (urlPtr, urlLen, looping, streaming) => this.load(mem.string(urlPtr, urlLen), looping !== 0),
      sound_play: (sound, paused, volume, pan) => this.play(sound, paused !== 0, volume, pan),
      channel_get: (channel, property) => this.#channels.get(channel)?.get(property) ?? 0,
      channel_set: (channel, property, value) => this.#channels.get(channel)?.set(property, value),
      channel_stop: (channel) => this.#channels.get(channel)?.stop(),
    }
  }

  load(url, loop) {
    if (!this.#buffers.has(url)) {
      this.#buffers.set(url, this.#decode(url))
    }
    const sound = { url, loop, buffer: null }
    this.#buffers.get(url).then((buffer) => { sound.buffer = buffer })
    this.#sounds.push(sound)
    return this.#sounds.length - 1
  }

  play(soundId, paused, volume, pan) {
    const sound = this.#sounds[soundId]
    if (!sound) return 0
    const id = this.#nextChannel++
    const channel = new Channel(this.#context, this.#master, sound, paused, volume, pan, () => this.#channels.delete(id))
    this.#channels.set(id, channel)
    if (sound.buffer) {
      channel.ready(sound.buffer)
    } else {
      this.#buffers.get(sound.url).then((buffer) => channel.ready(buffer))
    }
    return id
  }

  async #decode(url) {
    try {
      const response = await fetch(url)
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return await this.#context.decodeAudioData(await response.arrayBuffer())
    } catch (err) {
      console.error(`Failed to load sound "${url}":`, err)
      return null
    }
  }
}

// One playback of a sound. AudioBufferSourceNodes can only play once, so pausing
// stops the source and remembers the position; resuming starts a new source there.
class Channel {
  #context
  #loop
  #buffer = null
  #loading = true
  #source = null
  #gain
  #panner
  #paused
  #stopped = false
  #rate = 1
  #offset = 0 // seconds into the buffer when the current source started (or when paused)
  #startedAt = 0 // context time the current source started
  #onDone

  constructor(context, output, sound, paused, volume, pan, onDone) {
    this.#context = context
    this.#loop = sound.loop
    this.#paused = paused
    this.#onDone = onDone
    this.#gain = context.createGain()
    this.#gain.gain.value = volume
    this.#panner = context.createStereoPanner()
    this.#panner.pan.value = clampPan(pan)
    this.#panner.connect(this.#gain).connect(output)
  }

  ready(buffer) {
    this.#loading = false
    if (this.#stopped) return
    if (!buffer) return this.#finish()
    this.#buffer = buffer
    if (!this.#paused) this.#start()
  }

  get(property) {
    switch (property) {
      case PROP.VOLUME: return this.#gain.gain.value
      case PROP.PAN: return this.#panner.pan.value
      case PROP.FREQUENCY: return (this.#buffer?.sampleRate ?? DEFAULT_SAMPLE_RATE) * this.#rate
      case PROP.PAUSED: return this.#paused ? 1 : 0
      case PROP.PLAYING: return this.#stopped ? 0 : 1 // like SoLoud/FMOD: true while paused or loading
    }
    return 0
  }

  set(property, value) {
    switch (property) {
      case PROP.VOLUME: this.#gain.gain.value = value; break
      case PROP.PAN: this.#panner.pan.value = clampPan(value); break
      case PROP.FREQUENCY: {
        const position = this.#position()
        this.#rate = Math.max(0.01, value / (this.#buffer?.sampleRate ?? DEFAULT_SAMPLE_RATE))
        if (this.#source) {
          this.#offset = position
          this.#startedAt = this.#context.currentTime
          this.#source.playbackRate.value = this.#rate
        }
        break
      }
      case PROP.PAUSED: {
        const pause = value !== 0
        if (pause === this.#paused || this.#stopped) break
        this.#paused = pause
        if (pause) {
          this.#offset = this.#position()
          this.#stopSource()
        } else if (this.#buffer) {
          this.#start()
        }
        break
      }
    }
  }

  stop() {
    this.#stopSource()
    this.#finish()
  }

  #start() {
    const source = this.#context.createBufferSource()
    source.buffer = this.#buffer
    source.loop = this.#loop
    source.playbackRate.value = this.#rate
    source.connect(this.#panner)
    source.onended = () => {
      if (this.#source === source) this.#finish() // ignore sources stopped by pause()
    }
    this.#source = source
    this.#startedAt = this.#context.currentTime
    source.start(0, this.#offset % this.#buffer.duration)
  }

  #stopSource() {
    const source = this.#source
    this.#source = null
    if (source) {
      source.onended = null
      source.stop()
      source.disconnect()
    }
  }

  // Playback position in seconds.
  #position() {
    if (!this.#source || !this.#buffer) return this.#offset
    const position = this.#offset + (this.#context.currentTime - this.#startedAt) * this.#rate
    return this.#loop ? position % this.#buffer.duration : Math.min(position, this.#buffer.duration)
  }

  #finish() {
    if (this.#stopped) return
    this.#stopped = true
    this.#gain.disconnect()
    this.#onDone()
  }
}

function clampPan(pan) {
  return Math.max(-1, Math.min(1, pan))
}
