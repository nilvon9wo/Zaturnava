"""Set cells of the pieces spreadsheet in place, leaving the rest of the file as it is.

    py -3 scripts/set-pieces-cells.py O15==LeaderPromotionBonus Z5=LeaderPromotionBonus AA5=50 Q15:v=4 \
        name:LeaderPromotionBonus=Sheet1!$AA$5

Each argument is one change to docs/Other Potential Pieces.xlsx, on its first sheet:

- `A1=text` or `A1=12` sets a value; `A1==FORMULA` a formula (without its `=`), whose last computed value may follow
  after `@` (`O15==LeaderPromotionBonus@50`);
- `A1:v=12` sets only the last computed value of a formula cell, so that what a reader without a spreadsheet program
  sees stays true;
- `name:Name=Sheet1!$A$1` defines (or redefines) a name;
- `row-:14` deletes row 14, moving the rows below it up one, and every reference to them with them (in cells,
  formulas, ranges, comments and names). A range the row was inside shrinks by one;
- `@artifacts/changes.txt` reads changes from that file (relative to the repository), one to a line, so that long
  formulas need no quoting in the shell.

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


REFERENCE = re.compile(r"(?<![A-Za-z_\d])(\$?[A-Z]{1,3}\$?)(\d+)(?![\d(])")


def shift_references(text: str, deleted: int) -> str:
    """Every cell reference below the deleted row, one row up."""

    def shifted(found: re.Match[str]) -> str:
        row = int(found.group(2))
        return found.group(1) + str(row - 1 if row > deleted else row)

    return REFERENCE.sub(shifted, text)


def delete_row(parts: dict[str, bytes], deleted: int) -> None:
    sheet = parts[SHEET].decode("utf-8")
    row = re.search(rf'<row r="{deleted}"[^>]*?(?:/>|>.*?</row>)', sheet, re.S)
    if not row:
        raise SystemExit(f"row {deleted} is not in the sheet")
    if re.search(r'<f t="shared"[^>]*ref=', row.group(0)):
        raise SystemExit(f"row {deleted} holds the master of a shared formula")
    sheet = sheet[: row.start()] + sheet[row.end() :]
    # Text the sheet shows (inline or shared strings) is never a reference: only markup and formulas are shifted.
    sheet = re.sub(
        r'(?<=\s)(r|ref|sqref)="([^"]*)"',
        lambda m: f'{m.group(1)}="{shift_references(m.group(2), deleted)}"',
        sheet,
    )
    sheet = re.sub(
        r"(<f[^>]*>)(.*?)(</f>)",
        lambda m: m.group(1) + shift_references(m.group(2), deleted) + m.group(3),
        sheet,
        flags=re.S,
    )
    parts[SHEET] = sheet.encode("utf-8")
    for name in [name for name in parts if re.fullmatch(r"xl/comments\d*\.xml", name)]:
        comments = parts[name].decode("utf-8")
        comments = re.sub(rf'<comment ref="[A-Z]+{deleted}".*?</comment>', "", comments, flags=re.S)
        comments = re.sub(r'ref="([^"]*)"', lambda m: f'ref="{shift_references(m.group(1), deleted)}"', comments)
        parts[name] = comments.encode("utf-8")
    workbook = parts[WORKBOOK].decode("utf-8")
    workbook = re.sub(
        r"(<definedName[^>]*>)(.*?)(</definedName>)",
        lambda m: m.group(1) + shift_references(m.group(2), deleted) + m.group(3),
        workbook,
        flags=re.S,
    )
    parts[WORKBOOK] = workbook.encode("utf-8")


def recalculate_on_load(workbook: str) -> str:
    if "<calcPr" not in workbook:
        return workbook.replace("</workbook>", '<calcPr fullCalcOnLoad="1"/></workbook>', 1)
    if "fullCalcOnLoad" in workbook:
        return re.sub(r'fullCalcOnLoad="[^"]*"', 'fullCalcOnLoad="1"', workbook)
    return workbook.replace("<calcPr", '<calcPr fullCalcOnLoad="1"', 1)


def expanded(arguments: list[str]) -> list[str]:
    """Each `@path` argument replaced by the changes in that file, one to a line (blank lines skipped)."""
    changes: list[str] = []
    for argument in arguments:
        if argument.startswith("@"):
            lines = (ROOT / argument[1:]).read_text(encoding="utf-8").splitlines()
            changes.extend(line for line in lines if line.strip())
        else:
            changes.append(argument)
    return changes


def main(changes: list[str]) -> None:
    changes = expanded(changes)
    with zipfile.ZipFile(BOOK) as book:
        parts = {item.filename: book.read(item.filename) for item in book.infolist()}
        infos = book.infolist()
    for change in [change for change in changes if change.startswith("row-:")]:
        delete_row(parts, int(change[len("row-:") :]))
    changes = [change for change in changes if not change.startswith("row-:")]
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
