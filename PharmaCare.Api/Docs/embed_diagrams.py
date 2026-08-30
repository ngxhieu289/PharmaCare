from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from tempfile import TemporaryDirectory
import shutil

base = Path(__file__).parent
source = base / "PharmaCare_Ho_so_Phan_tich_Thiet_ke_HTTT.docx"
output = base / "PharmaCare_Ho_so_Phan_tich_Thiet_ke_HTTT.final.docx"

items = [
    ("Hình 1 —", "system-context.svg", 1, 5669280, 2929100),
    ("Hình 2 —", "container-module.svg", 2, 5669280, 3590300),
    ("Hình 3 —", "order-sequence.svg", 3, 5669280, 3873650),
    ("Hình 4 —", "jwt-flow.svg", 4, 5669280, 2456700),
]

def drawing(rid: int, name: str, cx: int, cy: int) -> str:
    return f'''<w:p><w:pPr><w:jc w:val="center"/><w:spacing w:before="120" w:after="60"/></w:pPr><w:r><w:drawing>
<wp:inline xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" distT="0" distB="0" distL="0" distR="0">
<wp:extent cx="{cx}" cy="{cy}"/><wp:effectExtent l="0" t="0" r="0" b="0"/>
<wp:docPr id="{rid}" name="{name}" descr="Sơ đồ kiến trúc PharmaCare"/><wp:cNvGraphicFramePr/>
<a:graphic xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture">
<pic:pic xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:nvPicPr><pic:cNvPr id="{rid}" name="{name}"/><pic:cNvPicPr/></pic:nvPicPr>
<pic:blipFill><a:blip r:embed="rId{rid + 2}" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill>
<pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="{cx}" cy="{cy}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr></pic:pic>
</a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>'''

with TemporaryDirectory() as tmp_name:
    tmp = Path(tmp_name)
    with ZipFile(source) as archive:
        archive.extractall(tmp)

    doc_path = tmp / "word" / "document.xml"
    doc = doc_path.read_text(encoding="utf-8")
    for caption, filename, number, cx, cy in items:
        marker = doc.find(caption)
        if marker < 0:
            raise RuntimeError(f"Không tìm thấy caption: {caption}")
        paragraph_start = doc.rfind("<w:p", 0, marker)
        doc = doc[:paragraph_start] + drawing(number, filename, cx, cy) + doc[paragraph_start:]
    doc_path.write_text(doc, encoding="utf-8")

    media = tmp / "word" / "media"
    media.mkdir(exist_ok=True)
    for _, filename, _, _, _ in items:
        shutil.copy2(base / filename, media / filename)

    rels_path = tmp / "word" / "_rels" / "document.xml.rels"
    rels = rels_path.read_text(encoding="utf-8")
    additions = "".join(
        f'<Relationship Id="rId{number + 2}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/{filename}"/>'
        for _, filename, number, _, _ in items
    )
    rels = rels.replace("</Relationships>", additions + "</Relationships>")
    rels_path.write_text(rels, encoding="utf-8")

    types_path = tmp / "[Content_Types].xml"
    types = types_path.read_text(encoding="utf-8")
    types = types.replace("</Types>", '<Default Extension="svg" ContentType="image/svg+xml"/></Types>')
    types_path.write_text(types, encoding="utf-8")

    with ZipFile(output, "w", ZIP_DEFLATED) as archive:
        for path in sorted(tmp.rglob("*")):
            if path.is_file():
                archive.write(path, path.relative_to(tmp))

source.unlink()
output.rename(source)
