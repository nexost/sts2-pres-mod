"""
Build the publishable design review page (characters/<id>/design/compendium.html) from its design/cards.json.
Each character has its own page template (characters/<id>/design/compendium_template.html), because the page's prose
explains that character's mechanics. A new character starts with a copy of Trump's, rewritten during its design step.

  python scripts/render_compendium.py [-c trump]

Then republish the page (docs/ADDING_A_CHARACTER.md, phase C2).
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import presmod  # noqa: E402

_argv = sys.argv[1:]
CH = presmod.character(_argv[_argv.index("-c") + 1] if "-c" in _argv else _argv[_argv.index("--character") + 1] if "--character" in _argv else None)
SRC = CH.path("design", "cards.json")
TEMPLATE = CH.path("design", "compendium_template.html")
OUT = CH.path("design", "compendium.html")


def main():
    if not os.path.exists(TEMPLATE):
        sys.exit(f"No page template for {CH['name']}: {TEMPLATE}")
    data = json.load(open(SRC, encoding="utf-8"))
    data.pop("_format", None)
    page = open(TEMPLATE, encoding="utf-8").read().replace("__DATA__", json.dumps(data, ensure_ascii=False))
    with open(OUT, "w", encoding="utf-8") as fh:
        fh.write(page)
    print("wrote", OUT, f"({len(page):,} bytes)")


if __name__ == "__main__":
    main()
