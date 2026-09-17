from __future__ import annotations

# Rebuild the customer PDF from Vietnamese Markdown and original GUI PNGs.
# Runbook: engineering/infra/CUSTOMER_PDF.md.

import html
import re
from io import BytesIO
from pathlib import Path

from PIL import Image as PILImage
from reportlab.graphics.shapes import Drawing, Line, Polygon, Rect, String
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate,
    Frame,
    Flowable,
    Image,
    KeepTogether,
    ListFlowable,
    ListItem,
    NextPageTemplate,
    PageBreak,
    PageTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
)
from reportlab.platypus.tableofcontents import TableOfContents


ROOT = Path(__file__).resolve().parents[2]
DOC_DIR = ROOT / "doc"
OUTPUT = DOC_DIR / "QuanLyHoSo_TaiLieu_KhachHang_1.0.0.pdf"

NAVY = colors.HexColor("#12355B")
BLUE = colors.HexColor("#1769AA")
SKY = colors.HexColor("#EAF4FB")
TEAL = colors.HexColor("#198F8C")
GREEN = colors.HexColor("#E8F5EF")
INK = colors.HexColor("#243447")
MUTED = colors.HexColor("#617184")
LINE_COLOR = colors.HexColor("#D6E0EA")
WHITE = colors.white

pdfmetrics.registerFont(TTFont("Arial", r"C:\Windows\Fonts\arial.ttf"))
pdfmetrics.registerFont(TTFont("Arial-Bold", r"C:\Windows\Fonts\arialbd.ttf"))
pdfmetrics.registerFont(TTFont("Arial-Italic", r"C:\Windows\Fonts\ariali.ttf"))
pdfmetrics.registerFont(TTFont("Arial-BoldItalic", r"C:\Windows\Fonts\arialbi.ttf"))
pdfmetrics.registerFontFamily("Arial", normal="Arial", bold="Arial-Bold", italic="Arial-Italic", boldItalic="Arial-BoldItalic")


def inline_markup(text: str) -> str:
    text = html.escape(text.strip())
    text = re.sub(r"!\[([^]]*)\]\([^)]+\)", r"\1", text)
    text = re.sub(r"\[([^]]+)\]\([^)]+\)", r"\1", text)
    text = re.sub(r"`([^`]+)`", r'<font name="Arial">\1</font>', text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"(?<!\*)\*([^*]+)\*(?!\*)", r"<i>\1</i>", text)
    return text


styles = getSampleStyleSheet()
styles.add(ParagraphStyle(
    name="BodyVN", fontName="Arial", fontSize=10.5, leading=16,
    textColor=INK, spaceAfter=7, alignment=TA_LEFT, allowWidows=0, allowOrphans=0,
))
styles.add(ParagraphStyle(
    name="SmallVN", parent=styles["BodyVN"], fontSize=9.2, leading=13.4, textColor=INK,
))
styles.add(ParagraphStyle(name="TableHeaderVN", parent=styles["SmallVN"], fontName="Arial-Bold", textColor=WHITE))
styles.add(ParagraphStyle(name="H4VN", parent=styles["BodyVN"], fontName="Arial-Bold", textColor=NAVY, spaceBefore=8, keepWithNext=True))
styles.add(ParagraphStyle(
    name="H1VN", fontName="Arial-Bold", fontSize=19, leading=23,
    textColor=NAVY, spaceBefore=8, spaceAfter=12, keepWithNext=True,
))
styles.add(ParagraphStyle(
    name="H2VN", fontName="Arial-Bold", fontSize=14, leading=18,
    textColor=BLUE, spaceBefore=12, spaceAfter=7, keepWithNext=True,
))
styles.add(ParagraphStyle(
    name="H3VN", fontName="Arial-Bold", fontSize=11.2, leading=15,
    textColor=TEAL, spaceBefore=8, spaceAfter=5, keepWithNext=True,
))
styles.add(ParagraphStyle(
    name="CaptionVN", fontName="Arial", fontSize=9, leading=13,
    textColor=MUTED, alignment=TA_CENTER, spaceBefore=6, spaceAfter=12,
))
styles.add(ParagraphStyle(
    name="QuoteVN", parent=styles["BodyVN"], backColor=SKY, borderColor=BLUE,
    borderWidth=0, borderPadding=(7, 8, 7, 10), leftIndent=5, rightIndent=5,
    textColor=NAVY, spaceBefore=4, spaceAfter=8,
))
styles.add(ParagraphStyle(
    name="CodeVN", fontName="Arial", fontSize=9, leading=14,
    backColor=colors.HexColor("#F4F6F8"), borderColor=LINE_COLOR,
    borderWidth=0.5, borderPadding=7, textColor=INK, spaceBefore=4, spaceAfter=8,
))
styles.add(ParagraphStyle(
    name="PartVN", fontName="Arial-Bold", fontSize=28, leading=33,
    textColor=NAVY, alignment=TA_CENTER, spaceAfter=12,
))
styles.add(ParagraphStyle(
    name="PartSubVN", fontName="Arial", fontSize=12, leading=18,
    textColor=MUTED, alignment=TA_CENTER,
))
styles.add(ParagraphStyle(
    name="TOC1VN", fontName="Arial-Bold", fontSize=10.5, leading=15,
    textColor=NAVY, leftIndent=0, firstLineIndent=0, spaceBefore=4,
))
styles.add(ParagraphStyle(
    name="TOC2VN", fontName="Arial", fontSize=9, leading=13,
    textColor=INK, leftIndent=14, firstLineIndent=0,
))
styles.add(ParagraphStyle(
    name="TOCTitleVN", parent=styles["H1VN"], keepWithNext=True,
))
styles.add(ParagraphStyle(
    name="BulletVN", parent=styles["BodyVN"], leftIndent=17, firstLineIndent=0,
    bulletIndent=2, bulletFontName="Arial", bulletFontSize=10, spaceAfter=4,
))


class CustomerDocTemplate(BaseDocTemplate):
    def __init__(self, filename: str):
        super().__init__(
            filename,
            pagesize=A4,
            rightMargin=17 * mm,
            leftMargin=17 * mm,
            topMargin=20 * mm,
            bottomMargin=18 * mm,
            title="Bộ tài liệu khách hàng - Phần mềm Quản lý hồ sơ",
            author="QuanLyHoSo",
            subject="Thiết kế tổng thể, cài đặt, hướng dẫn sử dụng và phụ lục vận hành",
            creator="QuanLyHoSo Documentation",
        )
        frame = Frame(self.leftMargin, self.bottomMargin, self.width, self.height, id="content", leftPadding=0, rightPadding=0, topPadding=0, bottomPadding=0)
        self.addPageTemplates([
            PageTemplate(id="Cover", frames=frame, onPage=self.cover_page),
            PageTemplate(id="Content", frames=frame, onPage=self.content_page),
        ])

    def cover_page(self, canvas, doc):
        canvas.saveState()
        canvas.setFillColor(NAVY)
        canvas.rect(0, 0, A4[0], A4[1], fill=1, stroke=0)
        canvas.setFillColor(BLUE)
        canvas.circle(A4[0] - 24 * mm, A4[1] - 24 * mm, 42 * mm, fill=1, stroke=0)
        canvas.setFillColor(TEAL)
        canvas.circle(10 * mm, 8 * mm, 35 * mm, fill=1, stroke=0)
        canvas.restoreState()

    def content_page(self, canvas, doc):
        canvas.saveState()
        pw, ph = canvas._pagesize
        canvas.setStrokeColor(LINE_COLOR)
        canvas.setLineWidth(0.5)
        canvas.line(doc.leftMargin, ph - 13 * mm, pw - doc.rightMargin, ph - 13 * mm)
        canvas.setFont("Arial-Bold", 7.6)
        canvas.setFillColor(NAVY)
        canvas.drawString(doc.leftMargin, ph - 10 * mm, "QUẢN LÝ HỒ SƠ")
        canvas.setFont("Arial", 7.6)
        canvas.setFillColor(MUTED)
        canvas.drawRightString(pw - doc.rightMargin, ph - 10 * mm, "BỘ TÀI LIỆU KHÁCH HÀNG")
        canvas.line(doc.leftMargin, 12 * mm, pw - doc.rightMargin, 12 * mm)
        canvas.setFont("Arial", 7.4)
        canvas.drawString(doc.leftMargin, 8 * mm, "Phiên bản 1.0.0 | Cập nhật 17/09/2026")
        canvas.drawRightString(pw - doc.rightMargin, 8 * mm, f"Trang {doc.page}")
        canvas.restoreState()

    def afterFlowable(self, flowable):
        if isinstance(flowable, Paragraph):
            style_name = flowable.style.name
            if style_name in {"PartVN", "H1VN", "H2VN"}:
                level = 0 if style_name in {"PartVN", "H1VN"} else 1
                text = flowable.getPlainText()
                key = f"heading-{id(flowable)}"
                self.canv.bookmarkPage(key)
                self.canv.addOutlineEntry(text, key, level=level, closed=False)
                self.notify("TOCEntry", (level, text, self.page, key))


def part_page(part_no: str, title: str, subtitle: str):
    return [
        PageBreak(),
        Spacer(1, 52 * mm),
        Paragraph(f"PHẦN {part_no}", styles["PartSubVN"]),
        Spacer(1, 5 * mm),
        Paragraph(title, styles["PartVN"]),
        Paragraph(subtitle, styles["PartSubVN"]),
        Spacer(1, 50 * mm),
        Table([[""]], colWidths=[65 * mm], rowHeights=[2 * mm], style=TableStyle([
            ("BACKGROUND", (0, 0), (-1, -1), TEAL),
        ]), hAlign="CENTER"),
        PageBreak(),
    ]


def arrow(d: Drawing, x1, y1, x2, y2, color=BLUE):
    d.add(Line(x1, y1, x2, y2, strokeColor=color, strokeWidth=1.5))
    angle = 5
    if abs(x2 - x1) >= abs(y2 - y1):
        direction = 1 if x2 >= x1 else -1
        points = [x2, y2, x2 - direction * 7, y2 + angle, x2 - direction * 7, y2 - angle]
    else:
        direction = 1 if y2 >= y1 else -1
        points = [x2, y2, x2 - angle, y2 - direction * 7, x2 + angle, y2 - direction * 7]
    d.add(Polygon(points, fillColor=color, strokeColor=color))


def box(d: Drawing, x, y, w, h, title, subtitle="", fill=SKY):
    d.add(Rect(x, y, w, h, rx=6, ry=6, fillColor=fill, strokeColor=BLUE, strokeWidth=1))
    d.add(String(x + w / 2, y + h / 2 + (5 if subtitle else 0), title,
                 fontName="Arial-Bold", fontSize=9, fillColor=NAVY, textAnchor="middle"))
    if subtitle:
        d.add(String(x + w / 2, y + h / 2 - 9, subtitle,
                     fontName="Arial", fontSize=7.4, fillColor=MUTED, textAnchor="middle"))


def diagram(kind: int):
    w = 475
    if kind == 0:
        d = Drawing(w, 205)
        box(d, 5, 65, 95, 80, "NGƯỜI DÙNG", "Admin | Lãnh đạo | Cán bộ", GREEN)
        box(d, 127, 65, 100, 80, "CLIENT", "Giao diện và kiểm tra nhập", SKY)
        box(d, 254, 65, 100, 80, "SERVER", "Xác thực và nghiệp vụ", colors.HexColor("#EDEBFA"))
        box(d, 381, 65, 89, 80, "LƯU TRỮ", "SQLite | Tệp | Sao lưu", colors.HexColor("#FFF3E5"))
        arrow(d, 100, 105, 127, 105)
        arrow(d, 227, 105, 254, 105)
        arrow(d, 354, 105, 381, 105)
        d.add(String(w / 2, 180, "SƠ ĐỒ THÀNH PHẦN HỆ THỐNG", fontName="Arial-Bold", fontSize=11, fillColor=NAVY, textAnchor="middle"))
        d.add(String(w / 2, 43, "Trao đổi qua HTTP trong mạng LAN - Client không truy cập trực tiếp SQLite", fontName="Arial", fontSize=8, fillColor=MUTED, textAnchor="middle"))
        return d
    if kind == 1:
        d = Drawing(w, 210)
        d.add(String(w / 2, 188, "MÔ HÌNH TRIỂN KHAI TRONG MẠNG NỘI BỘ", fontName="Arial-Bold", fontSize=11, fillColor=NAVY, textAnchor="middle"))
        for y, label in [(130, "Máy trạm 1"), (78, "Máy trạm 2"), (26, "Máy trạm khác")]:
            box(d, 25, y, 125, 36, label, "QuanLyHoSo Client", SKY)
            arrow(d, 150, y + 18, 305, 103)
        box(d, 305, 70, 145, 68, "MÁY SERVER", "API | SQLite | Sao lưu", colors.HexColor("#EDEBFA"))
        d.add(String(228, 110, "Cổng 5055", fontName="Arial-Bold", fontSize=8, fillColor=BLUE, textAnchor="middle"))
        return d
    if kind == 2:
        d = Drawing(w, 205)
        labels = ["Tiếp nhận", "Phân loại", "Phân công", "Xác minh", "Kết quả"]
        xs = [5, 101, 197, 293, 389]
        for i, (x, label) in enumerate(zip(xs, labels)):
            box(d, x, 115, 80, 42, label, fill=SKY if i < 4 else GREEN)
            if i < 4:
                arrow(d, x + 80, 136, xs[i + 1], 136)
        box(d, 173, 35, 128, 42, "Chờ bổ sung", "Quay lại bước xác minh", colors.HexColor("#FFF3E5"))
        arrow(d, 333, 115, 285, 77, TEAL)
        arrow(d, 173, 56, 90, 112, TEAL)
        d.add(String(w / 2, 185, "VÒNG ĐỜI HỒ SƠ", fontName="Arial-Bold", fontSize=11, fillColor=NAVY, textAnchor="middle"))
        return d
    d = Drawing(w, 230)
    actors = [(45, "Người dùng"), (160, "Client"), (285, "Server"), (420, "SQLite")]
    for x, label in actors:
        box(d, x - 38, 183, 76, 30, label, fill=SKY)
        d.add(Line(x, 183, x, 20, strokeColor=LINE_COLOR, strokeWidth=1, strokeDashArray=[3, 3]))
    events = [
        (45, 160, 155, "Nhập và lưu"),
        (160, 285, 125, "Gửi yêu cầu + phiên bản"),
        (285, 420, 95, "Kiểm tra và lưu"),
        (420, 285, 65, "Kết quả"),
        (285, 160, 35, "Phản hồi"),
    ]
    for x1, x2, y, label in events:
        arrow(d, x1, y, x2, y, TEAL if x1 > x2 else BLUE)
        d.add(String((x1 + x2) / 2, y + 5, label, fontName="Arial", fontSize=7.3, fillColor=INK, textAnchor="middle"))
    d.add(String(w / 2, 222, "LUỒNG TRAO ĐỔI DỮ LIỆU", fontName="Arial-Bold", fontSize=11, fillColor=NAVY, textAnchor="middle"))
    return d


def make_table(rows):
    parsed = [[Paragraph(inline_markup(cell.strip()), styles["TableHeaderVN" if i == 0 else "SmallVN"]) for cell in row] for i, row in enumerate(rows)]
    cols = max(len(r) for r in rows)
    available = 176 * mm
    if cols == 4:
        widths = [57 * mm, 31 * mm, 38 * mm, 50 * mm]
    elif cols == 3:
        widths = [48 * mm, 58 * mm, 70 * mm]
    elif cols == 2:
        widths = [48 * mm, 128 * mm]
    else:
        widths = [available / cols] * cols
    table = Table(parsed, colWidths=widths, repeatRows=1, hAlign="LEFT")
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), NAVY),
        ("TEXTCOLOR", (0, 0), (-1, 0), WHITE),
        ("FONTNAME", (0, 0), (-1, 0), "Arial-Bold"),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LINEBELOW", (0, 0), (-1, 0), 0.8, BLUE),
        ("LINEBELOW", (0, 1), (-1, -1), 0.35, LINE_COLOR),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [WHITE, colors.HexColor("#F7FAFC")]),
        ("LEFTPADDING", (0, 0), (-1, -1), 8),
        ("RIGHTPADDING", (0, 0), (-1, -1), 8),
        ("TOPPADDING", (0, 0), (-1, -1), 7),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
    ]))
    return KeepTogether([table])


CROPS = {
    # Viewports in original PNG pixels. PDF clips the original, lossless image;
    # it never resamples or rewrites customer screenshots.
    "2026-09-11_21h16_25.png": (1160, 222, 1705, 790),
    "2026-09-11_21h28_41.png": (1160, 190, 1710, 835),
    "2026-09-16_21h53_33.png": (570, 158, 1570, 900),
    "2026-09-16_21h55_27.png": (620, 185, 1510, 860),
    "2026-09-16_21h23_52.png": (520, 175, 1630, 865),
    "2026-09-16_21h34_53.png": (700, 127, 1440, 930),
    "2026-09-16_21h59_43.png": (700, 127, 1440, 930),
    "2026-09-16_22h00_29.png": (600, 245, 1540, 790),
    "2026-09-16_22h00_37.png": (840, 315, 1300, 715),
    "2026-09-17_20h11_58.png": (680, 230, 1460, 795),
    "2026-09-17_20h12_36.png": (570, 174, 1570, 882),
    "2026-09-17_20h12_14.png": (600, 255, 1540, 795),
}


class OriginalImage(Flowable):
    def __init__(self, path, viewport, max_w, max_h):
        super().__init__()
        self.path = str(path)
        self.iw, self.ih = PILImage.open(path).size
        self.viewport = viewport or (0, 0, self.iw, self.ih)
        x0, y0, x1, y1 = self.viewport
        self.scale = min(max_w / (x1-x0), max_h / (y1-y0))
        self.width, self.height = (x1-x0)*self.scale, (y1-y0)*self.scale
        self.hAlign = "CENTER"

    def draw(self):
        c = self.canv
        x0, y0, x1, y1 = self.viewport
        c.saveState()
        clip = c.beginPath()
        clip.rect(0, 0, self.width, self.height)
        c.clipPath(clip, stroke=0)
        c.drawImage(self.path, -x0*self.scale, (y1-self.ih)*self.scale,
                    width=self.iw*self.scale, height=self.ih*self.scale, mask="auto")
        c.restoreState()
        c.setStrokeColor(LINE_COLOR)
        c.setLineWidth(0.5)
        c.rect(0, 0, self.width, self.height, stroke=1, fill=0)


figure_number = 0


def image_flowables(source: Path, rel_path: str, caption: str, story):
    global figure_number
    path = (source.parent / rel_path).resolve()
    if not path.exists():
        raise FileNotFoundError(path)
    # The installation screenshot shows the actual server control panel;
    # the former illustration instead showed an unrelated reset dialog.
    if path.name == "2026-09-16_22h01_25.png":
        path = DOC_DIR / "GUI/installation/2026-09-17_18h17_00.png"
    figure_number += 1
    iw, ih = PILImage.open(path).size
    crop = CROPS.get(path.name)
    if caption == "Bộ lọc danh sách hồ sơ":
        crop = (235, 20, 1900, 355)
    elif caption == "Chế độ chọn nhiều hồ sơ":
        crop = (235, 315, 1900, 755)
    elif caption == "Khu vực kiểm tra cập nhật phần mềm":
        crop = (240, 455, 1070, 685)
    elif caption == "Sao lưu và khôi phục dữ liệu":
        crop = (240, 420, 1070, 1010)
    label = f"Hình {figure_number:02d}. {inline_markup(caption)}"
    viewport = crop
    if "installation" in path.parts and path.name != "2026-09-17_18h17_00.png":
        viewport = (0, 32, iw, ih)
    # All pages remain A4 portrait. Wide screens use the full content width,
    # preserving their aspect ratio and original pixels without page rotation.
    max_w = 176*mm if crop or iw >= 1900 else 145*mm
    if path.name in {"2026-09-11_21h16_25.png", "2026-09-11_21h28_41.png", "2026-09-16_22h00_37.png"}:
        max_w = 110*mm
    carried = []
    while story and isinstance(story[-1], Paragraph) and story[-1].style.name in {"H2VN", "H3VN", "H4VN"}:
        carried.insert(0, story.pop())
    if not carried and len(story) >= 2 and isinstance(story[-1], Paragraph) and isinstance(story[-2], Paragraph) and story[-1].style.name == "BodyVN" and story[-2].style.name in {"H2VN", "H3VN", "H4VN"}:
        carried = story[-2:]
        del story[-2:]
    return [KeepTogether([*carried, Spacer(1, 3*mm), OriginalImage(path, viewport, max_w, 145*mm),
                          Paragraph(label, styles["CaptionVN"])])]


def markdown_story(source: Path):
    lines = source.read_text(encoding="utf-8").splitlines()
    story = []
    i = 0
    diagram_index = 0
    skip_local_toc = False
    while i < len(lines):
        raw = lines[i]
        line = raw.strip()
        if skip_local_toc:
            if line.startswith("## ") and line != "## Mục lục":
                skip_local_toc = False
            else:
                i += 1
                continue
        if line == "## Mục lục":
            skip_local_toc = True
            i += 1
            continue
        if not line or line == "---":
            i += 1
            continue
        if line == "GitHub hỗ trợ hiển thị trực tiếp sơ đồ Mermaid dưới đây.":
            i += 1
            continue
        if line.startswith("```"):
            language = line[3:].strip().lower()
            block = []
            i += 1
            while i < len(lines) and not lines[i].strip().startswith("```"):
                block.append(lines[i])
                i += 1
            i += 1
            if language == "mermaid":
                drawing = diagram(diagram_index)
                carried = []
                while story and isinstance(story[-1], Paragraph) and story[-1].style.name in {"H2VN", "BodyVN"}:
                    carried.insert(0, story.pop())
                    if carried[0].style.name == "H2VN":
                        break
                story.extend([KeepTogether([*carried, drawing]), Spacer(1, 5 * mm)])
                diagram_index += 1
            else:
                story.append(Paragraph("<br/>".join(html.escape(x) for x in block), styles["CodeVN"]))
            continue
        image_match = re.match(r"!\[([^]]*)\]\(([^)]+)\)", line)
        if image_match:
            story.extend(image_flowables(source, image_match.group(2), image_match.group(1), story))
            i += 1
            continue
        heading = re.match(r"^(#{1,4})\s+(.+)$", line)
        if heading:
            level = len(heading.group(1))
            story.append(Paragraph(inline_markup(heading.group(2)), styles[{1: "H1VN", 2: "H2VN", 3: "H3VN", 4: "H4VN"}[level]]))
            i += 1
            continue
        if line.startswith("|"):
            table_lines = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                table_lines.append(lines[i].strip())
                i += 1
            rows = [[cell.strip() for cell in row.strip("|").split("|")] for row in table_lines]
            if len(rows) > 1 and all(re.fullmatch(r":?-{3,}:?", c.replace(" ", "")) for c in rows[1]):
                rows.pop(1)
            story.extend([make_table(rows), Spacer(1, 4 * mm)])
            continue
        if re.match(r"^[-*]\s+", line) or re.match(r"^\d+\.\s+", line):
            ordered = bool(re.match(r"^\d+\.\s+", line))
            items = []
            pattern = r"^\d+\.\s+" if ordered else r"^[-*]\s+"
            item_number = 1
            while i < len(lines) and re.match(pattern, lines[i].strip()):
                text = re.sub(pattern, "", lines[i].strip())
                marker = re.match(r"^(\d+)\.", lines[i].strip()).group(1) + "." if ordered else "•"
                indent = len(lines[i]) - len(lines[i].lstrip())
                item_style = styles["BulletVN"]
                if indent:
                    item_style = ParagraphStyle("NestedBulletVN", parent=item_style, leftIndent=30, bulletIndent=16)
                items.append(Paragraph(inline_markup(text), item_style, bulletText=marker))
                item_number += 1
                i += 1
            if len(items) <= 8:
                story.append(KeepTogether(items))
            else:
                story.extend(items)
            continue
        if line.startswith(">"):
            quote = []
            while i < len(lines) and lines[i].strip().startswith(">"):
                quote.append(lines[i].strip().lstrip("> "))
                i += 1
            story.append(Paragraph(inline_markup(" ".join(quote)), styles["QuoteVN"]))
            continue
        para = [line]
        i += 1
        while i < len(lines):
            nxt = lines[i].strip()
            if (not nxt or nxt == "---" or nxt.startswith("#") or nxt.startswith("|") or
                    nxt.startswith("```") or nxt.startswith(">") or nxt.startswith("![") or
                    re.match(r"^[-*]\s+", nxt) or re.match(r"^\d+\.\s+", nxt)):
                break
            para.append(nxt)
            i += 1
        story.append(Paragraph(inline_markup(" ".join(para)), styles["BodyVN"]))
    # Explicitly bind leads to a following atomic list/table/figure. ReportLab's
    # automatic keepWithNext can otherwise leave a heading before KeepTogether.
    for j in range(len(story)-2, -1, -1):
        lead, following = story[j:j+2]
        if isinstance(lead, Paragraph) and isinstance(following, KeepTogether) and (
            getattr(lead.style, "keepWithNext", False) or lead.getPlainText().endswith(":")):
            story[j:j+2] = [KeepTogether([lead, *following._content])]
    return story


def appendix_story():
    role_rows = [
        ["Phạm vi", "Admin", "Lãnh đạo", "Cán bộ"],
        ["Tổng quan", "Toàn hệ thống", "Toàn hệ thống", "Chỉ dữ liệu/hồ sơ của chính mình"],
        ["Danh sách hồ sơ", "Toàn hệ thống", "Toàn hệ thống - chủ yếu chỉ xem", "Hồ sơ được giao"],
        ["Theo dõi cán bộ", "Toàn hệ thống", "Toàn hệ thống", "Chỉ bản thân"],
        ["Quản trị hệ thống", "Có", "Không", "Không"],
    ]
    checklist = [
        "Đã sao lưu database trước khi nâng cấp Server.",
        "Server và toàn bộ Client có cùng số phiên bản.",
        "Server hiển thị trạng thái đang hoạt động và đúng địa chỉ kết nối.",
        "Client đăng nhập, xem Tổng quan và tải danh sách hồ sơ thành công.",
        "Tài khoản Cán bộ chỉ xem Tổng quan và hồ sơ thuộc phạm vi cá nhân.",
        "Đã kiểm tra thao tác thêm hồ sơ, xử lý hồ sơ và tải gói cập nhật.",
    ]
    story = part_page("IV", "PHỤ LỤC VẬN HÀNH", "Tra cứu nhanh khi triển khai và nghiệm thu")
    story.extend([
        Paragraph("A. Ma trận quyền tra cứu nhanh", styles["H1VN"]),
        Paragraph("Bảng dưới đây tóm tắt phạm vi dữ liệu quan trọng. Quyền thực tế được Server kiểm tra lại ở mỗi yêu cầu.", styles["BodyVN"]),
        make_table(role_rows),
        Spacer(1, 5 * mm),
        Paragraph("B. Checklist cập nhật và nghiệm thu", styles["H1VN"]),
        *[Paragraph("[ ] " + inline_markup(x), styles["BulletVN"]) for x in checklist],
        Paragraph("C. Thông tin cần gửi khi yêu cầu hỗ trợ", styles["H1VN"]),
        Paragraph("Cung cấp tên máy, vai trò tài khoản, thời điểm xảy ra lỗi, thao tác vừa thực hiện, ảnh chụp thông báo, phiên bản Client/Server và file nhật ký liên quan. Không gửi mật khẩu hoặc dữ liệu nhạy cảm qua kênh công khai.", styles["BodyVN"]),
        Paragraph("D. Quy tắc phiên bản", styles["H1VN"]),
        Paragraph("Client gửi phiên bản trong mỗi kết nối. Server chỉ cho phép đăng nhập và thao tác nghiệp vụ khi phiên bản Client khớp chính xác phiên bản Server. API kiểm tra trạng thái vẫn phản hồi để bộ cài và công cụ chẩn đoán xác định phiên bản cần dùng.", styles["QuoteVN"]),
    ])
    return story


def build():
    global figure_number
    figure_number = 0
    doc = CustomerDocTemplate(str(OUTPUT))
    toc = TableOfContents()
    toc.levelStyles = [styles["TOC1VN"], styles["TOC2VN"]]
    story = [
        NextPageTemplate("Cover"),
        Spacer(1, 36 * mm),
        Paragraph("BỘ TÀI LIỆU KHÁCH HÀNG", ParagraphStyle(
            "CoverKicker", fontName="Arial-Bold", fontSize=11, leading=14,
            textColor=colors.HexColor("#9FD8FF"), alignment=TA_CENTER, spaceAfter=12,
        )),
        Paragraph("PHẦN MỀM<br/>QUẢN LÝ HỒ SƠ", ParagraphStyle(
            "CoverTitle", fontName="Arial-Bold", fontSize=31, leading=38,
            textColor=WHITE, alignment=TA_CENTER, spaceAfter=15,
        )),
        Paragraph("Thiết kế tổng thể · Cài đặt Server/Client · Hướng dẫn sử dụng · Phụ lục vận hành", ParagraphStyle(
            "CoverSub", fontName="Arial", fontSize=12, leading=19,
            textColor=colors.HexColor("#D9ECF8"), alignment=TA_CENTER,
        )),
        Spacer(1, 55 * mm),
        Paragraph("PHIÊN BẢN 1.0.0", ParagraphStyle(
            "CoverVersion", fontName="Arial-Bold", fontSize=11, leading=14,
            textColor=WHITE, alignment=TA_CENTER,
        )),
        Paragraph("Cập nhật ngày 17/09/2026", ParagraphStyle(
            "CoverDate", fontName="Arial", fontSize=9, leading=13,
            textColor=colors.HexColor("#D9ECF8"), alignment=TA_CENTER,
        )),
        NextPageTemplate("Content"),
        PageBreak(),
        Paragraph("MỤC LỤC", styles["TOCTitleVN"]),
        Paragraph("Bộ tài liệu được tổ chức theo trình tự: hiểu hệ thống, triển khai, sử dụng và kiểm tra vận hành.", styles["BodyVN"]),
        Spacer(1, 4 * mm),
        toc,
    ]

    documents = [
        ("I", "THIẾT KẾ TỔNG THỂ HỆ THỐNG", "Kiến trúc, phân quyền, dữ liệu và nguyên tắc vận hành", DOC_DIR / "Thiet_Ke_He_Thong_Quan_Ly_Ho_So_WPF_NET5.md"),
        ("II", "HƯỚNG DẪN CÀI ĐẶT", "Triển khai Server và Client trong mạng nội bộ", DOC_DIR / "Huong_dan_cai_dat_Server_Client.md"),
        ("III", "HƯỚNG DẪN SỬ DỤNG", "Thao tác theo vai trò Admin, Lãnh đạo và Cán bộ", DOC_DIR / "Huong_dan_su_dung_QuanLyHoSo.md"),
    ]
    for part_no, title, subtitle, source in documents:
        story.extend(part_page(part_no, title, subtitle))
        story.extend(markdown_story(source))
    story.extend(appendix_story())
    doc.multiBuild(story)
    print(OUTPUT)


if __name__ == "__main__":
    build()
