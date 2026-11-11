// Connects the page's master volume control (#volume in index.html) to the
// game's audio (audio.js) and remembers the setting in this browser.
//
// The game reads keys from the canvas, so after the control is used with the
// mouse, focus goes back to the canvas; otherwise the arrow keys would move the
// slider instead of player 2. Keyboard users can Tab to the control, adjust it
// with the arrow keys, and press Escape to return to the game.

const STORAGE_KEY = "fighting-game.volume"

function load() {
  try {
    const saved = JSON.parse(localStorage.getItem(STORAGE_KEY))
    if (saved && typeof saved.volume === "number") return { volume: saved.volume, muted: !!saved.muted }
  } catch { } // storage may be unavailable (private mode, blocked site data)
  return null
}

function save(audio) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ volume: audio.volume, muted: audio.muted }))
  } catch { }
}

export function initVolumeControl(audio, canvas) {
  const root = document.getElementById("volume")
  const slider = document.getElementById("volumeSlider")
  const mute = document.getElementById("volumeMute")
  const value = document.getElementById("volumeValue")
  if (!root || !slider || !mute || !value) return

  const backToGame = () => canvas.focus({ preventScroll: true })

  const render = () => {
    const percent = Math.round(audio.volume * 100)
    slider.value = String(percent)
    value.textContent = `${percent}%`
    root.toggleAttribute("data-muted", audio.muted || percent === 0)
    const label = audio.muted ? "Unmute" : "Mute"
    mute.setAttribute("aria-label", label)
    mute.title = label
    slider.setAttribute("aria-valuetext", audio.muted ? `${percent}%, muted` : `${percent}%`)
  }

  const saved = load()
  audio.volume = saved ? saved.volume : Number(slider.value) / 100
  audio.muted = saved ? saved.muted : false
  render()

  slider.addEventListener("input", () => {
    audio.volume = Number(slider.value) / 100
    if (audio.muted && audio.volume > 0) audio.muted = false // moving the slider unmutes
    render()
    save(audio)
  })

  mute.addEventListener("click", (e) => {
    if (!audio.muted && audio.volume === 0) audio.volume = 0.5 // unmuting at 0% would stay silent
    else audio.muted = !audio.muted
    render()
    save(audio)
    if (e.detail > 0) backToGame() // a mouse click, not Enter/Space
  })

  slider.addEventListener("pointerup", backToGame)
  root.addEventListener("keydown", (e) => {
    if (e.key === "Escape") backToGame()
  })
}
