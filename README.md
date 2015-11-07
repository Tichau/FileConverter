# File Converter

## Description
**File Converter** is a very simple tool which allows you to convert one or several file(s) from one format to another using the context menu in windows explorer.
This program uses [ffmpeg](https://www.ffmpeg.org/) as file conversion software.

You can convert files from formats **Aiff, Ape, Avi, Flac, M4a, Mp3, Mp4, Ogg, Wav, Wma** to formats **Mp3, Ogg, Wav, Flac**.

*Note: If your source file is a video, the conversion to an audio format will extract the audio part of the video.*

***Remove any previous installation of the application before installing the newer version.***

This application is under GPL version 3 licence. 
For more informations check the LICENSE file in your installation folder or the [gnu website](http://www.gnu.org/licenses/gpl.html).

## Input file post conversion actions

This actions allow you to chose what you want to do with your input files if the conversion succeed.

Action     					| Description
----------------------------|------------------------------------------------------------------------------------------------
**None**     				| Don't modify the input file. If the output file name conflict with the input, it will be renamed.
**Move in archive folder**  | Move the input file in a folder named *Conversion Archives* if the conversion is a success.
**Delete**     				| Delete the input file if the conversion is a success (be careful no warning will be prompted).

## Output file path template

This template allow you to define how you want to generate the output file path (depending on the input file path).
You have access to the following informations:

Pattern	| Description					| Example (with input path: *C:\Music\Artist\Album\Song.wav*)
--------|-------------------------------|-----------------------------------------------------------------
(p)   	| Input file path				| C:\Music\Artist\Album\
(f)  	| Input file name				| Song
(o)		| Output file format			| mp3
(i)		| Input file format				| wav
(d0)	| Input parent folder name		| Album
(d1)	| Input sub parent folder name	| Artist

*Tips: You can use uppercase to retrieve caps lock informations. Example: (f) -> Default / (F) -> DEFAULT *

### Examples

Simple default template (just change the extension): 

	Input: C:\Music\Artist\Album\Song.wav
	Template: (p)(f)
	Generated output: C:\Music\Artist\Album\Song.mp3

File name customization:

	Input: C:\Music\Artist\Album\Song.wav
	Template: (p)(O)_(f) (from (i))
	Generated output: C:\Music\Artist\Album\MP3_Song (from wav).mp3
		
File path customization:
		
	Input: C:\Music\Artist\Album\Song.wav
	Template: D:\(o)\(d1)\(f)
	Generated output: D:\mp3\Artist\Song.mp3

## Change Log
v0.3
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

v0.2
- New: Support new input file format: ape.
- New: Add notion of conversion preset (in order to customize the conversion possibilities).
- New: Add settings window to edit conversion presets.
- New: Add application icon.
- New: Customize application installer.
- New: Add diagnostic window to read the application logs.

v0.1
- New: Add decode support for file formats Mp3, Ogg, Wav, Flac, Wma 
- New: Add encode support for file formats Mp3, Ogg, Wav, Flac
- New: UI to visualize the conversion progress