# Change Log

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