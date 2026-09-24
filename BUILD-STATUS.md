# BatchCraft 1.4.2 release status — 2026-09-24

## Security and update reliability

- Installed builds select the standard Setup EXE; portable builds select the ZIP package.
- A valid GitHub SHA-256 digest is mandatory and verified after download.
- Partial or checksum-failed downloads are deleted.
- Portable updates create a backup and restore it if replacement fails.
- Old update folders and interrupted `.partial` files are cleaned automatically.
- NuGet audit is enabled for all dependency levels.
- Automated tests cover digest enforcement, installed/portable selection, Thai and spaced filenames, image-to-PDF, PDF-to-PNG, PDF merge page counts, Split PDF all pages, Extract page ranges, Rotate PDF, page range parsing, multiple PDFs to JPG batch conversion, watermark with Thai text, and duplicate filename auto-rename.

## Changes in v1.4.2

- **Watermark PDF (ระบบใส่ลายน้ำเอกสาร PDF)**: Complete watermark stamping with custom text, presets ("สำเนาถูกต้อง", "DRAFT", etc.), font size, opacity (5%-100%), angle (45°, 0°, 90°), color (Gray, Red, Blue), page scope, and visual preview navigation.
- **Square Drop Zones (ช่องลากวางทรงสี่เหลี่ยม)**: Upgraded all tools (PDF to Images, Images to PDF, Merge PDF, Split PDF, Rotate PDF, Watermark PDF) to prominent, modern square drop zones with centered icons, descriptions, and file browsing buttons.
- **Multiple PDFs to JPG/PNG Batch Conversion (แปลง PDF หลายไฟล์เป็น JPG)**: Drag & drop multiple PDF documents at once in the PDF to Images tab, manage the file list, preview pages of each document, and batch export with aggregated progress.
- **Duplicate Filename Warning & Auto-Rename (ระบบแจ้งเตือนไฟล์ชื่อซ้ำกัน)**: Interactive prompt providing 3 safe choices whenever destination files collide: Overwrite (ทับไฟล์เดิม), Auto-rename with `(1)` suffix (เปลี่ยนชื่อใหม่อัตโนมัติ), or Cancel (ยกเลิก).
- **Responsive Layout & Theme Support**: Retains full window responsiveness, bilingual Thai/English localization, and Dark/Light themes across all new components.

## Verification

- Automated test suite: passed (16/16 tests, 100% pass rate).
- Release build: 0 warnings, 0 errors.
- Self-contained Windows x64 publish: passed.
- Inno Setup installer maintains stable AppId for upgrade-in-place compatibility.
