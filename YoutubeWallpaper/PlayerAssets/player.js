"use strict";
const params = new URLSearchParams(location.search);
const video = params.get("v") || "";
const playlist = params.get("list") || "";
const index = Number(params.get("index")) || 0;
let volume = Math.max(0, Math.min(100, Number(params.get("volume")) || 0));
let intensity = Math.max(0, Math.min(100, Number(params.get("intensity")) || 0));
let player, ready = false;
const send = (kind, code = 0) => window.chrome.webview.postMessage({kind, code});
const applyVolume = () => {
  player.setVolume(volume);
  if (volume === 0) player.mute(); else player.unMute();
};
const applyIntensity = () => {
  const surface = document.getElementById("player");
  if (surface) surface.style.filter = "brightness(" + (intensity / 100) + ")";
};
window.onYouTubeIframeAPIReady = () => {
  const vars = {autoplay: 0, controls: 0, disablekb: 1, fs: 0, playsinline: 1,
    origin: location.origin, rel: 0};
  if (playlist) { vars.listType = "playlist"; vars.list = playlist; }
  else { vars.loop = 1; vars.playlist = video; }
  player = new YT.Player("player", {
    width: "100%", height: "100%", videoId: video || undefined, playerVars: vars,
    events: {
      onReady: () => {
        ready = true;
        player.getIframe().setAttribute("allow", "autoplay; encrypted-media");
        player.getIframe().setAttribute("referrerpolicy", "strict-origin-when-cross-origin");
        applyIntensity();
        applyVolume();
        if (playlist) {
          player.setLoop(true);
          if (index > 0 || !video) {
            player.loadPlaylist({listType: "playlist", list: playlist, index});
          } else player.playVideo();
        } else player.playVideo();
        send("ready");
      },
      onStateChange: e => {
        const kind = {0: "ended", 1: "playing", 2: "paused", 3: "buffering", 5: "buffering"}[e.data];
        if (e.data === 1) applyVolume();
        if (kind) send(kind);
      },
      onError: e => send("error", e.data),
      onAutoplayBlocked: () => send("blocked")
    }
  });
};
window.chrome.webview.addEventListener("message", e => {
  const message = e.data;
  if (message.command === "volume" && Number.isFinite(message.value)) {
    volume = Math.max(0, Math.min(100, message.value));
    if (ready) applyVolume();
  } else if (message.command === "intensity" && Number.isFinite(message.value)) {
    intensity = Math.max(0, Math.min(100, message.value));
    applyIntensity();
  } else if (message.command === "resume" && ready) player.playVideo();
});
