"""Set cells of the pieces spreadsheet in place, leaving the rest of the file as it is.

    py -3 scripts/set-pieces-cells.py O15==LeaderPromotionBonus Z5=LeaderPromotionBonus AA5=50 Q15:v=4 \
        name:LeaderPromotionBonus=Sheet1!$AA$5

Each argument is one change to docs/Other Potential Pieces.xlsx, on its first sheet:

- `A1=text` or `A1=12` sets a value; `A1==FORMULA` a formula (without its `=`), whose last computed value may follow
  after `@` (`O15==LeaderPromotionBonus@50`);
- `A1:v=12` sets only the last computed value of a formula cell, so that what a reader without a spreadsheet program
  sees stays true;
- `name:Name=Sheet1!$A$1` defines (or redefines) a name.

The file is edited as the XML it is, by text, so that nothing it holds is lost; a cell keeps its style, and a new
cell takes the style of the cell to its left. Excel recalculates everything when the file is next opened. Needs
nothing beyond Python itself. Then `py -3 scripts/dump-pieces-sheet.py` shows the result.
"""

import re
import sys
import zipfile
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parent.parent
BOOK = ROOT / "docs" / "Other Potential Pieces.xlsx"
SHEET = "xl/worksheets/sheet1.xml"
WORKBOOK = "xl/workbook.xml"


def column_number(reference: str) -> int:
    letters = re.sub(r"\d+$", "", reference)
    number = 0
    for letter in letters:
        number = number * 26 + ord(letter) - ord("A") + 1
    return number


def cell_pattern(reference: str) -> re.Pattern[str]:
    return re.compile(rf'<c r="{reference}"(?P<attributes>[^>]*?)(?:/>|>(?P<body>.*?)</c>)', re.S)


def cell_xml(reference: str, style: str, value: str) -> str:
    style_attribute = f' s="{style}"' if style else ""
    if value.startswith("="):
        formula, _, cached = value[1:].partition("@")
        cached_xml = f"<v>{escape(cached)}</v>" if cached else ""
        return f'<c r="{reference}"{style_attribute}><f>{escape(formula)}</f>{cached_xml}</c>'
    if re.fullmatch(r"-?\d+(\.\d+)?", value):
        return f'<c r="{reference}"{style_attribute}><v>{value}</v></c>'
    return f'<c r="{reference}"{style_attribute} t="inlineStr"><is><t>{escape(value)}</t></is></c>'


def style_of(attributes: str) -> str:
    found = re.search(r'\bs="(\d+)"', attributes)
    return found.group(1) if found else ""


def set_cell(sheet: str, reference: str, value: str) -> str:
    existing = cell_pattern(reference).search(sheet)
    if existing:
        replacement = cell_xml(reference, style_of(existing.group("attributes")), value)
        return sheet[: existing.start()] + replacement + sheet[existing.end() :]
    row = re.sub(r"^[A-Z]+", "", reference)
    found = re.search(rf'<row r="{row}"[^>]*>(?P<cells>.*?)</row>', sheet, re.S)
    if not found:
        raise SystemExit(f"row {row} is not in the sheet")
    cells = list(re.finditer(r'<c r="(?P<ref>[A-Z]+\d+)"(?P<attributes>[^>]*?)(?:/>|>.*?</c>)', found.group("cells"), re.S))
    before = [cell for cell in cells if column_number(cell.group("ref")) < column_number(reference)]
    style = style_of(before[-1].group("attributes")) if before else ""
    at = found.start("cells") + (before[-1].end() if before else 0)
    return sheet[:at] + cell_xml(reference, style, value) + sheet[at:]


def set_cached(sheet: str, reference: str, cached: str) -> str:
    existing = cell_pattern(reference).search(sheet)
    if not existing or "<f" not in (existing.group("body") or ""):
        raise SystemExit(f"{reference} holds no formula")
    body = re.sub(r"<v>.*?</v>", "", existing.group("body"), flags=re.S) + f"<v>{escape(cached)}</v>"
    attributes = re.sub(r'\st="[^"]*"', "", existing.group("attributes"))
    replacement = f'<c r="{reference}"{attributes}>{body}</c>'
    return sheet[: existing.start()] + replacement + sheet[existing.end() :]


def set_name(workbook: str, name: str, reference: str) -> str:
    defined = f'<definedName name="{name}">{escape(reference)}</definedName>'
    existing = re.search(rf'<definedName name="{re.escape(name)}"[^>]*>.*?</definedName>', workbook, re.S)
    if existing:
        return workbook[: existing.start()] + defined + workbook[existing.end() :]
    if "<definedNames>" in workbook:
        return workbook.replace("</definedNames>", defined + "</definedNames>", 1)
    return workbook.replace("</sheets>", "</sheets><definedNames>" + defined + "</definedNames>", 1)


def recalculate_on_load(workbook: str) -> str:
    if "<calcPr" not in workbook:
        return workbook.replace("</workbook>", '<calcPr fullCalcOnLoad="1"/></workbook>', 1)
    if "fullCalcOnLoad" in workbook:
        return re.sub(r'fullCalcOnLoad="[^"]*"', 'fullCalcOnLoad="1"', workbook)
    return workbook.replace("<calcPr", '<calcPr fullCalcOnLoad="1"', 1)


def main(changes: list[str]) -> None:
    with zipfile.ZipFile(BOOK) as book:
        parts = {item.filename: book.read(item.filename) for item in book.infolist()}
        infos = book.infolist()
    sheet = parts[SHEET].decode("utf-8")
    workbook = parts[WORKBOOK].decode("utf-8")
    for change in changes:
        target, _, value = change.partition("=")
        if target.startswith("name:"):
            workbook = set_name(workbook, target[len("name:") :], value)
        elif target.endswith(":v"):
            sheet = set_cached(sheet, target[: -len(":v")], value)
        else:
            sheet = set_cell(sheet, target, value)
    parts[SHEET] = sheet.encode("utf-8")
    parts[WORKBOOK] = recalculate_on_load(workbook).encode("utf-8")
    with zipfile.ZipFile(BOOK, "w", zipfile.ZIP_DEFLATED) as book:
        for info in infos:
            book.writestr(info, parts[info.filename])
    print(f"set {len(changes)} change(s) in {BOOK.relative_to(ROOT)}")


if __name__ == "__main__":
    main(sys.argv[1:])
