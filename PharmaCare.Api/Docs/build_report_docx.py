from html.parser import HTMLParser
from html import escape
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from datetime import datetime, timezone
import re

BASE = Path(__file__).parent
HTML = BASE / "PharmaCare_Rubric_Report.html"
OUT = BASE / "PharmaCare_Ho_so_Phan_tich_Thiet_ke_HTTT.docx"
W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"

class Node:
    def __init__(self, tag="", attrs=None, parent=None):
        self.tag, self.attrs, self.parent, self.children = tag, dict(attrs or []), parent, []

class Parser(HTMLParser):
    void = {"meta", "img", "br", "hr", "link"}
    def __init__(self):
        super().__init__(convert_charrefs=True); self.root = Node("root"); self.cur = self.root
    def handle_starttag(self, tag, attrs):
        n = Node(tag, attrs, self.cur); self.cur.children.append(n)
        if tag not in self.void: self.cur = n
    def handle_startendtag(self, tag, attrs): self.handle_starttag(tag, attrs)
    def handle_endtag(self, tag):
        n = self.cur
        while n is not self.root and n.tag != tag: n = n.parent
        if n is not self.root: self.cur = n.parent
    def handle_data(self, data):
        if data: self.cur.children.append(data)

def text_of(n):
    if isinstance(n, str): return n
    return "".join(text_of(c) for c in n.children)

def xtext(s): return escape(re.sub(r"\s+", " ", s).strip(), quote=False)

def run(text, bold=False, italic=False, color=None, size=None, mono=False):
    if not text: return ""
    props = [f'<w:rFonts w:ascii="{"Consolas" if mono else "Calibri"}" w:hAnsi="{"Consolas" if mono else "Calibri"}"/>']
    if bold: props.append("<w:b/>")
    if italic: props.append("<w:i/>")
    if color: props.append(f'<w:color w:val="{color}"/>')
    if size: props.append(f'<w:sz w:val="{size}"/><w:szCs w:val="{size}"/>')
    return f'<w:r><w:rPr>{"".join(props)}</w:rPr><w:t xml:space="preserve">{escape(text, quote=False)}</w:t></w:r>'

def inline_runs(n, inherited=None):
    inherited = inherited or {}
    out = []
    if isinstance(n, str):
        val = re.sub(r"\s+", " ", n)
        if val: out.append(run(val, **inherited))
        return "".join(out)
    state = dict(inherited)
    if n.tag in ("b", "strong"): state["bold"] = True
    if n.tag in ("i", "em"): state["italic"] = True
    if n.tag == "code" or n.attrs.get("class") == "code": state["mono"] = True
    for c in n.children: out.append(inline_runs(c, state))
    return "".join(out)

def paragraph(content="", style=None, align=None, before=0, after=120, keep=False,
              shade=None, border=None, num=None, indent=None):
    ppr = []
    if style: ppr.append(f'<w:pStyle w:val="{style}"/>')
    if align: ppr.append(f'<w:jc w:val="{align}"/>')
    ppr.append(f'<w:spacing w:before="{before}" w:after="{after}" w:line="264" w:lineRule="auto"/>')
    if keep: ppr.append("<w:keepNext/>")
    if shade: ppr.append(f'<w:shd w:val="clear" w:color="auto" w:fill="{shade}"/>')
    if border: ppr.append(f'<w:pBdr><w:left w:val="single" w:sz="20" w:space="8" w:color="{border}"/></w:pBdr>')
    if num is not None: ppr.append(f'<w:numPr><w:ilvl w:val="0"/><w:numId w:val="{num}"/></w:numPr>')
    if indent: ppr.append(f'<w:ind w:left="{indent}"/>')
    return f'<w:p><w:pPr>{"".join(ppr)}</w:pPr>{content}</w:p>'

def page_break(): return '<w:p><w:r><w:br w:type="page"/></w:r></w:p>'

def table(node):
    rows = [n for n in walk_direct(node, "tr")]
    if not rows: return ""
    matrix = []
    for row in rows:
        cells = [c for c in row.children if isinstance(c, Node) and c.tag in ("th", "td")]
        if cells: matrix.append(cells)
    if not matrix: return ""
    cols = max(len(r) for r in matrix); widths = [9360 // cols] * cols; widths[-1] += 9360 - sum(widths)
    grid = "".join(f'<w:gridCol w:w="{w}"/>' for w in widths)
    trs = []
    for ri, row in enumerate(matrix):
        tcs = []
        for ci in range(cols):
            cell = row[ci] if ci < len(row) else Node("td")
            header = cell.tag == "th" or ri == 0 and all(c.tag == "th" for c in row)
            fill = "E8EEF5" if header else "FFFFFF"
            tcpr = f'<w:tcPr><w:tcW w:w="{widths[ci]}" w:type="dxa"/><w:shd w:val="clear" w:fill="{fill}"/><w:vAlign w:val="center"/><w:tcMar><w:top w:w="80" w:type="dxa"/><w:left w:w="120" w:type="dxa"/><w:bottom w:w="80" w:type="dxa"/><w:right w:w="120" w:type="dxa"/></w:tcMar></w:tcPr>'
            content = inline_runs(cell, {"bold": header, "size": 18 if "small" in node.attrs.get("class", "") else 20})
            tcs.append(f'<w:tc>{tcpr}{paragraph(content, after=40)}</w:tc>')
        trpr = '<w:trPr><w:tblHeader/></w:trPr>' if ri == 0 else ""
        trs.append(f'<w:tr>{trpr}{"".join(tcs)}</w:tr>')
    props = '<w:tblPr><w:tblW w:w="9360" w:type="dxa"/><w:tblInd w:w="120" w:type="dxa"/><w:tblLayout w:type="fixed"/><w:tblBorders><w:top w:val="single" w:sz="4" w:color="AAB7C3"/><w:left w:val="single" w:sz="4" w:color="AAB7C3"/><w:bottom w:val="single" w:sz="4" w:color="AAB7C3"/><w:right w:val="single" w:sz="4" w:color="AAB7C3"/><w:insideH w:val="single" w:sz="4" w:color="AAB7C3"/><w:insideV w:val="single" w:sz="4" w:color="AAB7C3"/></w:tblBorders></w:tblPr>'
    return f'<w:tbl>{props}<w:tblGrid>{grid}</w:tblGrid>{"".join(trs)}</w:tbl>{paragraph(after=80)}'

def walk_direct(node, tag):
    for c in node.children:
        if isinstance(c, Node):
            if c.tag == tag: yield c
            else: yield from walk_direct(c, tag)

image_ids = {"system-context.svg": 5, "container-module.svg": 6, "order-sequence.svg": 7, "jwt-flow.svg": 8}
image_sizes = {"system-context.svg": (5669280,2929100), "container-module.svg":(5669280,3590300), "order-sequence.svg":(5669280,3873650), "jwt-flow.svg":(5669280,2456700)}
def image_paragraph(src):
    name = Path(src).name
    if name.endswith(".svg.png"): name = name[:-4]
    rid = image_ids[name]; cx, cy = image_sizes[name]
    pic = f'''<w:r><w:drawing><wp:inline xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" distT="0" distB="0" distL="0" distR="0"><wp:extent cx="{cx}" cy="{cy}"/><wp:docPr id="{rid}" name="{name}" descr="Sơ đồ PharmaCare"/><wp:cNvGraphicFramePr/><a:graphic xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:pic xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:nvPicPr><pic:cNvPr id="{rid}" name="{name}"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed="rId{rid}" xmlns:r="{R}"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="{cx}" cy="{cy}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r>'''
    return paragraph(pic, align="center", before=120, after=60)

def render_node(n):
    if isinstance(n, str) or n.tag in ("head", "style", "script"): return ""
    cls = n.attrs.get("class", "")
    if "pagebreak" in cls: return page_break()
    if n.tag == "h1": return paragraph(inline_runs(n), "Heading1", before=360, after=160, keep=True)
    if n.tag == "h2": return paragraph(inline_runs(n), "Heading2", before=280, after=120, keep=True)
    if n.tag == "h3": return paragraph(inline_runs(n), "Heading3", before=200, after=80, keep=True)
    if n.tag == "p":
        align = "center" if "center" in cls else None
        style = "Caption" if "caption" in cls or "footer-note" in cls else "Normal"
        return paragraph(inline_runs(n), style, align=align)
    if n.tag in ("ul", "ol"):
        return "".join(paragraph(inline_runs(c), "Normal", after=80, num=1 if n.tag == "ul" else 2)
                       for c in n.children if isinstance(c, Node) and c.tag == "li")
    if n.tag == "table": return table(n)
    if n.tag == "div" and "diagram" in cls:
        out = []
        for c in n.children:
            if isinstance(c, Node) and c.tag == "img": out.append(image_paragraph(c.attrs["src"]))
            elif isinstance(c, Node): out.append(render_node(c))
        return "".join(out)
    if n.tag == "div" and "code" in cls:
        lines = text_of(n).splitlines()
        return "".join(paragraph(run(line, mono=True, size=18), shade="F5F7F9", after=0) for line in lines if line.strip()) + paragraph(after=80)
    if n.tag == "div" and ("callout" in cls or "warn" in cls or "good" in cls):
        fill = "FFF7E5" if "warn" in cls else "EFF8F1" if "good" in cls else "F4F6F9"
        color = "B18418" if "warn" in cls else "39824D" if "good" in cls else "2E74B5"
        return paragraph(inline_runs(n), shade=fill, border=color, before=120, after=120)
    if n.tag == "section" and "cover" in cls:
        out = [paragraph(after=1500)]
        for c in n.children:
            if isinstance(c, Node) and c.tag == "h1": out.append(paragraph(inline_runs(c), "DocTitle", align="center", after=160))
            elif isinstance(c, Node) and c.tag == "h2": out.append(paragraph(inline_runs(c), "DocSubtitle", align="center", after=160))
            elif isinstance(c, Node): out.append(render_node(c))
        return "".join(out)
    return "".join(render_node(c) for c in n.children if isinstance(c, Node))

p = Parser(); p.feed(HTML.read_text(encoding="utf-8"))
body_node = next(n for n in p.root.children if isinstance(n, Node) and n.tag == "html")
body = next(n for n in body_node.children if isinstance(n, Node) and n.tag == "body")
content = "".join(render_node(c) for c in body.children if isinstance(c, Node))
sect = '<w:sectPr><w:headerReference w:type="default" r:id="rId3"/><w:footerReference w:type="default" r:id="rId4"/><w:pgSz w:w="12240" w:h="15840"/><w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708"/><w:cols w:space="720"/></w:sectPr>'
document = f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="{W}" xmlns:r="{R}"><w:body>{content}{sect}</w:body></w:document>'

styles = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:styles xmlns:w="{W}">
<w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii="Calibri" w:hAnsi="Calibri"/><w:sz w:val="22"/><w:szCs w:val="22"/></w:rPr></w:rPrDefault></w:docDefaults>
<w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:pPr><w:spacing w:after="120" w:line="264" w:lineRule="auto"/></w:pPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:pPr><w:keepNext/><w:spacing w:before="320" w:after="160"/></w:pPr><w:rPr><w:b/><w:color w:val="2E74B5"/><w:sz w:val="32"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="heading 2"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:pPr><w:keepNext/><w:spacing w:before="240" w:after="120"/></w:pPr><w:rPr><w:b/><w:color w:val="2E74B5"/><w:sz w:val="26"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading3"><w:name w:val="heading 3"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:pPr><w:keepNext/><w:spacing w:before="160" w:after="80"/></w:pPr><w:rPr><w:b/><w:color w:val="1F4D78"/><w:sz w:val="24"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="DocTitle"><w:name w:val="Document Title"/><w:basedOn w:val="Normal"/><w:rPr><w:b/><w:color w:val="0B2545"/><w:sz w:val="54"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="DocSubtitle"><w:name w:val="Document Subtitle"/><w:basedOn w:val="Normal"/><w:rPr><w:b/><w:color w:val="2E74B5"/><w:sz w:val="34"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Caption"><w:name w:val="Caption"/><w:basedOn w:val="Normal"/><w:pPr><w:jc w:val="center"/><w:spacing w:after="160"/></w:pPr><w:rPr><w:i/><w:color w:val="526779"/><w:sz w:val="18"/></w:rPr></w:style></w:styles>'''
numbering = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:numbering xmlns:w="{W}"><w:abstractNum w:abstractNumId="0"><w:multiLevelType w:val="singleLevel"/><w:lvl w:ilvl="0"><w:numFmt w:val="bullet"/><w:lvlText w:val="•"/><w:lvlJc w:val="left"/><w:pPr><w:tabs><w:tab w:val="num" w:pos="720"/></w:tabs><w:ind w:left="720" w:hanging="360"/></w:pPr></w:lvl></w:abstractNum><w:abstractNum w:abstractNumId="1"><w:multiLevelType w:val="singleLevel"/><w:lvl w:ilvl="0"><w:start w:val="1"/><w:numFmt w:val="decimal"/><w:lvlText w:val="%1."/><w:lvlJc w:val="left"/><w:pPr><w:tabs><w:tab w:val="num" w:pos="720"/></w:tabs><w:ind w:left="720" w:hanging="360"/></w:pPr></w:lvl></w:abstractNum><w:num w:numId="1"><w:abstractNumId w:val="0"/></w:num><w:num w:numId="2"><w:abstractNumId w:val="1"/></w:num></w:numbering>'''
header = f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:hdr xmlns:w="{W}"><w:p><w:pPr><w:jc w:val="right"/><w:pBdr><w:bottom w:val="single" w:sz="4" w:color="D8E1E8"/></w:pBdr></w:pPr>{run("PHARMACARE · PHÂN TÍCH & THIẾT KẾ HTTT", color="526779", size=18)}</w:p></w:hdr>'
footer = f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:ftr xmlns:w="{W}"><w:p><w:pPr><w:jc w:val="right"/></w:pPr>{run("Trang ", color="526779", size=18)}<w:r><w:fldChar w:fldCharType="begin"/></w:r><w:r><w:instrText> PAGE </w:instrText></w:r><w:r><w:fldChar w:fldCharType="end"/></w:r></w:p></w:ftr>'
rels = [
    ('rId1','http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles','styles.xml'),
    ('rId2','http://schemas.openxmlformats.org/officeDocument/2006/relationships/numbering','numbering.xml'),
    ('rId3','http://schemas.openxmlformats.org/officeDocument/2006/relationships/header','header1.xml'),
    ('rId4','http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer','footer1.xml'),
] + [(f'rId{rid}','http://schemas.openxmlformats.org/officeDocument/2006/relationships/image',f'media/{name}') for name,rid in image_ids.items()]
docrels = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">' + ''.join(f'<Relationship Id="{i}" Type="{t}" Target="{x}"/>' for i,t,x in rels) + '</Relationships>'
rootrels = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/></Relationships>'
types = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Default Extension="svg" ContentType="image/svg+xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/><Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/><Override PartName="/word/numbering.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml"/><Override PartName="/word/header1.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml"/><Override PartName="/word/footer1.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml"/><Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/></Types>'''
now = datetime.now(timezone.utc).isoformat().replace('+00:00','Z')
core = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?><cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><dc:title>PharmaCare - Hồ sơ Phân tích và Thiết kế HTTT</dc:title><dc:subject>Rubric Phân tích &amp; Thiết kế Hệ thống Thông tin</dc:subject><dc:creator>Nhóm PharmaCare</dc:creator><dcterms:created xsi:type="dcterms:W3CDTF">{now}</dcterms:created></cp:coreProperties>'''

parts = {"[Content_Types].xml":types,"_rels/.rels":rootrels,"word/document.xml":document,"word/styles.xml":styles,"word/numbering.xml":numbering,"word/header1.xml":header,"word/footer1.xml":footer,"word/_rels/document.xml.rels":docrels,"docProps/core.xml":core}
with ZipFile(OUT, "w", ZIP_DEFLATED) as z:
    for name, data in parts.items(): z.writestr(name, data.encode("utf-8"))
    for name in image_ids: z.write(BASE/name, f"word/media/{name}")
print(OUT)
