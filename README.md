# File Converter

## Description
**File Converter** is a very simple tool which allows you to convert one or several file(s) from one format to another using the context menu in windows explorer.

This application is under GPL version 3 licence. 
For more informations check the LICENSE file in your installation folder or the [gnu website](http://www.gnu.org/licenses/gpl.html).

You can download it here: [www.file-converter.org](http://file-converter.org).

## Supported file formats

Category 	| Supported output formats	| Compatible input formats
------------|---------------------------|----------------------------------------------------------------------------
 Audio		| flac, aac, ogg, mp3, wav	| aiff, ape, avi, bik, cda, flac, flv, m4a, mkv, mov, mp3, mp4, ogg, wav, wma
 Video		| mkv						| avi, bik, flv, mkv, mov
 Image		| png, jpg, ico				| bmp, ico, jpg, jpeg, png, tiff

*Note: If your source file is a video, the conversion to an audio format will extract the audio part of the video.*

*Note 2: The mkv output file format encodes video using H.264 encoder for video and AAC encoder for audio.*

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
(p:d) 	| My documents folder path		| C:\Users\UserName\Documents\
(p:m)	| My music folder path			| C:\Users\UserName\Music\
(p:v)	| My videos folder path			| C:\Users\UserName\Videos\
(p:p)	| My pictures path				| C:\Users\UserName\Pictures\
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

CDA extraction:
		
	Input: E:\Track01.cda
	Template: (p:m)CDA Extraction\(f)
	Generated output: C:\Users\UserName\Music\CDA Extraction\Track01.mp3
	
## Thanks

File converter uses the following middlewares:

**ffmpeg** as file conversion software.
Thanks to ffmpeg devs for this awesome open source file convesion tool. [Web site link](https://www.ffmpeg.org/)

**SharpShell** to easily create windows context menu extensions.
Thanks to Dave Kerr for his work on SharpShell. [CodePlex link](https://sharpshell.codeplex.com/)

**Ripper** and **yeti.mmedia** for CD Audio extraction.
Thanks to Idael Cardoso for his work on CD Audio ripper. [Code project link](http://www.codeproject.com/Articles/5458/C-Sharp-Ripper)

## Change Log
### v0.4
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
- New: Possibily to chose if the application quit after succeeded conversions.
- Fixed: Reordering presets does not update the registry. 
- Tech: Handle incorrect user settings case.
- Tech: Improve diagnostics system (compatibility with logs from multiple threads, dump files in AppData folder and error messages).
- Tech: Update ffmpeg version.

### v0.3
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

### v0.2
- New: Support new input file format: ape.
- New: Add notion of conversion preset (in order to customize the conversion possibilities).
- New: Add settings window to edit conversion presets.
- New: Add application icon.
- New: Customize application installer.
- New: Add diagnostic window to read the application logs.

### v0.1
- New: Add decode support for file formats Mp3, Ogg, Wav, Flac, Wma 
- New: Add encode support for file formats Mp3, Ogg, Wav, Flac
- New: UI to visualize the conversion progress