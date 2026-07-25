// app.js — direction switching + section nav highlight
(function () {
  const root = document.documentElement;
  const STORAGE_KEY = "smp-direction";

  function setDirection(dir) {
    root.setAttribute("data-direction", dir);
    document.querySelectorAll(".switcher button").forEach((b) => {
      b.setAttribute("aria-pressed", b.dataset.dir === dir ? "true" : "false");
    });
    try { localStorage.setItem(STORAGE_KEY, dir); } catch (e) {}
  }

  // wire switcher
  document.querySelectorAll(".switcher button").forEach((b) => {
    b.addEventListener("click", () => setDirection(b.dataset.dir));
  });

  // Restore the saved direction, defaulting to the CHOSEN direction.
  // "touchline" (analyst tool) was selected 2026-07-25; "stadium" is retained
  // in tokens.css as the rejected alternative, reachable only via the switcher.
  const DEFAULT_DIRECTION = "touchline";
  let saved = DEFAULT_DIRECTION;
  try { saved = localStorage.getItem(STORAGE_KEY) || DEFAULT_DIRECTION; } catch (e) {}
  setDirection(saved);

  // sidebar nav highlight
  const links = [...document.querySelectorAll(".sidebar a[href^='#']")];
  const sections = links
    .map((l) => document.querySelector(l.getAttribute("href")))
    .filter(Boolean);

  function highlight() {
    const y = window.scrollY + 120;
    let active = sections[0];
    for (const s of sections) {
      if (s.offsetTop <= y) active = s;
    }
    links.forEach((l) => {
      const target = document.querySelector(l.getAttribute("href"));
      l.classList.toggle("active", target === active);
    });
  }
  window.addEventListener("scroll", highlight, { passive: true });
  highlight();

  // smooth scroll
  links.forEach((l) => {
    l.addEventListener("click", (e) => {
      const id = l.getAttribute("href");
      const target = document.querySelector(id);
      if (target) {
        e.preventDefault();
        window.scrollTo({ top: target.offsetTop - 60, behavior: "smooth" });
        history.replaceState(null, "", id);
      }
    });
  });
})();
