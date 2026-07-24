"""Build the non-confidential DOCX template used by the P0 renderer spike."""

from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.style import WD_STYLE_TYPE
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor


ROOT = Path(__file__).resolve().parent
ASSETS = ROOT / "assets"
OUTPUT = ASSETS / "proposal-template.docx"

BLUE = "2E74B5"
DARK_BLUE = "17365D"
TEXT = "30343B"
MUTED = "667085"
PALE = "F4F6F9"
WHITE = "FFFFFF"


def insert_before(parent, element, following_tags):
    following = {qn(tag) for tag in following_tags}
    for index, child in enumerate(parent):
        if child.tag in following:
            parent.insert(index, element)
            return
    parent.append(element)


def set_cell_shading(cell, fill):
    tc_pr = cell._tc.get_or_add_tcPr()
    shading = tc_pr.find(qn("w:shd"))
    if shading is None:
        shading = OxmlElement("w:shd")
        insert_before(
            tc_pr,
            shading,
            (
                "w:noWrap",
                "w:tcMar",
                "w:textDirection",
                "w:tcFitText",
                "w:vAlign",
                "w:hideMark",
            ),
        )
    shading.set(qn("w:val"), "clear")
    shading.set(qn("w:fill"), fill)


def set_cell_margins(cell, top=100, start=120, bottom=100, end=120):
    tc_pr = cell._tc.get_or_add_tcPr()
    margins = tc_pr.first_child_found_in("w:tcMar")
    if margins is None:
        margins = OxmlElement("w:tcMar")
        insert_before(
            tc_pr,
            margins,
            ("w:textDirection", "w:tcFitText", "w:vAlign", "w:hideMark"),
        )
    for name, value in (
        ("top", top),
        ("start", start),
        ("bottom", bottom),
        ("end", end),
    ):
        node = margins.find(qn(f"w:{name}"))
        if node is None:
            node = OxmlElement(f"w:{name}")
            margins.append(node)
        node.set(qn("w:w"), str(value))
        node.set(qn("w:type"), "dxa")


def set_repeat_table_header(row):
    tr_pr = row._tr.get_or_add_trPr()
    repeat = OxmlElement("w:tblHeader")
    tr_pr.append(repeat)


def set_row_cant_split(row):
    tr_pr = row._tr.get_or_add_trPr()
    tr_pr.append(OxmlElement("w:cantSplit"))


def set_table_borders(table, color="D0D5DD", size=6):
    tbl_pr = table._tbl.tblPr
    borders = tbl_pr.first_child_found_in("w:tblBorders")
    if borders is None:
        borders = OxmlElement("w:tblBorders")
        insert_before(
            tbl_pr,
            borders,
            ("w:shd", "w:tblLayout", "w:tblCellMar", "w:tblLook"),
        )
    for edge in ("top", "start", "bottom", "end", "insideH", "insideV"):
        border = borders.find(qn(f"w:{edge}"))
        if border is None:
            border = OxmlElement(f"w:{edge}")
            borders.append(border)
        border.set(qn("w:val"), "single")
        border.set(qn("w:sz"), str(size))
        border.set(qn("w:color"), color)


def set_cell_width(cell, width_twips):
    tc_pr = cell._tc.get_or_add_tcPr()
    tc_w = tc_pr.first_child_found_in("w:tcW")
    if tc_w is None:
        tc_w = OxmlElement("w:tcW")
        tc_pr.append(tc_w)
    tc_w.set(qn("w:w"), str(width_twips))
    tc_w.set(qn("w:type"), "dxa")


def set_paragraph_keep(paragraph, keep_next=False, keep_lines=False):
    p_pr = paragraph._p.get_or_add_pPr()
    if keep_next:
        p_pr.append(OxmlElement("w:keepNext"))
    if keep_lines:
        p_pr.append(OxmlElement("w:keepLines"))
    p_pr.append(OxmlElement("w:widowControl"))


def add_field(paragraph, instruction, display_text=""):
    run = paragraph.add_run()
    begin = OxmlElement("w:fldChar")
    begin.set(qn("w:fldCharType"), "begin")
    instruction_node = OxmlElement("w:instrText")
    instruction_node.set(qn("xml:space"), "preserve")
    instruction_node.text = f" {instruction} "
    separate = OxmlElement("w:fldChar")
    separate.set(qn("w:fldCharType"), "separate")
    text = OxmlElement("w:t")
    text.text = display_text
    end = OxmlElement("w:fldChar")
    end.set(qn("w:fldCharType"), "end")
    for element in (begin, instruction_node, separate, text, end):
        run._r.append(element)


def add_cover_text_box(paragraph):
    xml = f"""
    <w:pict
      xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
      xmlns:v="urn:schemas-microsoft-com:vml">
      <v:roundrect style="width:128pt;height:24pt"
        arcsize="12%" fillcolor="#{PALE}" strokecolor="#{BLUE}" strokeweight="1pt">
        <v:textbox inset="8pt,3pt,8pt,3pt">
          <w:txbxContent>
            <w:p>
              <w:pPr><w:jc w:val="center"/></w:pPr>
              <w:r>
                <w:rPr>
                  <w:b/>
                  <w:color w:val="{DARK_BLUE}"/>
                  <w:sz w:val="18"/>
                </w:rPr>
                <w:t>{{{{ds.Document.Classification}}}}</w:t>
              </w:r>
            </w:p>
          </w:txbxContent>
        </v:textbox>
      </v:roundrect>
    </w:pict>
    """
    paragraph.add_run()._r.append(parse_xml(xml))


def configure_page(section):
    section.different_first_page_header_footer = False
    section.page_width = Inches(8.5)
    section.page_height = Inches(11)
    section.top_margin = Inches(1)
    section.bottom_margin = Inches(1)
    section.left_margin = Inches(1)
    section.right_margin = Inches(1)
    section.header_distance = Inches(0.492)
    section.footer_distance = Inches(0.492)


def set_font(style, size, color=TEXT, bold=False, italic=False):
    style.font.name = "Calibri"
    style.font.size = Pt(size)
    style.font.color.rgb = RGBColor.from_string(color)
    style.font.bold = bold
    style.font.italic = italic
    style._element.rPr.rFonts.set(qn("w:eastAsia"), "Calibri")


def configure_styles(document):
    styles = document.styles

    normal = styles["Normal"]
    set_font(normal, 11)
    normal.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    normal.paragraph_format.space_after = Pt(8)
    normal.paragraph_format.line_spacing = 1.333

    heading_specs = {
        "Heading 1": (16, BLUE, 18, 10),
        "Heading 2": (13, BLUE, 12, 6),
        "Heading 3": (12, DARK_BLUE, 8, 4),
        "Heading 4": (11, DARK_BLUE, 8, 4),
        "Heading 5": (11, TEXT, 6, 3),
        "Heading 6": (10, MUTED, 6, 3),
    }
    for style_name, (size, color, before, after) in heading_specs.items():
        style = styles[style_name]
        set_font(style, size, color, bold=True)
        style.paragraph_format.space_before = Pt(before)
        style.paragraph_format.space_after = Pt(after)
        style.paragraph_format.keep_with_next = True
        style.paragraph_format.keep_together = True

    title = styles["Title"]
    set_font(title, 30, DARK_BLUE, bold=True)
    title.paragraph_format.space_after = Pt(14)

    subtitle = styles["Subtitle"]
    set_font(subtitle, 16, MUTED)
    subtitle.paragraph_format.space_after = Pt(18)

    quote = styles["Quote"]
    set_font(quote, 11, DARK_BLUE, italic=True)
    quote.paragraph_format.left_indent = Inches(0.35)
    quote.paragraph_format.right_indent = Inches(0.2)
    quote.paragraph_format.space_before = Pt(6)
    quote.paragraph_format.space_after = Pt(10)

    caption = styles["Caption"]
    set_font(caption, 9, MUTED, italic=True)
    caption.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.CENTER
    caption.paragraph_format.space_before = Pt(4)
    caption.paragraph_format.space_after = Pt(10)

    if "Code" not in styles:
        code = styles.add_style("Code", WD_STYLE_TYPE.PARAGRAPH)
    else:
        code = styles["Code"]
    code.base_style = normal
    code.font.name = "Consolas"
    code.font.size = Pt(9)
    code.font.color.rgb = RGBColor.from_string(TEXT)
    code.paragraph_format.left_indent = Inches(0.2)
    code.paragraph_format.right_indent = Inches(0.2)
    code.paragraph_format.space_before = Pt(5)
    code.paragraph_format.space_after = Pt(8)
    code._element.rPr.rFonts.set(qn("w:eastAsia"), "Consolas")

    for style_name, color, underline in (
        ("CodeInline", DARK_BLUE, False),
        ("Hyperlink", BLUE, True),
    ):
        if style_name not in styles:
            style = styles.add_style(style_name, WD_STYLE_TYPE.CHARACTER)
        else:
            style = styles[style_name]
        style.font.name = "Consolas" if style_name == "CodeInline" else "Calibri"
        style.font.size = Pt(10 if style_name == "CodeInline" else 11)
        style.font.color.rgb = RGBColor.from_string(color)
        style.font.underline = underline

    if "CoverTitle" not in styles:
        cover_title = styles.add_style("CoverTitle", WD_STYLE_TYPE.PARAGRAPH)
    else:
        cover_title = styles["CoverTitle"]
    set_font(cover_title, 30, DARK_BLUE, bold=True)
    cover_title.paragraph_format.space_after = Pt(14)
    cover_title.paragraph_format.keep_with_next = True

    if "SectionEyebrow" not in styles:
        eyebrow = styles.add_style("SectionEyebrow", WD_STYLE_TYPE.PARAGRAPH)
    else:
        eyebrow = styles["SectionEyebrow"]
    set_font(eyebrow, 10, BLUE, bold=True)
    eyebrow.paragraph_format.space_after = Pt(8)

    if "RevisionHeading" not in styles:
        revision_heading = styles.add_style(
            "RevisionHeading", WD_STYLE_TYPE.PARAGRAPH
        )
    else:
        revision_heading = styles["RevisionHeading"]
    set_font(revision_heading, 13, BLUE, bold=True)
    revision_heading.paragraph_format.space_before = Pt(12)
    revision_heading.paragraph_format.space_after = Pt(6)
    revision_heading.paragraph_format.keep_with_next = True

    if "AkodeTable" not in styles:
        table_style = styles.add_style("AkodeTable", WD_STYLE_TYPE.TABLE)
    else:
        table_style = styles["AkodeTable"]
    table_style.base_style = styles["Table Grid"]
    set_font(table_style, 10)


def configure_header(section, label, include_placeholders=False):
    section.header.is_linked_to_previous = False
    header = section.header
    paragraph = header.paragraphs[0]
    paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    paragraph.paragraph_format.space_after = Pt(0)
    run = paragraph.add_run(label)
    run.bold = True
    run.font.name = "Calibri"
    run.font.size = Pt(9)
    run.font.color.rgb = RGBColor.from_string(BLUE)
    if include_placeholders:
        suffix = paragraph.add_run(
            "  •  {{ds.Document.Project}}  •  v{{ds.Document.Version}}"
        )
        suffix.font.name = "Calibri"
        suffix.font.size = Pt(9)
        suffix.font.color.rgb = RGBColor.from_string(MUTED)


def configure_footer(section, final=False):
    section.footer.is_linked_to_previous = False
    paragraph = section.footer.paragraphs[0]
    paragraph.paragraph_format.space_before = Pt(0)
    paragraph.paragraph_format.space_after = Pt(0)
    if final:
        paragraph.alignment = WD_ALIGN_PARAGRAPH.CENTER
        run = paragraph.add_run("akode.com  •  hello@akode.com")
        run.font.color.rgb = RGBColor.from_string(MUTED)
        run.font.size = Pt(9)
        return

    paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    run = paragraph.add_run(
        "{{ds.Document.Classification}}  •  v{{ds.Document.Version}}  •  "
    )
    run.font.color.rgb = RGBColor.from_string(MUTED)
    run.font.size = Pt(9)
    add_field(paragraph, "PAGE", "1")


def add_brand_band(document, text):
    table = document.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    cell = table.cell(0, 0)
    set_cell_width(cell, 9360)
    set_cell_shading(cell, BLUE)
    set_cell_margins(cell, 180, 220, 180, 220)
    paragraph = cell.paragraphs[0]
    paragraph.alignment = WD_ALIGN_PARAGRAPH.LEFT
    paragraph.paragraph_format.space_after = Pt(0)
    run = paragraph.add_run(text)
    run.bold = True
    run.font.name = "Calibri"
    run.font.size = Pt(17)
    run.font.color.rgb = RGBColor.from_string(WHITE)


def add_metadata_table(document):
    rows = (
        ("Client", "{{ds.Document.Client}}"),
        ("Project", "{{ds.Document.Project}}"),
        (
            "Prepared by",
            "{{ds.Document.Author.FirstName}} {{ds.Document.Author.LastName}}",
        ),
        ("Role", "{{ds.Document.Author.Role}}"),
        ("Email", "{{ds.Document.Author.Email}}"),
        ("Date", "{{ds.Document.Date}}"),
        ("Version / status", "v{{ds.Document.Version}} • {{ds.Document.Status}}"),
    )
    table = document.add_table(rows=0, cols=2)
    table.style = "AkodeTable"
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    set_table_borders(table)
    for label, value in rows:
        cells = table.add_row().cells
        set_cell_width(cells[0], 2400)
        set_cell_width(cells[1], 6960)
        set_cell_shading(cells[0], PALE)
        for cell in cells:
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            set_cell_margins(cell)
        label_run = cells[0].paragraphs[0].add_run(label)
        label_run.bold = True
        label_run.font.color.rgb = RGBColor.from_string(DARK_BLUE)
        cells[1].paragraphs[0].add_run(value)


def add_cover_metadata_table(document):
    rows = (
        ("Client", "{{ds.Document.Client}}"),
        ("Project", "{{ds.Document.Project}}"),
        (
            "Prepared by",
            "{{ds.Document.Author.FirstName}} {{ds.Document.Author.LastName}}"
            " • {{ds.Document.Author.Role}}",
        ),
        ("Email", "{{ds.Document.Author.Email}}"),
        (
            "Date / version",
            "{{ds.Document.Date}} • v{{ds.Document.Version}}"
            " • {{ds.Document.Status}}",
        ),
    )
    table = document.add_table(rows=0, cols=2)
    table.style = "AkodeTable"
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    set_table_borders(table)
    for label, value in rows:
        cells = table.add_row().cells
        set_cell_width(cells[0], 2400)
        set_cell_width(cells[1], 6960)
        set_cell_shading(cells[0], PALE)
        for cell in cells:
            set_cell_margins(cell, 65, 120, 65, 120)
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
        label_run = cells[0].paragraphs[0].add_run(label)
        label_run.bold = True
        label_run.font.color.rgb = RGBColor.from_string(DARK_BLUE)
        cells[1].paragraphs[0].add_run(value)


def add_revision_table(document):
    table = document.add_table(rows=2, cols=4)
    table.style = "AkodeTable"
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    set_table_borders(table)
    widths = (1100, 1800, 2200, 4260)
    for index, (cell, label) in enumerate(
        zip(table.rows[0].cells, ("Version", "Date", "Author", "Change"), strict=True)
    ):
        set_cell_width(cell, widths[index])
        set_cell_shading(cell, DARK_BLUE)
        set_cell_margins(cell)
        cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
        paragraph = cell.paragraphs[0]
        run = paragraph.add_run(label)
        run.bold = True
        run.font.color.rgb = RGBColor.from_string(WHITE)
    set_repeat_table_header(table.rows[0])
    set_row_cant_split(table.rows[0])

    values = (
        "{{#ds.Revisions}}{{.Version}}",
        "{{.Date}}",
        "{{.Author}}",
        "{{.Description}}{{/ds.Revisions}}",
    )
    for index, (cell, value) in enumerate(
        zip(table.rows[1].cells, values, strict=True)
    ):
        set_cell_width(cell, widths[index])
        set_cell_margins(cell)
        cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.TOP
        cell.paragraphs[0].add_run(value)
    set_row_cant_split(table.rows[1])


def add_toc(document):
    document.add_paragraph("{{:ignore}}")
    paragraph = document.add_paragraph()
    add_field(paragraph, 'TOC \\o "1-4" \\h \\z \\u', "Update field in Word")
    document.add_paragraph("{{/:ignore}}")
    note = document.add_paragraph(
        "Page numbers are refreshed automatically when the document opens in Word."
    )
    note.style = "Caption"


def build():
    document = Document()
    document.core_properties.title = "Akode.DocxGen renderer spike template"
    document.core_properties.subject = "P0 rendering-engine validation"
    document.core_properties.author = "Akode.DocxGen"
    document.core_properties.keywords = "docx, markdown, template, spike"

    configure_styles(document)
    for section in document.sections:
        configure_page(section)

    cover_section = document.sections[0]
    cover_section.different_first_page_header_footer = True
    configure_header(cover_section, "AKODE  /  SOLUTION PROPOSAL")
    configure_footer(cover_section)

    add_brand_band(document, "AKODE  /  SYNTHETIC TEMPLATE")
    for _ in range(2):
        document.add_paragraph()
    eyebrow = document.add_paragraph("SOLUTION PROPOSAL", style="SectionEyebrow")
    set_paragraph_keep(eyebrow, keep_next=True)
    title = document.add_paragraph("{{ds.Document.Title}}", style="CoverTitle")
    set_paragraph_keep(title, keep_next=True, keep_lines=True)
    subtitle = document.add_paragraph(
        "{{ds.Document.Description}}", style="Subtitle"
    )
    set_paragraph_keep(subtitle, keep_lines=True)
    project = document.add_paragraph("{{ds.Document.Project}}")
    project.paragraph_format.space_after = Pt(24)
    project.runs[0].font.size = Pt(14)
    project.runs[0].font.color.rgb = RGBColor.from_string(MUTED)
    text_box_paragraph = document.add_paragraph()
    text_box_paragraph.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    add_cover_text_box(text_box_paragraph)
    for _ in range(1):
        document.add_paragraph()
    add_cover_metadata_table(document)

    control_section = document.add_section(WD_SECTION.NEW_PAGE)
    configure_page(control_section)
    configure_header(control_section, "AKODE  /  DOCUMENT CONTROL")
    configure_footer(control_section)
    document.add_paragraph("DOCUMENT GOVERNANCE", style="SectionEyebrow")
    document.add_paragraph("Document Control", style="Title")
    document.add_paragraph(
        "This page identifies the document owner, review state, and controlled revision history."
    )
    add_metadata_table(document)
    document.add_paragraph("Revision history", style="RevisionHeading")
    add_revision_table(document)

    document.add_page_break()
    document.add_paragraph("DOCUMENT NAVIGATION", style="SectionEyebrow")
    document.add_paragraph("Contents", style="Title")
    add_toc(document)

    body_section = document.add_section(WD_SECTION.NEW_PAGE)
    configure_page(body_section)
    configure_header(
        body_section,
        "AKODE.DOCXGEN",
        include_placeholders=True,
    )
    configure_footer(body_section)
    body_slot = document.add_paragraph("{{ds.Body}:MD}")
    body_slot.paragraph_format.space_after = Pt(0)

    final_section = document.add_section(WD_SECTION.NEW_PAGE)
    configure_page(final_section)
    configure_header(final_section, "AKODE")
    configure_footer(final_section, final=True)
    add_brand_band(document, "AKODE  /  THANK YOU")
    for _ in range(6):
        document.add_paragraph()
    closing = document.add_paragraph("MAKE KNOWLEDGE\nWORK HARDER.", style="Title")
    closing.alignment = WD_ALIGN_PARAGRAPH.CENTER
    closing.runs[0].font.size = Pt(28)
    closing.runs[0].font.color.rgb = RGBColor.from_string(DARK_BLUE)
    note = document.add_paragraph(
        "A reusable, governed, and agent-friendly publishing pipeline."
    )
    note.alignment = WD_ALIGN_PARAGRAPH.CENTER
    note.runs[0].font.size = Pt(14)
    note.runs[0].font.color.rgb = RGBColor.from_string(MUTED)
    for _ in range(5):
        document.add_paragraph()
    contact = document.add_paragraph("akode.com  •  hello@akode.com")
    contact.alignment = WD_ALIGN_PARAGRAPH.CENTER
    contact.runs[0].bold = True
    contact.runs[0].font.color.rgb = RGBColor.from_string(BLUE)

    ASSETS.mkdir(parents=True, exist_ok=True)
    document.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    build()
