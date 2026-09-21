# BatchCraft 1.4.1 release status — 2026-09-21

## Security and update reliability

- Installed builds select the standard Setup EXE; portable builds select the ZIP package.
- A valid GitHub SHA-256 digest is mandatory and verified after download.
- Partial or checksum-failed downloads are deleted.
- Portable updates create a backup and restore it if replacement fails.
- Old update folders and interrupted `.partial` files are cleaned automatically.
- NuGet audit is enabled for all dependency levels.
- Automated tests cover digest enforcement, installed/portable selection, Thai and spaced filenames, image-to-PDF, PDF-to-PNG, PDF merge page counts, Split PDF all pages, Extract page ranges, Rotate PDF, and page range parsing.

## Changes in v1.4.1

- **Modern Vector Path Icons**: Replaced Unicode character glyphs with 100% resolution-independent WPF Vector Geometries across all tab items, ensuring crisp rendering, exact vertical baseline alignment, and theme responsiveness.
- **Split and Rotate PDF Layout Redesign**: Redesigned Split PDF and Rotate PDF pages with compact modern drop zones, clean card grouping, and scroll view support so controls and action buttons never cut off on any screen scale.
- **Expanded Window Geometry**: Adjusted application window dimensions (1100x760) for a more spacious, elegant desktop layout.
- **Split PDF Feature**: Full Split PDF capability allowing users to split all pages into individual files or extract custom page ranges (e.g. `1-3, 5, 8-10`).
- **Rotate PDF Feature**: PDF page rotation (90° clockwise, 180°, 270° counter-clockwise) with scope selection (All pages, Odd pages, Even pages, Custom range).
- **Multi-page Preview Navigation**: Page navigation controls (`◀` `[ Page X / Y ]` `▶`) across PDF preview panels to inspect any page in the document.
- **Repository Migration**: Migrated update feed and download target to `thenikonboy/BatchCraft-Downloads`.

## Verification

- Automated test suite: passed (100% pass rate).
- Release build: 0 warnings, 0 errors.
- Self-contained Windows x64 publish: passed.
- Installer retains the existing stable AppId for upgrade-in-place compatibility.

