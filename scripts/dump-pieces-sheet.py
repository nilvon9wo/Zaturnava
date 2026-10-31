"""Write the pieces spreadsheet out as text, each cell's formula beside its last computed value.

    py -3 scripts/dump-pieces-sheet.py

Reads docs/Other Potential Pieces.xlsx and writes artifacts/pieces-sheet.txt: for every sheet, each non-empty cell
as `A1: =formula -> value`, so that a change to the sheet can be planned, and checked, by its cells. Needs nothing
beyond Python itself: an .xlsx is a zip of XML.
"""

import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SHEET = ROOT / "docs" / "Other Potential Pieces.xlsx"
OUT = ROOT / "artifacts" / "pieces-sheet.txt"
MAIN = "{http://schemas.openxmlformats.org/spreadsheetml/2006/main}"
RELS = "{http://schemas.openxmlformats.org/package/2006/relationships}"
DOC_RELS = "{http://schemas.openxmlformats.org/officeDocument/2006/relationships}"


def shared_strings(book: zipfile.ZipFile) -> list[str]:
    if "xl/sharedStrings.xml" not in book.namelist():
        return []
    root = ET.fromstring(book.read("xl/sharedStrings.xml"))
    return ["".join(text.text or "" for text in item.iter(MAIN + "t")) for item in root.iter(MAIN + "si")]


def sheets(book: zipfile.ZipFile) -> list[tuple[str, str]]:
    workbook = ET.fromstring(book.read("xl/workbook.xml"))
    rels = ET.fromstring(book.read("xl/_rels/workbook.xml.rels"))
    targets = {rel.get("Id"): rel.get("Target") for rel in rels.iter(RELS + "Relationship")}
    found = []
    for sheet in workbook.iter(MAIN + "sheet"):
        target = (targets[sheet.get(DOC_RELS + "id")] or "").lstrip("/")
        found.append((sheet.get("name"), target if target.startswith("xl/") else "xl/" + target))
    names = [f"name {name.get('name')}: {name.text}" for name in workbook.iter(MAIN + "definedName")]
    return found + [("", line) for line in names]


def main() -> None:
    lines: list[str] = []
    with zipfile.ZipFile(SHEET) as book:
        strings = shared_strings(book)
        for name, part in sheets(book):
            if not name:
                lines.append(part)
                continue
            lines.append(f"## {name} ({part})")
            root = ET.fromstring(book.read(part))
            for cell in root.iter(MAIN + "c"):
                formula = cell.find(MAIN + "f")
                value = cell.find(MAIN + "v")
                inline = cell.find(MAIN + "is")
                shown = value.text if value is not None else None
                shown = strings[int(shown)] if cell.get("t") == "s" and shown is not None else shown
                shown = "".join(t.text or "" for t in inline.iter(MAIN + "t")) if inline is not None else shown
                if formula is None and shown is None:
                    continue
                text = f"={formula.text}" if formula is not None and formula.text else ""
                shared = f" [shared {formula.get('si')} {formula.get('ref') or ''}]" if formula is not None and formula.get("t") == "shared" else ""
                lines.append(f"{cell.get('r')}: {text}{shared} -> {shown!r}" if text or shared else f"{cell.get('r')}: {shown!r}")
    OUT.parent.mkdir(exist_ok=True)
    OUT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)} ({len(lines)} lines)")


if __name__ == "__main__":
    main()
