"""Build the three illustrated A4 manuals from their offline HTML editions."""
import argparse
from pathlib import Path
from xml.sax.saxutils import escape

from lxml import html
from PIL import Image
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen.canvas import Canvas
from reportlab.platypus import Paragraph

MANUALS = Path(__file__).resolve().parent.parent / "Manuals"
W, H = A4
LEFT, RIGHT = 43, W - 43
WIDTH = RIGHT - LEFT
INK, MUTED, TEAL = "#18323A", "#425C63", "#116B61"
DARK, PAPER, LIGHT, LINE = "#153E40", "#FFFFFF", "#EDF4F1", "#CCDAD5"
WARM, GOLD = "#F8F0DF", "#C89045"


def color(value):
    return colors.HexColor(value)


def markup(node):
    parts = [escape(node.text or "")]
    for child in node:
        text = escape(child.text_content())
        parts.append("<b>" + text + "</b>" if child.tag == "strong" else text)
        parts.append(escape(child.tail or ""))
    return "".join(parts)


def paragraph(content, width, size, leading, bold=False, ink=INK):
    style = ParagraphStyle("body", fontName="GuideBold" if bold else "Guide",
                           fontSize=size, leading=leading, textColor=color(ink))
    p = Paragraph(content, style)
    height = p.wrap(width, H)[1]
    return p, height


def write(page, text, x, top, width, size, leading, bold=False, ink=INK):
    p, height = paragraph(text, width, size, leading, bold, ink)
    p.drawOn(page, x, top - height)
    return top - height


def box(page, x, top, width, height, fill):
    page.setFillColor(color(fill))
    page.rect(x, top - height, width, height, fill=1, stroke=0)


def rule(page, x, right, y, ink=LINE):
    page.setStrokeColor(color(ink))
    page.line(x, y, right, y)


def footer(page, toc_title, num, count):
    rule(page, LEFT, RIGHT, 40)
    page.setFillColor(color(MUTED))
    page.setFont("Guide", 8.5)
    page.drawString(LEFT, 25, toc_title)
    page.linkRect("", "contents", (LEFT, 18, LEFT + 145, 38), thickness=0)
    page.drawRightString(RIGHT, 25, f"{num} / {count}")


def cover(page, doc, chapters, product, title, toc_title):
    page.bookmarkPage("contents")
    page.addOutlineEntry(toc_title, "contents", level=0)
    box(page, 0, H, W, 215, DARK)
    box(page, 0, H - 215, W, 5, "#79BEAB")
    write(page, escape(product), LEFT, H - 34, WIDTH, 12, 16, True, "#B9E4D9")
    write(page, escape(title), LEFT, H - 70, WIDTH, 31, 39, True, PAPER)
    lead = doc.xpath('//p[@class="intro"]')[0].text_content()
    write(page, escape(lead), LEFT, H - 131, WIDTH - 12, 11.5, 17, False, PAPER)
    top = H - 255
    write(page, escape(toc_title), LEFT, top, WIDTH, 21, 26, True)
    width = (WIDTH - 25) / 2
    per_column = (len(chapters) + 1) // 2
    for n, section in enumerate(chapters):
        x = LEFT + (n // per_column) * (width + 25)
        y = top - 45 - (n % per_column) * 41
        name = section.find("h2").text_content()
        write(page, escape(name), x, y, width - 25, 10.3, 13, True)
        page.setFont("Guide", 10)
        page.setFillColor(color(TEAL))
        page.drawRightString(x + width, y - 11, str(n + 2))
        rule(page, x, x + width, y - 33)
        page.linkRect("", section.get("id"), (x, y - 33, x + width, y + 2), thickness=0)
    jump = doc.xpath('//aside[@class="jump"]')[0]
    y = top - 55 - per_column * 41 - 22
    box(page, LEFT, y, WIDTH, 95, LIGHT)
    write(page, escape(jump.find("h2").text_content()), LEFT + 15, y - 12, WIDTH - 30, 13, 17, True, TEAL)
    links = jump.findall("a")
    for i, link in enumerate(links):
        col = (WIDTH - 30) / len(links)
        x = LEFT + 15 + i * col
        dest = link.get("href")[1:]
        page_no = next(n + 2 for n, chapter in enumerate(chapters) if chapter.get("id") == dest)
        write(page, escape(link.text_content()), x, y - 42, col - 9, 9.8, 12.5, True)
        write(page, str(page_no), x, y - 72, col - 10, 9.5, 12, False, TEAL)
        page.linkRect("", dest, (x, y - 86, x + col - 4, y - 39), thickness=0)
    footer(page, toc_title, 1, len(chapters) + 1)
    page.showPage()


def table(page, element, top, scale):
    split = WIDTH * .37
    for index, row in enumerate(element.xpath(".//tr")):
        first, second = row.xpath("./th|./td")
        head = index == 0
        size = (9.2 if head else 9.8) * scale
        leading = 13.7 * scale
        ink = PAPER if head else INK
        a, ha = paragraph(markup(first), split - 20, size, leading, True, ink)
        b, hb = paragraph(markup(second), WIDTH - split - 20, size, leading, head, ink)
        height = max(ha, hb) + 13 * scale
        box(page, LEFT, top, WIDTH, height, DARK if head else LIGHT if index % 2 == 0 else "#FAFCFA")
        a.drawOn(page, LEFT + 10, top - 6 * scale - ha)
        b.drawOn(page, LEFT + split + 10, top - 6 * scale - hb)
        top -= height
        rule(page, LEFT, RIGHT, top)
    return top - 12 * scale


def palette(page, element, top, scale):
    top = write(page, escape(element.find("h3").text_content()), LEFT, top, WIDTH, 12 * scale, 16 * scale, True, TEAL) - 6
    col = WIDTH / 8
    step = 30 * scale
    for i, item in enumerate(element.xpath("./ul/li")):
        x = LEFT + (i % 8) * col
        y = top - (i // 8) * step
        rgb = item.find("span").get("style").split("#")[1]
        page.setFillColor(color("#" + rgb))
        page.setStrokeColor(color("#788B8A"))
        page.setLineWidth(.45)
        page.roundRect(x, y - 9, col - 8, 9, 2, fill=1, stroke=1)
        write(page, escape(item.text_content()), x, y - 11, col - 2, 7.65 * scale, 8.6 * scale)
    page.setLineWidth(.7)
    return top - 4 * step - 7


def figure(page, element, top, scale):
    src = MANUALS / element.find("img").get("src")
    with Image.open(src) as im:
        pw, ph = im.size
    width = min(WIDTH, pw * .75)
    height = width * ph / pw
    page.drawImage(str(src), LEFT, top - height, width=width, height=height)
    page.setStrokeColor(color(LINE))
    page.rect(LEFT, top - height, width, height, fill=0, stroke=1)
    return write(page, escape(element.find("figcaption").text_content()), LEFT, top - height - 4,
                 WIDTH, 8.6 * scale, 12 * scale, False, MUTED) - 9 * scale


def chapter(page, section, product, title, scale):
    write(page, escape(product), LEFT, H - 23, WIDTH * .49, 9.5, 13, True, TEAL)
    page.setFont("Guide", 9.2)
    page.setFillColor(color(MUTED))
    page.drawRightString(RIGHT, H - 34, title)
    rule(page, LEFT, RIGHT, H - 45)
    top = write(page, escape(section.find("h2").text_content()), LEFT, H - 63, WIDTH, 23, 29, True) - 15
    for el in section:
        if el.tag == "h3":
            top = write(page, escape(el.text_content()), LEFT, top, WIDTH, 13.3 * scale,
                        17 * scale, True, TEAL) - 6 * scale
        elif el.tag == "p":
            top = write(page, markup(el), LEFT, top, WIDTH, 10.45 * scale, 15.3 * scale) - 8 * scale
        elif el.tag == "table":
            top = table(page, el, top, scale)
        elif el.tag == "figure":
            top = figure(page, el, top, scale)
        elif el.get("class") == "palette":
            top = palette(page, el, top, scale)
        elif el.get("class") == "tip":
            head, hh = paragraph(escape(el.find("h3").text_content()), WIDTH - 27, 11.1 * scale, 14.5 * scale, True)
            body, hb = paragraph(markup(el.find("p")), WIDTH - 27, 10.1 * scale, 14.5 * scale)
            height = hh + hb + 24 * scale
            box(page, LEFT, top, WIDTH, height, WARM)
            box(page, LEFT, top, 3.5, height, GOLD)
            head.drawOn(page, LEFT + 13, top - 8 * scale - hh)
            body.drawOn(page, LEFT + 13, top - 13 * scale - hh - hb)
            top -= height
    return top


def render(source):
    doc = html.parse(str(source))
    chapters = doc.xpath("//main/section")
    title = doc.xpath("//h1")[0].text_content()
    product = doc.xpath('//p[@class="product"]')[0].text_content()
    toc = doc.xpath('//*[@id="contents"]/h2')[0].text_content()
    page = Canvas(str(source.with_suffix(".pdf")), pagesize=A4, invariant=True)
    page.setTitle(product + " · " + title)
    page.setAuthor("Budget Desk")
    page.setSubject(doc.xpath('//p[@class="intro"]')[0].text_content())
    cover(page, doc, chapters, product, title, toc)
    for n, section in enumerate(chapters, start=2):
        page.bookmarkPage(section.get("id"))
        page.addOutlineEntry(section.find("h2").text_content(), section.get("id"), level=0)
        bottom = chapter(page, section, product, title, 1.05)
        if bottom < 53:
            raise ValueError(f"{source.name}: chapter {section.get('id')} reaches y={bottom:.0f}")
        footer(page, toc, n, len(chapters) + 1)
        page.showPage()
    page.save()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--font-dir", type=Path, default=Path("/usr/share/fonts/truetype/dejavu"))
    args = parser.parse_args()
    for name, filename in [("Guide", "DejaVuSans.ttf"), ("GuideBold", "DejaVuSans-Bold.ttf")]:
        pdfmetrics.registerFont(TTFont(name, str(args.font_dir / filename)))
    pdfmetrics.registerFontFamily("Guide", normal="Guide", bold="GuideBold")
    for lang in ("English", "Ellinika", "Cesky"):
        render(MANUALS / (lang + ".html"))
