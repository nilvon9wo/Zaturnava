"""Set cells of the pieces spreadsheet in place, leaving the rest of the file as it is.

    py -3 scripts/set-pieces-cells.py O15==LeaderPromotionBonus Z5=LeaderPromotionBonus AA5=50 Q15:v=4 \
        name:LeaderPromotionBonus=Sheet1!$AA$5

Each argument is one change to docs/Other Potential Pieces.xlsx, on its first sheet unless a cell says another
(`Settings!B2=1`):

- `sheet+:Settings` adds an empty sheet of that name, after the others, before any other change is made;
- `A1=text` or `A1=12` sets a value, in a new row if its own is not there (its cells styled as those above them);
  `A1==FORMULA` a formula (without its `=`), whose last computed value may follow
  after `@` (`O15==LeaderPromotionBonus@50`);
- `A1:v=12` sets only the last computed value of a formula cell, so that what a reader without a spreadsheet program
  sees stays true;
- `name:Name=Sheet1!$A$1` defines (or redefines) a name;
- `clear:A1` takes the cell away;
- `formulas:OLD=>NEW` replaces the text OLD by NEW in every formula, keeping each one's last computed value (as
  `formulas:$Y$2:$Y$47=>BaseValues`, so that a range of rows becomes a name, and a sort of the rows breaks nothing);
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


def added_row(sheet: str, row: str) -> str:
    """The sheet with an empty row `row` after the last row above it (or first, if none is), and its dimension grown
    (a row of formatting alone may come far below, as row 1048576 does)."""
    rows = list(re.finditer(r'<row r="(\d+)"[^>]*?(?:/>|>.*?</row>)', sheet, re.S))
    above = [found for found in rows if int(found.group(1)) < int(row)]
    sheet = sheet.replace("<sheetData/>", "<sheetData></sheetData>", 1)
    at = above[-1].end() if above else sheet.index("<sheetData>") + len("<sheetData>")
    sheet = sheet[:at] + f'<row r="{row}"></row>' + sheet[at:]
    return re.sub(
        r'(<dimension ref="[A-Z]+\d+:[A-Z]+)(\d+)(")',
        lambda m: m.group(1) + str(max(int(m.group(2)), int(row))) + m.group(3),
        sheet,
    )


def set_cell(sheet: str, reference: str, value: str) -> str:
    existing = cell_pattern(reference).search(sheet)
    if existing:
        replacement = cell_xml(reference, style_of(existing.group("attributes")), value)
        return sheet[: existing.start()] + replacement + sheet[existing.end() :]
    row = re.sub(r"^[A-Z]+", "", reference)
    found = re.search(rf'<row r="{row}"[^>]*>(?P<cells>.*?)</row>', sheet, re.S)
    if not found:
        sheet = added_row(sheet, row)
        found = re.search(rf'<row r="{row}"[^>]*>(?P<cells>.*?)</row>', sheet, re.S)
    if found is None:
        raise SystemExit(f"row {row} could not be added")
    cells = list(re.finditer(r'<c r="(?P<ref>[A-Z]+\d+)"(?P<attributes>[^>]*?)(?:/>|>.*?</c>)', found.group("cells"), re.S))
    before = [cell for cell in cells if column_number(cell.group("ref")) < column_number(reference)]
    # a new cell takes the style of the cell to its left; in a new row, of the cell above it
    above = cell_pattern(re.sub(r"\d+$", str(int(row) - 1), reference)).search(sheet)
    style = style_of(before[-1].group("attributes")) if before else style_of(above.group("attributes")) if above else ""
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


def cleared(sheet: str, reference: str) -> str:
    """The sheet without the cell, if it holds one."""
    existing = cell_pattern(reference).search(sheet)
    return sheet[: existing.start()] + sheet[existing.end() :] if existing else sheet


def formulas_replaced(sheet: str, old: str, new: str) -> str:
    """Every formula with `old` replaced by `new`, its last computed value kept."""
    return re.sub(
        r"(<f[^>]*>)(.*?)(</f>)",
        lambda m: m.group(1) + m.group(2).replace(escape(old), escape(new)) + m.group(3),
        sheet,
        flags=re.S,
    )


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


RELATIONSHIPS = "xl/_rels/workbook.xml.rels"
CONTENT_TYPES = "[Content_Types].xml"
WORKSHEET_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"
WORKSHEET_RELATIONSHIP = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"
EMPTY_SHEET = (
    '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>\n'
    '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" '
    'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheetData></sheetData></worksheet>'
)


def sheet_parts(parts: dict[str, bytes]) -> dict[str, str]:
    """Each sheet's part in the file, by the sheet's name."""
    workbook = parts[WORKBOOK].decode("utf-8")
    rels = parts[RELATIONSHIPS].decode("utf-8")
    targets = {
        found.group("id"): found.group("target")
        for found in re.finditer(r'<Relationship\b(?=[^>]*\bId="(?P<id>[^"]+)")(?=[^>]*\bTarget="(?P<target>[^"]+)")', rels)
    }
    found = {}
    for sheet in re.finditer(r'<sheet\b(?=[^>]*\bname="(?P<name>[^"]+)")(?=[^>]*:id="(?P<id>[^"]+)")', workbook):
        target = targets[sheet.group("id")].lstrip("/")
        found[sheet.group("name")] = target if target.startswith("xl/") else "xl/" + target
    return found


def add_sheet(parts: dict[str, bytes], name: str) -> None:
    """A new, empty sheet called `name`, after the others."""
    if name in sheet_parts(parts):
        raise SystemExit(f"there is a sheet {name} already")
    number = 1
    while f"xl/worksheets/sheet{number}.xml" in parts:
        number += 1
    part = f"xl/worksheets/sheet{number}.xml"
    parts[part] = EMPTY_SHEET.encode("utf-8")
    types = parts[CONTENT_TYPES].decode("utf-8")
    override = f'<Override PartName="/{part}" ContentType="{WORKSHEET_TYPE}"/>'
    parts[CONTENT_TYPES] = types.replace("</Types>", override + "</Types>", 1).encode("utf-8")
    rels = parts[RELATIONSHIPS].decode("utf-8")
    used = [int(found) for found in re.findall(r'\bId="rId(\d+)"', rels)]
    relationship = f"rId{max(used, default=0) + 1}"
    added = f'<Relationship Id="{relationship}" Type="{WORKSHEET_RELATIONSHIP}" Target="worksheets/sheet{number}.xml"/>'
    parts[RELATIONSHIPS] = rels.replace("</Relationships>", added + "</Relationships>", 1).encode("utf-8")
    workbook = parts[WORKBOOK].decode("utf-8")
    ids = [int(found) for found in re.findall(r'<sheet\b[^>]*\bsheetId="(\d+)"', workbook)]
    prefix = re.search(r'<sheet\b[^>]*\b(\w+):id="', workbook)
    attribute = f"{prefix.group(1) if prefix else 'r'}:id"
    entry = f'<sheet name="{escape(name)}" sheetId="{max(ids, default=0) + 1}" {attribute}="{relationship}"/>'
    parts[WORKBOOK] = workbook.replace("</sheets>", entry + "</sheets>", 1).encode("utf-8")


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
    for change in [change for change in changes if change.startswith("sheet+:")]:
        add_sheet(parts, change[len("sheet+:") :])
    changes = [change for change in changes if not change.startswith("sheet+:")]
    named = sheet_parts(parts)
    texts: dict[str, str] = {}

    def on_sheet(reference: str) -> tuple[str, str]:
        """The part a cell reference's sheet is (the first sheet's, unless it says `Name!A1`), and the cell."""
        sheet_name, _, cell = reference.rpartition("!")
        if sheet_name and sheet_name not in named:
            raise SystemExit(f"there is no sheet {sheet_name}")
        part = named[sheet_name] if sheet_name else SHEET
        texts.setdefault(part, parts[part].decode("utf-8"))
        return part, cell

    workbook = parts[WORKBOOK].decode("utf-8")
    for change in changes:
        target, _, value = change.partition("=")
        if target.startswith("name:"):
            workbook = set_name(workbook, target[len("name:") :], value)
        elif target.startswith("clear:"):
            part, cell = on_sheet(target[len("clear:") :])
            texts[part] = cleared(texts[part], cell)
        elif change.startswith("formulas:"):
            old, _, new = change[len("formulas:") :].partition("=>")
            part, _ = on_sheet("A1")
            texts[part] = formulas_replaced(texts[part], old, new)
        elif target.endswith(":v"):
            part, cell = on_sheet(target[: -len(":v")])
            texts[part] = set_cached(texts[part], cell, value)
        else:
            part, cell = on_sheet(target)
            texts[part] = set_cell(texts[part], cell, value)
    for part, text in texts.items():
        parts[part] = text.encode("utf-8")
    parts[WORKBOOK] = recalculate_on_load(workbook).encode("utf-8")
    known = {info.filename for info in infos}
    with zipfile.ZipFile(BOOK, "w", zipfile.ZIP_DEFLATED) as book:
        for info in infos:
            book.writestr(info, parts[info.filename])
        for name in [name for name in parts if name not in known]:
            book.writestr(name, parts[name])
    print(f"set {len(changes)} change(s) in {BOOK.relative_to(ROOT)}")


if __name__ == "__main__":
    main(sys.argv[1:])
