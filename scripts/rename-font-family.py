from __future__ import annotations

import argparse

from fontTools.ttLib import TTFont


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Rename an OFL-derived subset so reserved upstream names are absent."
    )
    parser.add_argument("font_path")
    parser.add_argument("family_name")
    parser.add_argument("postscript_name")
    args = parser.parse_args()

    font = TTFont(args.font_path, recalcTimestamp=False)
    replacements = {
        1: args.family_name,
        2: "Regular",
        3: "1.0;FRDF;" + args.postscript_name,
        4: args.family_name,
        6: args.postscript_name,
        16: args.family_name,
        17: "Regular",
    }
    for record in font["name"].names:
        replacement = replacements.get(record.nameID)
        if replacement is not None:
            record.string = replacement.encode(record.getEncoding())
    font.save(args.font_path, reorderTables=False)


if __name__ == "__main__":
    main()
