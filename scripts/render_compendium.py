"""
Build the publishable design review page (docs/design/compendium.html) from docs/design/cards.json.

  python scripts/render_compendium.py
"""
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "design", "cards.json")
TEMPLATE = os.path.join(ROOT, "scripts", "templates", "compendium.html")
OUT = os.path.join(ROOT, "docs", "design", "compendium.html")


def main():
    data = json.load(open(SRC, encoding="utf-8"))
    data.pop("_format", None)
    page = open(TEMPLATE, encoding="utf-8").read().replace("__DATA__", json.dumps(data, ensure_ascii=False))
    with open(OUT, "w", encoding="utf-8") as fh:
        fh.write(page)
    print("wrote", OUT, f"({len(page):,} bytes)")


if __name__ == "__main__":
    main()
