// The page around the game (index.html): start screen, error display, the
// "click to keep playing" hint and the fullscreen button.
//
// The game only starts when the visitor presses Play. That one click is what
// browsers require before they play audio, and it gives the canvas keyboard
// focus, which matters when the page is embedded in an <iframe>.

const $ = (id) => document.getElementById(id)

export function initPage(canvas) {
  const startScreen = $("startScreen")
  const playButton = $("playButton")
  const startMessage = $("startMessage")
  const focusHint = $("focusHint")
  const errorMessage = $("errorMessage")
  const fullscreenButton = $("fullscreenButton")

  let playing = false

  // Fullscreen covers the whole page so the volume control stays usable.
  // Inside an iframe this needs allow="fullscreen" on the <iframe> element.
  if (fullscreenButton && document.fullscreenEnabled) {
    fullscreenButton.hidden = false
    fullscreenButton.addEventListener("click", (e) => {
      if (document.fullscreenElement) document.exitFullscreen()
      else document.documentElement.requestFullscreen().catch((err) => console.warn("Fullscreen failed:", err))
      if (e.detail > 0) canvas.focus({ preventScroll: true })
    })
    document.addEventListener("fullscreenchange", () => {
      const label = document.fullscreenElement ? "Exit fullscreen" : "Fullscreen"
      fullscreenButton.setAttribute("aria-label", label)
      fullscreenButton.title = label
    })
  }

  // Keys only reach the game while the canvas has focus. Say so when it loses it,
  // e.g. after clicking elsewhere on the portfolio page around the iframe.
  const updateFocusHint = () => {
    if (focusHint) focusHint.hidden = !playing || (document.activeElement === canvas && document.hasFocus())
  }
  canvas.addEventListener("focus", updateFocusHint)
  canvas.addEventListener("blur", () => setTimeout(updateFocusHint, 0))
  window.addEventListener("focus", updateFocusHint)
  window.addEventListener("blur", () => {
    if (focusHint && playing) focusHint.hidden = false
  })
  canvas.addEventListener("pointerdown", () => canvas.focus({ preventScroll: true }))

  return {
    // Loading progress, shown on the (disabled) Play button.
    progress(text) {
      playButton.textContent = text
    },

    // Shows a message instead of the Play button, e.g. when WebGPU is missing.
    unsupported(text) {
      playButton.hidden = true
      startMessage.hidden = false
      startMessage.textContent = text
    },

    // Enables Play and resolves when the visitor presses it.
    waitForPlay() {
      playButton.disabled = false
      playButton.textContent = "Play"
      playButton.focus({ preventScroll: true })
      return new Promise((resolve) => {
        playButton.addEventListener("click", () => {
          startScreen.hidden = true
          playing = true
          canvas.focus({ preventScroll: true })
          updateFocusHint()
          resolve()
        }, { once: true })
      })
    },

    // A crash or startup failure, with the game's last output lines.
    error(text) {
      playing = false
      updateFocusHint()
      errorMessage.textContent = text
      errorMessage.hidden = false
    },
  }
}
