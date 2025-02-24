# Change Log

## Version 2.1

- New: Option to use NVidia hardware acceleration for mp4 video (thanks to tacheometry).
- New: Add Gif to Image conversion support (issue #433, #115) (thanks to RTnhN)
- New: Persian translation (thanks to MrHero118 and Mehrdad32).
- New: Serbian translation (thanks to crnobog69).
- New: Japanese translation (thanks to oogamiyuta).
- New: Czech translation (thanks to AidyTheWeird).
- New: Korean translation (thanks to Alanimdeo).
- New: Vietnamese translation (thanks to vrykolakas166).
- New: Russian translation (thanks to iliamak).
- Fixes: Issue where video where rotated when using the To Mp4 scale 25% and 75% presets.
- Fixes: Hebrew translation issues (thanks to AshiVered).
- Fixes: Traditional Chinese translation issues (thanks to NeKoOuO and PeterDaveHello).
- Tech: Update ffmpeg to v7.1 and ImageMagick to v14.4 (issue #527).
- Tech: Update project installer to Wix 5.
- Tech: Migrate packages.config to PackageReferences.

## Version 2.0.2

- New: Hebrew translation (thanks to AshiVered).
- Fixes: Issue where installer was not working due to registry key not updated correctly during install (issue #382).
- Fixes: Update chinese translations (thanks to jie65535).
- Fixes: Issue where tempo/pitch conversion settings were not considered as default settings resulting is some issues during backward compatibility check.

## Version 2.0.1

- New: Tempo and pitch conversion presets in default settings (github issue #18).
- New: Russian translation (thanks to dragomano).
- Fixes: Issue where File Converter was not appearing in Windows explorer after a successful install (issue #389).
- Fixes: Issue where newly supported input types (ts, heic, ...) were not used in presets (github issue #388).
- Fixes: Issue where standard output was not activated when started from command line.

## Version 2.0

- New: Possibility to create custom command line preset for types converted with FFMPEG (video and audio) (github issue #19 #18 #41 #61 #73 #128 #140 #153 #177 #182 #225 #255 #310 #314 #316 #325).
- New: Possibility to create preset folders in File Converter context menu.
- New: Ability to drag and drop to move conversion presets.
- New: Possibility to export and import conversion presets to share it with other users.
- New: More presets for scale and rotation in File Converter default configuration.
- New: Display conversion progress on the Windows taskbar item.
- New: Display estimated remaining time for each jobs.
- New: Support new image input formats: arw, cr2, dds, dng, jfif, nef, raf, tif and heic (github issue #29 #96 #113 #130).
- New: Support new video input formats: 3gpp, mpg, rm, ts (github issue #23 #59 #101 #318).
- New: Support new audio input formats: m4b, opus (github issue #111 #351).
- New: Icons on context menu elements and conversion preset list actions.
- New: Remove the need to ask administrator privileges to edit File Converter settings (github issue #4 #30 #32).
- New: Highlight in red elements that contains errors in the preset list.
- New: Messages logged from main thread are now displayed in the console standard output.
- New: Add the bookmarks in pdf when converting from word file (thanks to wangweirui).
- New: Possibility to use conversion date in output file path template (github issue #56).
- New: Option to copy files in clipboard after conversion (thanks to hsayed21).
- New: Brazilian Portuguese translation (thanks to Marhc).
- New: Spanish translation (thanks to Chachak).
- New: Italian translation (thanks to Davide).
- New: German translation (thanks to nikotschierske).
- New: Simplified Chinese translation (thanks to Snoopy1866).
- New: Turkish translation (thanks to MayaC0re).
- New: Hindi translation (thanks to vishveshjain).
- New: Arabic translation (thanks to Mahmoud0Sultan).
- New: Traditional Chinese translation (thanks to Sedimentary-Rock).
- New: Greek translation (thanks to CrisBalGreece).
- Change: Portuguese translation improvement (thanks to hugok79).
- Change: Rework extension so it uses the settings file on disk instead of registry settings (github issue #22 #32 #176 #340 #343).
- Change: Some UX improvements.
- Change: Replace Pledgie donation button by Paypal donation button since Pledgie does not exist anymore.
- Change: Correction of spell mistakes in the french translation (thanks to Sylvain Pollet-Villard).
- Change: Change output files timestamp to match original file (github issue #33) (thanks to Diego López Bugna).
- Fixes: Issue where there was a maximum number of files to convert at the same time depending on the length of file paths (github issue #86).
- Fixes: Issue where File Converter version upgrade download was not working due to an issue with https encryption.
- Fixes: Issue where output video was not working correclty on some video players like Quick time (github issue #34) (thanks to Diego López Bugna).
- Fixes: Issue where icons and images were blurry on high dpi device.
- Fixes: Issue where file with 'Error' in there name were generating false negative result (github issue #247).
- Tech: Complete rework of the project architecture to be able to improve it in a long term perspective. The project is now using the MVVM Community Toolkit and is following more closely this design pattern.
- Tech: Update ffmpeg to v6.1.1 and ImageMagick to v13.5.
- Tech: Update SharpShell to v2.7.2.
- Tech: Change ghostscript version from 9.21 to 10.02.1.
- Tech: Update project to Visual Studio 2022 and installer Wix 4.
- Tech: Update project to .NET framework 4.8.
- Tech: Check for .NET framework version in installer and prompt the user if the required version is not installed.
- Tech: Remove sharpshell tools from installer dependencies. The shell extension is now registered by File Converter application (github issue #39 #46).
- Tech: Remove Windows Vista support and 32bit platform support. Use File Converter 1.2.3 if you need these.

-------------------------------------------------------------------------------------------------------------

## Version 1.2.3

- Fixes: Issue where the scale was not working for image conversions depending on the application current language.
- Tech: Include office library dependencies in package config (now you don't need to install office to build the project).
- Tech: Upgrade Ghostscript to version 9.21.
- Tech: Update ImageMagick to version 7.0.5.

## Version 1.2.2

- New: Portuguese translation (thanks to Khidreal).
- Fixes: Issue where scale was corrupted when switching application language (github issue #5).
- Fixes: Issue where file metadata were not copied when converting to aac format (github issue #15).
- Tech: Update to ffmpeg 3.2.2 version.
- Tech: Improve security using https instead of http for upgrade system and links (thanks to TheAresjej).

## Version 1.2.1

- Fixes: Issue where audio file tags were not copied in a format readable by Windows.
- Fixes: Issue where the default language was set to french (github issue #6).
- Fixes: Issue where the size of a pdf generated from images was not consistent.
- Change: Small improvements of File Converter application and installer interface thanks to TheAresjej (github issues #7 and #8).

## Version 1.2

- New: Possibility to convert Word documents (docx, odt and doc) to pdf and images (this feature is only available if you have Microsoft Word installed).
- New: Possibility to convert Excel documents (xlsx, ods and xls) to pdf and images (this feature is only available if you have Microsoft Excel installed).
- New: Possibility to convert PowerPoint documents (pptx, odp and ppt) to pdf and images (this feature is only available if you have Microsoft PowerPoint installed).
- New: Possibility to convert images and documents to webp format.
- New: Support new image input format: webp.
- New: Add the possibility to cancel image, CDA extraction and gif conversions.
- Fixes: Issue where multiple conversion jobs were created for the same file resulting in an error message.
- Tech: Improve application performances on startup.
- Tech: Improve diagnostics.

## Version 1.1

- New: Localization system. File Converter can now be translated in multiple languages. The default language is the same than your OS language (if available).
- New: Add the possibility to choose the application language in the application settings.
- New: Add french localization.
- New: Possibility to convert a Pdf file into image files (one image per page).
- New: Possibility to convert an image into a Pdf file.
- New: Possibility to encode videos using theora video codec and Ogg vorbis audio codec in a ogv container.
- New: Possibility to change the number of channels of an audio file (stereo -> mono, 5.1 -> stereo, ...).
- New: Improve the application upgrade UI. If you quit the application during the upgrade downloading, you will now see the download progress.
- New: File converter is now available for Windows 32 bits systems (download the x86 installer).
- New: Add an application option to choose the maximum number of simultaneous conversions.
- Change: Improve the application help start page (when you launch File Converter directly from the executable) with an animated picture.
- Change: Improve the output type dropdown visualization (splitting it by categories).
- Fixes: Issue where it was impossible to rotate an image for the output types: jpg and png.

## Version 1.0

- New: File Converter is now certified by Microsoft Authenticode.
- New: Add an about section that contains information on the software development (change log, documentation link, report issue link).
- New: Split conversion preset settings and application settings to improve ergonomic design.
- New: Add help shortcuts in the conversion preset edition interface that links directly to the corresponding documentation page.
- New: Add shortcuts in start menu.
- New: Create a donation campaign on Pledgie and linked it in the about section.
- Fixes: Issue with Avast antivirus where File Converter and Windows explorer was frozen when the user tried to use File Converter.
- Fixes: Issue where it was impossible to convert files from a network path.

## Version 0.7

- New: Possibility to encode videos using VP9 video codec and Ogg vorbis audio codec in a webm container.
- New: Possibility to convert videos and images to gif.
- New: Possibility to convert gif files to videos.
- New: Possibility to remove audio from video files (mkv, mp4, avi and webm output file formats).
- New: Add feedback to indicate that the application will automatically terminate (after conversions) (github issue #1).
- New: Support new video input formats: ogv and mpeg.
- New: Support new audio input format: oga.
- New: Possibility to cancel some conversion jobs (gif/cda and image conversion jobs are not cancelable for now).
- Fixes: Issue where video can't be compressed using H.264 codec (mp4 or mkv presets) when its size (width or height) is not divisible by 2 (append a lot when the user tries to scale a video) (github issue #2).
- Fixes: Issue where "Clamp to power of 2 size" option didn't work well with images that already have a power of 2 size.
- Fixes: Issue where conversion progress was not updated correctly (for converters based on ffmpeg).
- Fixes: Issue where converting a video into the ogg format did not extract audio in an ogg file correctly.
- Fixes: Issue where simultaneous read on the same CD drive was performed when files from different conversions came from the same drive (other than cda extraction preset).
- Fixes: Issue where the path generator was unable to create a valid path if the input file was at the root of a drive.
- Tech: Annotate the conversion presets to know if there are default preset or user preset in order to improve the upgrade process.

## Version 0.6

- New: Possibility to encode videos using H.264 video codec and AAC audio codec in a mp4 container (more portable than mkv).
- New: Support new image input file formats: psd, tga, svg and exr.
- New: Support new video input file format: m4v.
- New: Possibility to rotate images and videos.
- New: Remove restriction on image size to convert to ico file format (it is now possible to convert all images to ico).
- New: Add "Clamp to power of 2 size" option in image conversion.
- New: Support input images encoded in 16 bits or 32 bits per color channel.
- Fixes: Issue where the installer detects the uses of the extension dll in explorer, ask the user to restart it and fail to do it.
- Fixes: Issue where it is impossible to delete a conversion preset defined in default settings.
- Fixes: Issue where the Windows context menu does not contain user preset after an application upgrade.
- Fixes: Issue where some registry keys remain after file converter uninstall.
- Fixes: Issue where aac bitrate was not saved.
- Fixes: Issue where settings serialization version was not updated.
- Change: Allow the user to scale images until 1600% size (useful to scale pixel art images).

## Version 0.5

- New: Software update system. The application now checks if a new version of File Converter is available.
- New: Possibility to scale images and videos.
- New: Possibility to encode videos (avi output file format) using Xvid video codec and Mp3 audio codec.
- New: Support new video input file formats: 3gp, webm and wmv.
- New: Add a help window to explain how file converter works when you launch it without using the context menu.
- Fixes: Problem to convert images when accentuated characters were present in their path.
- Tech: Update ffmpeg version (aac encoding is not anymore experimental).

## Version 0.4

- New: Possibility to encode videos (mkv output file format) using H.264 video codec and AAC audio codec.
- New: Possibility to extract Audio CD content.
- New: Possibility to encode audio in aac format.
- New: Possibility to encode images (png, jpg and ico formats).
- New: Support new audio input file format: aac
- New: Support new video input file formats: bik, flv, mov, mkv
- New: Support new image input file formats: bmp, tiff, png, jpg and ico.
- New: Multi-thread conversion (file converter will now start multiple conversions at the time depending on your number of cores).
- New: Copy the currently selected preset when clicking on the add preset button.
- New: Add "My Documents", "My Music", "My Videos" and "My Pictures" folder to output file name generator.
- New: Possibility to choose if the application quit after succeeded conversions.
- Fixes: Reordering presets does not update the registry.
- Fixes: Merge default settings with user settings to prevent errors when upgrading the application.
- Tech: Handle incorrect user settings case.
- Tech: Improve diagnostics system (compatibility with logs from multiple threads, dump files in AppData folder and error messages).
- Tech: Update ffmpeg version.

## Version 0.3

- New: Possibility to extract audio from videos.
- New: Support new input file formats: aiff, m4a, avi, mp4.
- New: Quality settings for Mp3 (Encoding mode vbr/cbr and bitrate).
- New: Quality settings for Ogg (Bitrate).
- New: Quality settings for Wav (bits per sample: 8, 16, 24 or 32).
- New: Add output file path template system.
- New: Add button to move up or down a preset.
- New: Add input files post conversion action (No action, move in archive folder or delete).
- New: Add categories on input file extensions.
- Fixes: Lose focus of main window don't terminate the application.
- Fixes: The application icon now have a correct resolution at any size.
- Tech: Add error codes on error messages.

## Version 0.2

- New: Support new input file format: ape.
- New: Add notion of conversion preset (in order to customize the conversion possibilities).
- New: Add settings window to edit conversion presets.
- New: Add application icon.
- New: Customize application installer.
- New: Add diagnostic window to read the application logs.

## Version 0.1

- New: Add decode support for file formats Mp3, Ogg, Wav, Flac, Wma
- New: Add encode support for file formats Mp3, Ogg, Wav, Flac
- New: UI to visualize the conversion progress
