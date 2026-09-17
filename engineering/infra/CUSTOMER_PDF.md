# Customer PDF generation

## Maintenance contract: reuse the existing format

For future content updates, reuse `engineering/scripts/build_customer_pdf.py` as
the canonical layout implementation. Do not recreate the PDF generator, extract
content from the exported PDF, or use the older scratch builder under `tmp/pdfs/`.
The Markdown sources, original screenshots, and canonical builder together are
the editable master. The PDF is the delivery artifact.

Updating content still requires exporting the PDF again so pagination, figure
numbers, bookmarks, and the table of contents can be recalculated. It does not
require designing or generating the document structure from scratch.

Keep the established visual format unless the user requests a redesign. Do not
shrink fonts or images just to preserve the previous page count.

## Where to make each type of change

| Change | Authoritative location |
|---|---|
| System design narrative and tables | `doc/Thiet_Ke_He_Thong_Quan_Ly_Ho_So_WPF_NET5.md` |
| Installation steps | `doc/Huong_dan_cai_dat_Server_Client.md` |
| User instructions and screenshot references | `doc/Huong_dan_su_dung_QuanLyHoSo.md` |
| Original screenshots | `doc/GUI/`; update the corresponding Markdown image reference |
| Appendix text and checklist | `appendix_story()` in the canonical builder |
| Shared typography and colors | `styles` definitions and color constants in the builder |
| Table appearance and column widths | `make_table()` |
| Screenshot viewports and orientation | `CROPS`, `OriginalImage`, and `image_flowables()` |
| Cover, part order, version, and date | `build()`, `part_page()`, and `CustomerDocTemplate.content_page()` |
| PDF diagrams | `diagram()`; these are manually drawn vector equivalents of the Markdown Mermaid blocks |

## Format baseline

- Preserve the four-part order: system design, installation, usage, operational appendix.
- Use A4 portrait for every page, including wide application screens, as explicitly
  requested by the user. Do not restore landscape pages or rotate pages. Margins
  are 17 mm left/right, 20 mm top, and 18 mm bottom. Wide screenshots fit within
  the 176 mm content width with their original aspect ratio preserved.
- Reuse the embedded Arial family and existing heading hierarchy: H1 19/23 pt,
  H2 14/18 pt, H3 11.2/15 pt (font size/leading). Body is 10.5/16 pt;
  table text is 9.2/13.4 pt and captions are 9/13 pt.
- Reuse navy `#12355B`, blue `#1769AA`, teal `#198F8C`, dark body text
  `#243447`, and light alternating table rows. Header cells use white bold text.
- Keep automatic figure numbering, linked contents, bookmarks, and page numbers.
  Keep headings with their associated tables, lists, or illustrations.
- Retain Vietnamese customer-facing prose and exact UI labels. Internal maintenance
  instructions remain in English.

## Workflow for the next content revision

1. Read this page and check the current Git state. Preserve existing user changes.
2. Edit only the relevant source Markdown or appendix content. Reuse existing
   heading levels, numbered steps, tables, and relative image links.
3. If replacing a screenshot, use an original PNG from `doc/GUI/`. Check both the
   filename-keyed `CROPS` entry and caption-specific conditions in `image_flowables()`.
   Changing a caption can change its viewport selection. Confirm the entire dialog,
   its title, and action buttons remain visible in the intended illustration.
4. If changing a Mermaid diagram, update its vector counterpart in `diagram()` too.
   The builder selects these diagrams by order; it does not render Mermaid itself.
5. If changing the release version or revision date, synchronize the source metadata,
   cover, footer, and output filename. These values are currently defined in more
   than one place; do not assume changing the filename updates the visible version.
6. Run the existing builder using the command below, then perform the verification
   checks in this runbook. Review all pages because content changes can shift later
   sections and table-of-contents entries.
7. Update the verification baseline and `engineering/SESSION_HANDOFF.md` with actual
   results. Keep the canonical builder and source changes together with the export.

## Inputs and output

Run `python engineering/scripts/build_customer_pdf.py` from the repository root.
The builder requires Python, ReportLab, Pillow, and the Windows Arial font files.
It writes `doc/QuanLyHoSo_TaiLieu_KhachHang_1.0.0.pdf`.

Sources are the three customer-facing Markdown documents under `doc/`:

- `Thiet_Ke_He_Thong_Quan_Ly_Ho_So_WPF_NET5.md`
- `Huong_dan_cai_dat_Server_Client.md`
- `Huong_dan_su_dung_QuanLyHoSo.md`

The operational appendix is defined in the builder. Screenshots are loaded directly
from `doc/GUI/`. The server overview uses the installation control-panel screenshot
instead of the older reset-password dialog referenced by the usage guide.

## Layout and image quality

- Embed the Arial family for Vietnamese text; body text is 10.5 pt with 16 pt leading.
- Table header paragraphs explicitly use white bold text on navy. Table-level text
  color alone does not override a Paragraph's own text color.
- Use dark 9.2 pt table body text and alternating light row fills.
- Keep original image pixels losslessly. `OriginalImage` embeds the complete PNG
  and clips its viewport in PDF coordinates, without resizing the source bitmap.
- All pages are A4 portrait. Wide application screens use the full content width;
  dialogs or task-specific regions are enlarged. `CROPS` uses original pixel coordinates;
  recheck these coordinates when replacing a screenshot.
- Preserve source step numbers, nested list indentation, and level-four headings.
- Rebuild the linked table of contents and PDF bookmarks with `multiBuild`.

## Verification after regeneration

1. Render every page with Poppler and visually inspect page layout and screenshots.
2. Inspect tables, enlarged dialog crops, and wide screenshots at readable resolution.
   Require every page to be A4 portrait (approximately 595.28 x 841.89 pt) with zero rotation.
3. Check heading placement, figure captions, page numbers, and table-of-contents links.
4. Compare embedded image RGB pixels against the original PNGs, and check text bounds,
   missing glyphs, and the fonts actually used in PDF text.

The latest 2026-09-17 refreshed PDF has 58 pages, all A4 portrait, 37 figure
placements, and 48 bookmarks. All 33 unique embedded images matched the original
PNG RGB pixels exactly. Text-bound and missing-glyph checks passed; all pages were
rendered with Poppler and visually reviewed. These results apply to this export,
not automatically to future regenerations. Application code was not changed.
