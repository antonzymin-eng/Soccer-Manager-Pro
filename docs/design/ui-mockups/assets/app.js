// app.js — direction switching + section nav highlight
(function () {
  const root = document.documentElement;
  const STORAGE_KEY = "smp-direction";

  const TYPE_COPY = {
    touchline: {
      summary: "PT Sans Narrow + IBM Plex Sans",
      description: "PT Sans Narrow is the G1 display reference — compact, humanist, and condensed; AP-03 validates the exact shipping font. IBM Plex Sans remains the touchline body face. JetBrains Mono is used for comparative numeric/data content.",
      heavy: "700"
    },
    stadium: {
      summary: "Barlow Condensed + Barlow",
      description: "Barlow Condensed is the display face for the historical Stadium reference, with Barlow as its body face. JetBrains Mono is used for comparative numeric/data content.",
      heavy: "800"
    }
  };

  function updateDirectionCopy(dir) {
    const copy = TYPE_COPY[dir] || TYPE_COPY.touchline;
    const typeSummary = document.querySelector(".hero-meta > div:nth-child(4) .v");
    const typeDescription = document.querySelector("#type .s-head .desc");
    const heavySpecimens = document.querySelectorAll("#type .type-row:nth-of-type(-n+2) .meta:first-child");
    if (typeSummary) typeSummary.textContent = copy.summary;
    if (typeDescription) typeDescription.textContent = copy.description;
    heavySpecimens.forEach((node) => {
      node.textContent = node.textContent.replace(/\/(?:\s*)\d+$/, `/ ${copy.heavy}`);
    });
  }

  function setDirection(dir) {
    root.setAttribute("data-direction", dir);
    document.querySelectorAll(".switcher button").forEach((b) => {
      b.setAttribute("aria-pressed", b.dataset.dir === dir ? "true" : "false");
    });
    updateDirectionCopy(dir);
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
