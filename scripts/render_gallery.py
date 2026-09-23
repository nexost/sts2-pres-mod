"""
Build a self-contained, filterable card gallery (docs/design/gallery.html) from docs/design/cards.json.

  python scripts/render_gallery.py
"""
import html
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "design", "cards.json")
OUT = os.path.join(ROOT, "docs", "design", "gallery.html")

PAGE = r"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>The Donald: Card Gallery</title>
<style>
:root {
  --bg: #15171c; --panel: #1f232b; --ink: #ece6d6; --muted: #9a9384; --line: #343a46;
  --gold: #f2b92e; --gold-deep: #9c6d0e; --kw: #efc050; --red: #d94a3d;
  --basic: #8d8a82; --common: #a7a39a; --uncommon: #4f9fd8; --rare: #e2b33c; --ancient: #b784e8; --token: #6cc59a;
  --attack: #c9503f; --skill: #3f7fc9; --power: #c9a23f;
}
* { box-sizing: border-box; }
body { margin: 0; background: var(--bg); color: var(--ink); font: 15px/1.45 Georgia, "Times New Roman", serif; }
header { padding: 20px 16px 8px; max-width: 1200px; margin: 0 auto; }
h1 { margin: 0; font-size: 26px; color: var(--gold); letter-spacing: .5px; }
.sub { color: var(--muted); margin-top: 4px; }
.bar { position: sticky; top: 0; z-index: 5; background: rgba(21,23,28,.96); border-bottom: 1px solid var(--line); }
.filters { max-width: 1200px; margin: 0 auto; padding: 10px 16px; display: flex; flex-wrap: wrap; gap: 8px 14px; align-items: center; }
.group { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; }
.group span { color: var(--muted); font-size: 13px; margin-right: 2px; }
button.chip { background: var(--panel); color: var(--ink); border: 1px solid var(--line); border-radius: 999px; padding: 4px 10px; font: 13px Georgia, serif; cursor: pointer; }
button.chip.on { border-color: var(--gold); color: var(--gold); }
label.toggle { display: flex; gap: 6px; align-items: center; font-size: 13px; cursor: pointer; }
.count { color: var(--muted); font-size: 13px; margin-left: auto; }
main { max-width: 1200px; margin: 0 auto; padding: 16px; }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(210px, 1fr)); gap: 16px; }
.card { position: relative; background: linear-gradient(#2a2418, #1d1a14); border: 3px solid var(--gold-deep); border-radius: 14px; padding: 10px 10px 12px; box-shadow: 0 2px 0 #000, inset 0 0 0 1px rgba(242,185,46,.25); display: flex; flex-direction: column; min-height: 300px; }
.card[data-rarity="Common"] { border-color: #6f6b62; }
.card[data-rarity="Uncommon"] { border-color: #3b6f96; }
.card[data-rarity="Rare"] { border-color: #b38a22; box-shadow: 0 0 12px rgba(226,179,60,.35); }
.card[data-rarity="Ancient"] { border-color: #8a5cc0; box-shadow: 0 0 14px rgba(183,132,232,.4); }
.card[data-rarity="Token"] { border-color: #3e8a66; }
.cost { position: absolute; top: -10px; left: -10px; width: 38px; height: 38px; border-radius: 50%; background: radial-gradient(circle at 35% 30%, #ffe08a, var(--gold) 45%, var(--gold-deep)); color: #2b1d00; font-weight: bold; font-size: 20px; display: grid; place-items: center; border: 2px solid #5a3e06; }
.name { text-align: center; font-weight: bold; font-size: 16px; background: linear-gradient(#e9dcc0, #cbb88f); color: #2a2213; border-radius: 6px; padding: 3px 6px 3px 26px; margin: 2px 0 8px 10px; }
.art { height: 96px; border-radius: 6px; background: repeating-linear-gradient(135deg, #2f2a21 0 10px, #29251d 10px 20px); color: var(--muted); font-size: 12px; font-style: italic; padding: 8px; overflow: hidden; border: 1px solid #3d3527; }
.type { align-self: center; margin: -9px 0 6px; font-size: 11px; background: #3a3a3a; color: #eee; padding: 1px 8px; border-radius: 4px; border: 1px solid #555; }
.type.Attack { background: var(--attack); } .type.Skill { background: var(--skill); } .type.Power { background: var(--power); color: #231800; }
.text { flex: 1; text-align: center; font-size: 14px; padding: 2px 4px; }
.text b { color: var(--kw); font-weight: normal; }
.text .up { color: #7fd67f; }
.kw { display: block; color: var(--kw); margin-top: 4px; }
.foot { display: flex; justify-content: space-between; font-size: 11px; color: var(--muted); margin-top: 6px; border-top: 1px solid #3d3527; padding-top: 5px; gap: 6px; }
.rar { font-weight: bold; }
.rar.Basic { color: var(--basic); } .rar.Common { color: var(--common); } .rar.Uncommon { color: var(--uncommon); } .rar.Rare { color: var(--rare); } .rar.Ancient { color: var(--ancient); } .rar.Token { color: var(--token); }
.note { font-size: 11px; color: var(--muted); margin-top: 4px; font-style: italic; }
section h2 { color: var(--gold); font-size: 20px; margin: 28px 0 10px; border-bottom: 1px solid var(--line); padding-bottom: 4px; }
.items { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 12px; }
.item { background: var(--panel); border: 1px solid var(--line); border-radius: 10px; padding: 10px 12px; }
.item h3 { margin: 0 0 4px; font-size: 16px; color: var(--ink); }
.item .flav { color: var(--muted); font-style: italic; font-size: 13px; margin-top: 4px; }
.legend { color: var(--muted); font-size: 13px; }
.hidden { display: none !important; }
@media (max-width: 520px) { .grid { grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: 12px; } .card { min-height: 260px; } .art { height: 70px; } h1 { font-size: 22px; } }
</style>
</head>
<body>
<header>
  <h1>The Donald: Card Gallery</h1>
  <div class="sub">Step 3 design review · 88 cards + Tweet token · art boxes describe the planned illustration · <span class="legend">keywords in <b style="color:var(--kw)">gold</b></span></div>
</header>
<div class="bar"><div class="filters">
  <div class="group" id="f-rarity"><span>Rarity</span></div>
  <div class="group" id="f-type"><span>Type</span></div>
  <div class="group" id="f-arch"><span>Style</span></div>
  <label class="toggle"><input type="checkbox" id="upgraded"> Upgraded</label>
  <label class="toggle"><input type="checkbox" id="notes"> Design notes</label>
  <div class="count" id="count"></div>
</div></div>
<main>
  <div class="grid" id="grid"></div>
  <section><h2>Relics</h2><div class="items" id="relics"></div></section>
  <section><h2>Potions</h2><div class="items" id="potions"></div></section>
</main>
<script>
const DATA = __DATA__;
const RARITIES = ["Basic","Common","Uncommon","Rare","Ancient","Token"];
const TYPES = ["Attack","Skill","Power"];
const ARCHES = ["Wall","Deport","Deals","Tweets","General"];
const state = { rarity: new Set(), type: new Set(), arch: new Set(), upgraded: false, notes: false };
const KW = /\b(Build|Wall|Deport(?:ed)?|Tariff|Tweets?|Pay \d+ Gold|Block|Weak|Vulnerable|Strength|Intangible|Exhaust|Retain|Innate|Energy|Gold)\b/g;
function esc(s) { return s.replace(/[&<>]/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;"}[c])); }
function render(text, up) {
  let t = esc(text).replace(/\{([^|}]*)\|([^}]*)\}/g, (m, a, b) => up && a !== b ? `\u0000${b}\u0001` : (up ? b : a));
  t = t.replace(KW, "<b>$1</b>");
  return t.replace(/\u0000/g, '<span class="up">').replace(/\u0001/g, "</span>");
}
function chips(id, values, key) {
  const box = document.getElementById(id);
  values.forEach(v => {
    const b = document.createElement("button"); b.className = "chip"; b.textContent = v;
    b.onclick = () => { state[key].has(v) ? state[key].delete(v) : state[key].add(v); b.classList.toggle("on"); draw(); };
    box.appendChild(b);
  });
}
function draw() {
  const grid = document.getElementById("grid"); grid.innerHTML = ""; let n = 0;
  const order = c => [RARITIES.indexOf(c.rarity), TYPES.indexOf(c.type), ARCHES.indexOf(c.arch)];
  [...DATA.cards].sort((a, b) => { const x = order(a), y = order(b); for (let i = 0; i < 3; i++) if (x[i] !== y[i]) return x[i] - y[i]; return 0; })
  .forEach(c => {
    if (state.rarity.size && !state.rarity.has(c.rarity)) return;
    if (state.type.size && !state.type.has(c.type)) return;
    if (state.arch.size && !state.arch.has(c.arch)) return;
    n++;
    const up = state.upgraded;
    const cost = up && c.cost_up !== undefined ? `<span class="up">${c.cost_up}</span>` : c.cost;
    const kws = (c.kw || []).filter(k => !(up && c.up && c.up.includes("No longer Exhausts") && k === "Exhaust"));
    const extra = up && c.up ? `<span class="kw up">${esc(c.up)}</span>` : "";
    const el = document.createElement("div"); el.className = "card"; el.dataset.rarity = c.rarity;
    el.innerHTML = `<div class="cost">${cost}</div>
      <div class="name">${esc(c.name)}${up ? "+" : ""}</div>
      <div class="art">${esc(c.art || "")}</div>
      <div class="type ${c.type}">${c.type}</div>
      <div class="text">${render(c.text, up)}${kws.length ? `<span class="kw">${kws.join(". ")}.</span>` : ""}${extra}${c.mp ? '<span class="kw">Co-op only.</span>' : ""}</div>
      ${state.notes && c.note ? `<div class="note">${esc(c.note)}</div>` : ""}
      <div class="foot"><span class="rar ${c.rarity}">${c.rarity}</span><span>${c.arch}</span></div>`;
    grid.appendChild(el);
  });
  document.getElementById("count").textContent = `${n} shown`;
}
function items(id, list) {
  const box = document.getElementById(id);
  list.forEach(r => {
    const el = document.createElement("div"); el.className = "item";
    el.innerHTML = `<h3>${esc(r.name)} <span class="rar ${r.rarity.split(" ")[0]}" style="font-size:12px">${esc(r.rarity)}</span></h3>
      <div>${render(r.text, false)}</div>${r.flavor ? `<div class="flav">${esc(r.flavor)}</div>` : ""}`;
    box.appendChild(el);
  });
}
chips("f-rarity", RARITIES, "rarity"); chips("f-type", TYPES, "type"); chips("f-arch", ARCHES, "arch");
document.getElementById("upgraded").onchange = e => { state.upgraded = e.target.checked; draw(); };
document.getElementById("notes").onchange = e => { state.notes = e.target.checked; draw(); };
items("relics", DATA.relics); items("potions", DATA.potions); draw();
</script>
</body>
</html>
"""


def main():
    data = json.load(open(SRC, encoding="utf-8"))
    data.pop("_format", None)
    page = PAGE.replace("__DATA__", json.dumps(data, ensure_ascii=False))
    with open(OUT, "w", encoding="utf-8") as fh:
        fh.write(page)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
