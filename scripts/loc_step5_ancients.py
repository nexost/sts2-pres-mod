"""
One-off helper for Step 5: The Donald's lines with the Ancients, merged into localization/eng/ancients.json.
Line patterns must match AncientDialoguePatch (A = Ancient, C = The Donald). The last line of a dialogue has no button.
Tone: the jokes are on the persona (bragging, gold, deals); the Ancients keep their own voices.
"""
import json
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATH = os.path.join(ROOT, "mod", "trump_character", "localization", "eng", "ancients.json")
CHAR = "TRUMP"

# ancient: [dialogue 0 lines, dialogue 1 lines]; each line is (speaker, text, button or None for the last line)
DIALOGUES = {
    "NEOW": [
        [("A", "[sine]..arise... ..golden.. one...[/sine]", "Continue"),
         ("C", "Nobody arises like me. Everyone says so.\nBig whale, by the way. Tremendous size.", "Continue"),
         ("A", "[sine]...so... ..loud...[/sine]", None)],
        [("A", "[sine]..you... ..again...\n..still.. talking...[/sine]", None)],
    ],
    "DARV": [
        [("A", "Well look at you! Shiniest fella I've seen up here in ages!", "Brag"),
         ("C", "It's real gold. Most people don't know that.\nNice pile. Mine's bigger.", "Continue"),
         ("A", "Hah! Take somethin' before ya start appraisin' my stuff!", None)],
        [("A", "Back again? Don't go puttin' your name on my pile!", None)],
    ],
    "OROBAS": [
        [("A", "Shiny man! Gold man!! Where from? How so shiny??", "Brag"),
         ("C", "[i][font_size=22]The Donald describes his hotels for hours.[/font_size][/i]", "Continue"),
         ("A", "Tall towers! Golden towers!! Take take!!!", None)],
        [("A", "Gold man is back!! More stories!! Look look!", None)],
    ],
    "PAEL": [
        [("C", "Big dragon! Very nice. Bit melty.\nYou should see a guy about that. I know a guy.", "Continue"),
         ("A", "[thinky_dots]Take a part of me... and please... stop talking...[/thinky_dots]", None)],
        [("C", "Still melting? Sad!\nI'll take a little piece. Great publicity for you.", None)],
    ],
    "TANX": [
        [("A", "GOLD!? HOW IS THIS BEING GOLD!??", "Respond"),
         ("C", "It's a very expensive color. The best color.\nMaybe the best color ever.", "Continue"),
         ("A", "SO SHINY!! NOW GO FIGHT SOMETHING!!!", None)],
        [("A", "THE GOLD ONE RETURNS!! MORE WEAPONS FOR THE GOLD ONE!!", None)],
    ],
    "TEZCATARA": [
        [("A", "Oh my, what a [jitter][b]dazzling[/b][/jitter] visitor! Who might you be, sweetie?", "Respond"),
         ("C", "Sweetie? I'm The Donald. Very famous.\nI love fire, by the way. Fire and fury. Great combination.", "Continue"),
         ("A", "Such confidence!\n[i][font_size=22]Tezcatara cackles with delight.[/font_size][/i]", None)],
        [("A", "Welcome back, sweetie! Still so very... [jitter][b]golden[/b][/jitter].", None)],
    ],
    "NONUPEIPE": [
        [("C", "I seek audience with Nonupeipe. Big fan. Huge.", "Continue"),
         ("A", "A golden traveller! Your words are many, your hair is... remarkable.\nCome closer, receive my blessing.", None)],
        [("A", "Back again? Receive my blessing, and perhaps... fewer speeches.", None)],
    ],
    "VAKUU": [
        [("A", "A merchant of promises? Within these accursed walls?", "Chat"),
         ("C", "I don't make promises. I make deals.\nThe best deals. You'll see.", "Continue"),
         ("A", "Then let us deal, mortal. I always win.", None)],
        [("A", "Back to haggle with a demon? Bold. Let us see what you'll trade.", None)],
    ],
}


def main():
    with open(PATH, encoding="utf-8") as fh:
        loc = json.load(fh)
    added = 0
    for ancient, dialogues in DIALOGUES.items():
        for index, lines in enumerate(dialogues):
            suffix = "r" if index > 0 else ""
            for line, (speaker, text, button) in enumerate(lines):
                base = f"{ancient}.talk.{CHAR}.{index}-{line}{suffix}"
                loc[f"{base}.{'ancient' if speaker == 'A' else 'char'}"] = text
                added += 1
                if button:
                    loc[f"{base}.next"] = button
    with open(PATH, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(loc, fh, ensure_ascii=False, indent=2)
        fh.write("\n")
    print(f"{added} lines for {len(DIALOGUES)} Ancients, {len(loc)} keys total")


if __name__ == "__main__":
    main()
