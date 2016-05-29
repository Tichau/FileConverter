# Change Log

## Version 1.0
- New: File Converter is now certified by Microsoft Authenticode. 
- New: Add an about section that contains information on the software development (change log, documentation link, report issue link).
- New: Split conversion preset settings and application settings to improve ergonomy.
- New: Add help shortcut in the conversion preset edition interface that links directly to the corresponding documentation page.
- New: Add shortcuts in start menu.
- New: Create a donation campaign on Pledgie and linked it in the about section.
- Fixed: Issue with Avast antivirus where File Converter and Windows explorer was frozen when the user tried to use File Converter.
- Fixed: Issue where it was impossible to convert files from a network path.

## Version 0.7
- New: Possibility to encode videos using VP9 video codec and Ogg vorbis audio codec in a webm container.
- New: Possibility to convert videos and images to gif.
- New: Possibility to convert gif files to videos.
- New: Possibility to remove audio from video files (mkv, mp4, avi and webm output file formats).
- New: Add feedback to indicate that the application will automatically terminate (after conversions) (github issue #1).
- New: Support new video input formats: ogv and mpeg.
- New: Support new audio input format: oga.
- New: Possibility to cancel some conversion jobs (gif/cda and image conversion jobs are not cancelable for now).
- Fixed: Issue where video can't be compressed using H.264 codec (mp4 or mkv presets) when its size (width or height) is not divisible by 2 (append a lot when the user tries to scale a video) (github issue #2).
- Fixed: Issue where "Clamp to power of 2 size" option didn't work well with images that already have a power of 2 size.
- Fixed: Issue where conversion progress was not updated correctly (for converters based on ffmpeg).
- Fixed: Issue where converting a video into the ogg format did not extract audio in an ogg file correctly.
- Fixed: Issue where simultaneous read on the same CD drive was performed when files from differents conversions come from the same drive (other than cda extraction preset).
- Fixed: Issue where the path generator was unable to create a valid path if the input file was at the root of a drive.
- Tech: Annotate the conversion presets to know if there are default preset or user preset in order to improve the upgrade process.

## Version 0.6
- New: Possibility to encode videos using H.264 video codec and AAC audio codec in a mp4 container (more portable than mkv).
- New: Support new image input file formats: psd, tga, svg and exr.
- New: Support new video input file format: m4v.
- New: Possibility to rotate images and videos.
- New: Remove restriction on image size to convert to ico file format (it is now possible to convert all images to ico).
- New: Add "Clamp to power of 2 size" option in image conversion.
- New: Support input images encoded in 16 bits or 32 bits per color channel.
- Fixed: Issue where the installer detects the utilisation of the extension dll in explorer, ask the user to restart it and fail to do it.
- Fixed: Issue where it is impossible to delete a conversion preset defined in default settings.
- Fixed: Issue where the Windows context menu does not contain user preset after an application upgrade.
- Fixed: Issue where some registry keys remain after file converter uninstallation.
- Fixed: Issue where aac bitrate was not saved.
- Fixed: Issue where settings serialization version was not updated.
- Change: Allow the user to scale images until 1600% size (useful to scale pixel art images).

## Version 0.5
- New: Software update system. The application now checks if a new version of File Converter is available.
- New: Possibility to scale images and videos. 
- New: Possibility to encode videos (avi output file format) using Xvid video codec and Mp3 audio codec.
- New: Support new video input file formats: 3gp, webm and wmv.
- New: Add a help window to explain how file converter works when you launch it without using the context menu.
- Fixed: Problem to convert images when accentuated characters were present in their path.
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
- Fixed: Reordering presets does not update the registry. 
- Fixed: Merge default settings with user settings to prevent errors when upgrading the application. 
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
- Fixed: Lose focus of main window don't terminate the application.
- Fixed: The application icon now have a correct resolution at any size.
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