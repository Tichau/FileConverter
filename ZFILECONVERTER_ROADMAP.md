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

## Later Design Overhaul

This is intentionally a later phase, not part of the current stabilization pass.
The current revival should keep the familiar File Converter workflow intact: right-click a file,
choose a preset, get a clear result. The later design pass can make the app feel more polished,
modern, beautiful, and user-friendly without turning it into a complicated editor.

- Revisit the Settings window layout, typography, spacing, iconography, empty states, and health diagnostics.
- Design a proper ZFileConverter app icon, installer visual identity, and GitHub release artwork.
- Make presets easier to browse, search, edit, duplicate, import, and understand.
- Improve queue/progress visibility while keeping the Explorer-first workflow fast.
- Explore a more refined onboarding and troubleshooting flow for missing FFmpeg, ImageMagick, Ghostscript, and Office dependencies.
- When Mythos is released and available, consider using it as a dedicated frontend/design exploration partner for this UI pass.

## Later Product And Monetization Notes

ZFileConverter should remain useful as a simple, trustworthy local Windows utility first.
Any commercial path should protect that trust and respect GPL-3.0 continuity.

- Most realistic near-term path: free open-source app with optional donations, sponsorships, and paid support.
- Stronger business path: paid signed builds, managed enterprise packaging, deployment help, support SLAs, and custom presets/workflows.
- Possible product path: a hosted/API conversion service, but that would be a separate product with real infrastructure, privacy, abuse, and cost concerns.
- Avoid making core local conversion annoying, ad-heavy, account-gated, or artificially limited.
- Before charging for binaries or services, confirm GPL source-distribution obligations and third-party middleware licenses.
