# ZaidForge vNext

ZaidForge is a proposed vNext identity for a more workflow-focused fork of File Converter.
The name keeps the conversion idea grounded: files go in, useful outputs are forged out,
and the fast Windows Explorer context-menu flow remains the heart of the app.

## First Principle

Keep the right-click conversion workflow simple, but make every preset smart enough for
real batches: organized output folders, source-aware names, reliable previews, and fewer
manual cleanup steps after conversion.

## Seed Feature: Smart Output Templates

This branch starts by extending output filename templates with tokens that help users
organize conversions automatically:

- `(preset)` and `(presetname)` insert the selected preset name.
- `(presetpath)` inserts the preset folder path, useful for grouping outputs by workflow.
- `(sc:yyyy-MM-dd)` inserts the source file creation date.
- `(sm:yyyy-MM-dd)` inserts the source file modified date.
- `(n:i:D3)` and `(n:c:D3)` add formatted page or frame counters.

Example:

```text
(p:documents)ZaidForge\(presetpath)\(sm:yyyy-MM)\(f) - (preset)
```

That can turn a loose folder of media into a date-sorted, preset-sorted output archive
without asking the user to rename files afterward.

## Next Moves

- Add a conversion queue history with retry and "open output folder" actions.
- Add a preset pack format for importing and sharing workflow bundles.
- Add a dependency health screen for FFmpeg, ImageMagick, Ghostscript, and Office support.
- Add optional watch folders for automatic conversions.
- Add a modern installer and CI path that can produce signed preview builds.
- Add focused tests around preset serialization, template expansion, and output path safety.
