# ZFileConverter Roadmap

ZFileConverter is the maintained fork identity for the File Converter revival.
The goal is not to overcomplicate the app. The goal is to keep the right-click workflow simple
while making the project dependable again.

## Principles

- Keep Windows Explorer conversion as the primary workflow.
- Prefer presets and repair tools over complicated editors.
- Make failures explain themselves through Health and Diagnostics.
- Keep public builds reproducible and unsigned-by-default unless release signing is configured.
- Preserve credit and GPL-3.0 continuity from the original project.

## Implemented In This Revival Branch

- Smart output template tokens:
  - `(preset)` and `(presetname)`
  - `(presetpath)`
  - `(sc:yyyy-MM-dd)` and `(sm:yyyy-MM-dd)`
  - `(n:i:D3)` and `(n:c:D3)`
- AVIF surfaced in the preset output picker.
- Explorer shell-extension repair command and Settings > Health repair launcher.
- Dependency health checks for FFmpeg, ImageMagick, Ghostscript, Office, settings, and Explorer registration.
- Queue actions to open completed outputs and retry failed conversions.
- Diagnostics actions to copy logs and open the log folder.
- Safer cleanup and failure handling across FFmpeg, Office, CDA, GIF, ICO, ImageMagick, settings, and shell extension paths.
- Windows CI with artifact upload.
- Build guide, release checklist, and PR template.

## Next Practical Moves

- Add lightweight unit tests around template expansion and settings serialization.
- Add a small sample-files smoke test pack for maintainers.
- Add release signing documentation once signing credentials are available.
- Replace or refresh visual branding assets when a dedicated ZFileConverter icon is ready.
- Add an issue triage label set and first-maintainer milestones.
