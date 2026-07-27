#!/usr/bin/env python3
"""Convert presentation DOCX to a compact PDF with the same textual content."""

from __future__ import annotations

from pathlib import Path

import fitz
from docx import Document
from docx.oxml.ns import qn
from docx.table import Table
from docx.text.paragraph import Paragraph

ROOT = Path(__file__).resolve().parents[1]
DOCX = ROOT / "docs" / "CardMarket_OnlineAuction_Canva_Presentation.docx"
PDF = ROOT / "docs" / "CardMarket_OnlineAuction_Canva_Presentation.pdf"

FONT_CANDIDATES = [
    "/System/Library/Fonts/Supplemental/Arial.ttf",  # supports Vietnamese, smaller
    "/System/Library/Fonts/Supplemental/Times New Roman.ttf",
    "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
    "/Library/Fonts/Arial Unicode.ttf",
]

SLIDE_TITLES = {
    "Giới thiệu đề tài",
    "Giới thiệu thành viên",
    "Mục lục",
    "Giới thiệu bài toán",
    "Chức năng người dùng",
    "Chức năng Admin",
    "Activity Diagram",
    "Kiến trúc hệ thống",
    "Database",
    "Công nghệ sử dụng",
    "Kết luận",
    "Cảm ơn",
    "Phụ lục — Asset paths cho Canva",
}


def find_font() -> str:
    for path in FONT_CANDIDATES:
        if Path(path).exists():
            return path
    raise FileNotFoundError("No Unicode TTF found for Vietnamese text")


def iter_block_items(document: Document):
    body = document.element.body
    for child in body.iterchildren():
        if child.tag == qn("w:p"):
            yield Paragraph(child, document)
        elif child.tag == qn("w:tbl"):
            yield Table(child, document)


def is_list_paragraph(paragraph: Paragraph) -> bool:
    try:
        return bool(paragraph.style and paragraph.style.name and "List" in paragraph.style.name)
    except Exception:
        return False


def classify(paragraph: Paragraph) -> str:
    text = (paragraph.text or "").strip()
    if not text:
        return "empty"
    if text.startswith("─"):
        return "sep"
    if text == "PRESENTATION DOCUMENT":
        return "title"
    if text == "CardMarket — OnlineAuction (Nhóm 3)":
        return "subtitle"
    if text.startswith("# Slide") or text.startswith("Phụ lục"):
        return "h1"
    if text in SLIDE_TITLES:
        return "h2"
    if text.endswith(":") and len(text.split()) <= 10:
        return "label"
    if is_list_paragraph(paragraph):
        return "bullet"
    if text.startswith("[Browser]") or text.startswith("User Register"):
        return "code"
    return "body"


class PdfWriter:
    def __init__(self, fontfile: str):
        self.fontfile = fontfile
        self.font = fitz.Font(fontfile=fontfile)
        self.doc = fitz.open()
        self.page_w, self.page_h = fitz.paper_size("a4")
        self.margin = 50
        self.y = self.margin
        self.page = self.doc.new_page(width=self.page_w, height=self.page_h)
        self.max_w = self.page_w - 2 * self.margin

    def new_page(self):
        self.page = self.doc.new_page(width=self.page_w, height=self.page_h)
        self.y = self.margin

    def ensure(self, height: float):
        if self.y + height > self.page_h - self.margin:
            self.new_page()

    def wrap(self, text: str, fontsize: float, width: float | None = None) -> list[str]:
        width = width if width is not None else self.max_w
        if not text:
            return [""]
        words = text.split(" ")
        lines: list[str] = []
        current = ""
        for word in words:
            trial = word if not current else f"{current} {word}"
            if self.font.text_length(trial, fontsize=fontsize) <= width:
                current = trial
            else:
                if current:
                    lines.append(current)
                # hard-break very long tokens
                while self.font.text_length(word, fontsize=fontsize) > width and len(word) > 1:
                    lo, hi = 1, len(word)
                    cut = 1
                    while lo <= hi:
                        mid = (lo + hi) // 2
                        if self.font.text_length(word[:mid], fontsize=fontsize) <= width:
                            cut = mid
                            lo = mid + 1
                        else:
                            hi = mid - 1
                    lines.append(word[:cut])
                    word = word[cut:]
                current = word
        if current:
            lines.append(current)
        return lines or [""]

    def text(
        self,
        text: str,
        *,
        fontsize: float = 11,
        color=(0.12, 0.12, 0.18),
        indent: float = 0,
        space_after: float = 4,
        line_gap: float = 1.35,
    ):
        usable = self.max_w - indent
        raw_lines = text.split("\n") if text else [""]
        all_lines: list[str] = []
        for raw in raw_lines:
            all_lines.extend(self.wrap(raw, fontsize, usable))

        line_h = fontsize * line_gap
        tw = fitz.TextWriter(self.page.rect, color=color)
        page_ref = self.page
        for line in all_lines:
            if self.y + line_h + 2 > self.page_h - self.margin:
                tw.write_text(page_ref)
                self.new_page()
                tw = fitz.TextWriter(self.page.rect, color=color)
                page_ref = self.page
            tw.append(
                (self.margin + indent, self.y + fontsize),
                line,
                font=self.font,
                fontsize=fontsize,
            )
            self.y += line_h
        tw.write_text(page_ref)
        self.y += space_after

    def separator(self):
        self.ensure(16)
        y = self.y + 4
        self.page.draw_line(
            (self.margin, y),
            (self.page_w - self.margin, y),
            color=(0.7, 0.7, 0.7),
            width=0.6,
        )
        self.y += 14

    def paragraph(self, text: str, kind: str):
        if kind == "empty":
            self.y += 6
            return
        if kind == "sep":
            self.separator()
            return
        if kind == "title":
            self.text(text, fontsize=18, color=(0.1, 0.1, 0.18), space_after=8)
            return
        if kind == "subtitle":
            self.text(text, fontsize=13, color=(0.15, 0.15, 0.22), space_after=6)
            return
        if kind == "h1":
            self.y += 6
            self.text(text, fontsize=15, color=(0.1, 0.1, 0.18), space_after=6)
            return
        if kind == "h2":
            self.text(text, fontsize=12.5, color=(0.12, 0.12, 0.2), space_after=5)
            return
        if kind == "label":
            self.text(text, fontsize=11, color=(0.71, 0.33, 0.04), space_after=3)
            return
        if kind == "bullet":
            self.text(f"•  {text}", fontsize=10.5, indent=10, space_after=2)
            return
        if kind == "code":
            self.text(text, fontsize=9.5, color=(0.25, 0.25, 0.3), indent=8, space_after=3)
            return
        self.text(text, fontsize=10.5, space_after=3)

    def table(self, table: Table):
        rows = [[(cell.text or "").replace("\n", " ").strip() for cell in row.cells] for row in table.rows]
        if not rows:
            return
        cols = len(rows[0])
        col_w = self.max_w / cols
        pad = 4
        fontsize = 8.5

        for r_i, row in enumerate(rows):
            wrapped_cols = [self.wrap(cell, fontsize, col_w - 2 * pad) for cell in row]
            max_lines = max(len(lines) for lines in wrapped_cols)
            row_h = max(20.0, max_lines * fontsize * 1.3 + 2 * pad)
            self.ensure(row_h + 2)
            x = self.margin
            tw = fitz.TextWriter(self.page.rect, color=(0.12, 0.12, 0.18))
            for c_i, lines in enumerate(wrapped_cols):
                rect = fitz.Rect(x, self.y, x + col_w, self.y + row_h)
                fill = (0.93, 0.93, 0.95) if r_i == 0 else (1, 1, 1)
                self.page.draw_rect(rect, color=(0.75, 0.75, 0.78), fill=fill, width=0.5)
                ty = self.y + pad + fontsize
                for line in lines:
                    tw.append((x + pad, ty), line, font=self.font, fontsize=fontsize)
                    ty += fontsize * 1.3
                x += col_w
            tw.write_text(self.page)
            self.y += row_h
        self.y += 8

    def save(self, path: Path):
        path.parent.mkdir(parents=True, exist_ok=True)
        self.doc.save(str(path), garbage=4, deflate=True)
        self.doc.close()


def main():
    if not DOCX.exists():
        raise SystemExit(f"Missing DOCX: {DOCX}")

    fontfile = find_font()
    writer = PdfWriter(fontfile)
    document = Document(str(DOCX))

    for block in iter_block_items(document):
        if isinstance(block, Paragraph):
            text = block.text or ""
            kind = classify(block)
            writer.paragraph(text, kind)
        elif isinstance(block, Table):
            writer.table(block)

    writer.save(PDF)
    size = PDF.stat().st_size
    pages = fitz.open(str(PDF)).page_count
    print(f"Wrote {PDF}")
    print(f"pages={pages} size={size} bytes ({size/1024:.1f} KB)")


if __name__ == "__main__":
    main()
