# File Converter

## Description
**File Converter** is a very simple tool which allows you to convert and compress one or several file(s) using the context menu of windows explorer.

You can download it here: [www.file-converter.org](https://file-converter.org/?from=readme.md).

You can find more information about what's in File converter and how to use it on the [wiki](https://github.com/Tichau/FileConverter/wiki).

## Donate

File Converter is a personal open source project started in 2014. I have put hundreds of hours adding, refining and tuning File Converter with the goal of making the conversion and compression of files an easy task for everyone.

You can help me by [contributing to the project](https://github.com/Tichau/FileConverter/wiki#contribute), by [making a donation](https://www.paypal.com/donate/?cmd=_donations&business=3BDWQTYTTA3D8&item_name=File+Converter+Donations&currency_code=EUR&Z3JncnB0=) or just by [saying thanks​](https://saythanks.io/to/Tichau) :).

##  Troubleshooting

If you encounter any problem with File Converter, you can:

* See the already known problems in the [troubleshooting section of the documentation](https://github.com/Tichau/FileConverter/wiki/Troubleshooting).
* Or report an issue on the [bug tracker](https://github.com/Tichau/FileConverter/issues).

When you report an issue, please join the following informations:

* Registry.xml
* Settings.user.xml
* The Diagnostics folder of the session that encountered the issue.
* A screenshot (if possible) and a description that shows/explain the issue.

You will find the xml files and diagnostics folder in `c:\Users\[UserName]\AppData\Local\FileConverter\`.

## Setup development environement

### Requirements

For File Converter and its explorer extension:
- Visual Studio 2017

For the installer:
- [Wix toolset build tool v3.11 and visual studio extension](http://wixtoolset.org/)
- [Windows SDK Signing Tools for Desktop Apps](https://developer.microsoft.com/fr-fr/windows/downloads/windows-10-sdk)

## Thanks

Thanks to all the contributors of File Converter project.

### Localization

Thanks to **Khidreal** for the Portuguese localization.
Thanks to **Marhc** for the Brazilian localization.
Thanks to **Chachak** for the Spanish localization.
Thanks to **Davide** for the Italian localization.

## Middlewares

File converter uses the following middlewares:

**ffmpeg** as file conversion software.
Thanks to ffmpeg devs for this awesome open source file conversion tool. [Web site link](https://ffmpeg.org)

**ImageMagick** as image edition and conversion software.
Thanks to image magick devs for this awesome open source image edition software suite.  [Web site link](http://imagemagick.net)
And thanks to dlemstra for the C# wrapper of this software. [CodePlex link](https://magick.codeplex.com)

**Ghostscript** as pdf edition software.
Thanks to ghostscript devs. [Download link](https://www.ghostscript.com/download/gsdnld.html)

**SharpShell** to easily create windows context menu extensions.
Thanks to Dave Kerr for his work on SharpShell. [CodePlex link](https://sharpshell.codeplex.com)

**Ripper** and **yeti.mmedia** for CD Audio extraction.
Thanks to Idael Cardoso for his work on CD Audio ripper. [Code project link](https://www.codeproject.com/Articles/5458/C-Sharp-Ripper)

**Markdown.XAML** for markdown rendering in the wpf application.
Thanks to Bevan Arps for his work on Markdown.XAML. [GitHub link](https://github.com/theunrepentantgeek/Markdown.XAML)

**WpfAnimatedGif** for animated gif rendering in the wpf application.
Thanks to Thomas Levesque for his work on WpfAnimatedGif. [GitHub link](https://github.com/XamlAnimatedGif/WpfAnimatedGif)

## License

File Converter is licensed under the GPL version 3 License.
For more information check the LICENSE.md file in your installation folder or the [gnu website](https://www.gnu.org/licenses/gpl.html).
