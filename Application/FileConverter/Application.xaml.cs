// <copyright file="Application.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

/*  File Converter - This program allow you to convert file format to another.
    Copyright (C) 2015 Adrien Allard
    email: adrien.allard.pro@gmail.com

    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;

    public partial class Application : System.Windows.Application
    {
        private readonly List<ConversionJob> conversionJobs = new List<ConversionJob>();

        private bool debugMode;
        private bool initialized;

        public Application()
        {
            this.Initialize();

            if (this.initialized)
            {
                Thread fileConvertionThread = new Thread(this.ConvertFiles);
                fileConvertionThread.Start();
            }
        }

        public ReadOnlyCollection<ConversionJob> ConvertionJobs
        {
            get;
            private set;
        }

        public bool Verbose
        {
            get;
            set;
        }

        private void Initialize()
        {
            this.ConvertionJobs = this.conversionJobs.AsReadOnly();

            Diagnostics.Log("Retrieve arguments...");
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length <= 1)
            {
                this.debugMode = true;
                System.Array.Resize(ref args, 9);
                args[1] = "--output-type";
                args[2] = "mp3";
                args[3] = @"D:\Projects\FileConverter\TestFiles\Herbie Hancock - Speak Like A Child [RVG Edition].flac";
                args[4] = @"D:\Projects\FileConverter\TestFiles\test\Toccata.wav";
                args[5] = @"D:\Projects\FileConverter\TestFiles\test\Toccata - Copie (4).wav";
                //// args[5] = "--verbose";
                args[6] = @"D:\Projects\FileConverter\TestFiles\test\Toccata - Copie (3).wav";
                args[7] = @"D:\Projects\FileConverter\TestFiles\test\Toccata - Copie (2).wav";
                args[8] = @"D:\Projects\FileConverter\TestFiles\test\Toccata - Copie (5).wav";
            }

            for (int index = 0; index < args.Length; index++)
            {
                string argument = args[index];
                Diagnostics.Log("Arg{0}: {1}", index, argument);
            }

            Diagnostics.Log(string.Empty);

            OutputType outputType = OutputType.None;
            List<string> filePaths = new List<string>();
            for (int index = 1; index < args.Length; index++)
            {
                string argument = args[index];
                if (argument.StartsWith("--"))
                {
                    // This is an optional parameter.
                    string parameterTitle = argument.Substring(2).ToLowerInvariant();

                    switch (parameterTitle)
                    {
                        case "output-type":
                            {
                                if (index >= args.Length - 1)
                                {
                                    Diagnostics.Log("ERROR ! Invalid format.");
                                    return;
                                }

                                string outputTypeName = args[index + 1].ToLowerInvariant();
                                switch (outputTypeName)
                                {
                                    case "mp3":
                                        outputType = OutputType.Mp3;
                                        break;

                                    case "ogg":
                                        outputType = OutputType.Ogg;
                                        break;

                                    case "flac":
                                        outputType = OutputType.Flac;
                                        break;

                                    default:
                                        Diagnostics.Log("ERROR ! Unknown output type {0}.", outputType);
                                        return;
                                }

                                index++;
                                continue;
                            }

                        case "verbose":
                            {
                                this.Verbose = true;
                            }

                            break;

                        default:
                            Diagnostics.Log("ERROR ! Unknown option {0}.", parameterTitle);
                            return;
                    }
                }
                else
                {
                    filePaths.Add(argument);
                }
            }

            if (outputType == OutputType.None)
            {
                Diagnostics.Log("ERROR ! Can't retrieve the output type from arguments.");
                return;
            }

            // Create convertion jobs.
            for (int index = 0; index < filePaths.Count; index++)
            {
                ConversionJob conversionJob = new ConversionJob();

                string inputFilePath = filePaths[index];
                string extension = System.IO.Path.GetExtension(inputFilePath);
                string outputFilePath = inputFilePath.Substring(0, inputFilePath.Length - extension.Length) + "."
                                        + outputType.ToString().ToLowerInvariant();

                conversionJob.Initialize(outputType, inputFilePath, outputFilePath);

                this.conversionJobs.Add(conversionJob);
            }

            this.initialized = true;
        }

        private void ConvertFiles()
        {
            for (int index = 0; index < this.conversionJobs.Count; index++)
            {
                ConversionJob conversionJob = this.conversionJobs[index];
                conversionJob.StartConvertion();

                if (System.IO.File.Exists(conversionJob.OutputFilePath))
                {
                    Diagnostics.Log("Success!");

                    if (this.debugMode)
                    {
                        Diagnostics.Log("Delete file {0}.", conversionJob.OutputFilePath);
                        try
                        {
                            System.IO.File.Delete(conversionJob.OutputFilePath);
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    Diagnostics.Log("Fail!");
                }
            }

            Diagnostics.Log("End of job queue.");
        }
    }
}
