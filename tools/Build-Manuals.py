"""Render the three offline HTML guides as A4 PDFs. Requires reportlab, lxml and DejaVu Sans."""
import argparse
from pathlib import Path
from xml.sax.saxutils import escape

from lxml import html
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen.canvas import Canvas
from reportlab.platypus import Paragraph

MANUALS = Path(__file__).resolve().parent.parent / "Manuals"
PAGES = [
    ("start", "appearance"),
    ("accounts", "entries"),
    ("currency", "goals"),
    ("planned", "recurring"),
    ("shortcuts", "reports", "backup"),
]
WIDTH, HEIGHT = A4
LEFT, RIGHT = 43, WIDTH - 43
INK, GREEN, MUTED = "#192f34", "#13564e", "#476467"


def paragraph_markup(element):
    parts = [escape(element.text or "")]
    for child in element:
        text = escape(child.text_content())
        parts.append("<b>" + text + "</b>" if child.tag == "strong" else text)
        parts.append(escape(child.tail or ""))
    return "".join(parts)


def draw_text(page, markup, x, y, width, size, leading, bold=False, color=INK):
    style = ParagraphStyle("guide", fontName="GuideBold" if bold else "Guide", fontSize=size,
                           leading=leading, textColor=colors.HexColor(color))
    paragraph = Paragraph(markup, style)
    _, height = paragraph.wrap(width, HEIGHT)
    paragraph.drawOn(page, x, y - height)
    return y - height


def render(source):
    doc = html.parse(str(source))
    version = doc.xpath('//p[@class="product"]')[0].text_content()
    title = doc.xpath("//h1")[0].text_content()
    sections = {section.get("id"): section for section in doc.xpath("//section")}
    if set(sections) != {key for group in PAGES for key in group}:
        raise ValueError("Update the PDF section order to match " + source.name)
    page = Canvas(str(source.with_suffix(".pdf")), pagesize=A4, invariant=True)
    page.setTitle(version + " · " + title)
    page.setAuthor("Budget Desk")
    for index, group in enumerate(PAGES, start=1):
        page.setFillColor(colors.HexColor(GREEN))
        page.rect(0, HEIGHT - 10, WIDTH, 10, stroke=0, fill=1)
        page.setFont("GuideBold", 11)
        page.drawString(LEFT, HEIGHT - 39, version)
        page.setFont("Guide", 10)
        page.drawRightString(RIGHT, HEIGHT - 39, title)
        y = HEIGHT - 65
        if index == 1:
            y = draw_text(page, escape(title), LEFT, y, RIGHT - LEFT, 24, 30, True, GREEN) - 12
            contents = doc.xpath('//*[@id="contents"]/h2')[0].text_content()
            y = draw_text(page, escape(contents), LEFT, y, RIGHT - LEFT, 13, 18, True) - 5
            for number, keys in enumerate(PAGES, start=1):
                names = "  ·  ".join(sections[key].find("h2").text_content() for key in keys)
                page.setFillColor(colors.HexColor(GREEN))
                page.setFont("GuideBold", 9)
                page.drawString(LEFT + 1, y - 10, str(number))
                top = y
                y = draw_text(page, escape(names), LEFT + 20, y, RIGHT - LEFT - 20, 9.2, 13) - 5
                page.linkRect("", keys[0], (LEFT, y, RIGHT, top), relative=0, thickness=0)
            y -= 8
        for key in group:
            section = sections[key]
            heading = section.find("h2").text_content()
            page.bookmarkHorizontalAbsolute(key, y)
            page.addOutlineEntry(heading, key)
            y = draw_text(page, escape(heading), LEFT, y, RIGHT - LEFT, 15, 19, True, GREEN) - 7
            for paragraph in section.findall("p"):
                y = draw_text(page, paragraph_markup(paragraph), LEFT, y, RIGHT - LEFT, 10.4, 15) - 7
            y -= 9
        if y < 38:
            raise ValueError("Text overlaps footer on page %d of %s" % (index, source.name))
        page.setStrokeColor(colors.HexColor("#cbd9d5"))
        page.line(LEFT, 34, RIGHT, 34)
        page.setFont("Guide", 9)
        page.setFillColor(colors.HexColor(MUTED))
        page.drawString(LEFT, 20, title)
        page.drawRightString(RIGHT, 20, "%d / %d" % (index, len(PAGES)))
        page.showPage()
    page.save()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--font-dir", type=Path, default=Path("/usr/share/fonts/truetype/dejavu"))
    args = parser.parse_args()
    for name, file_name in [("Guide", "DejaVuSans.ttf"), ("GuideBold", "DejaVuSans-Bold.ttf")]:
        pdfmetrics.registerFont(TTFont(name, str(args.font_dir / file_name)))
    pdfmetrics.registerFontFamily("Guide", normal="Guide", bold="GuideBold")
    for language in ("English", "Ellinika", "Cesky"):
        render(MANUALS / (language + ".html"))
