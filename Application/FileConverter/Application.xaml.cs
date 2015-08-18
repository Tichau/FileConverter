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
    using System.Windows;

    using FileConverter.ConversionJobs;

    public partial class Application : System.Windows.Application
    {
        private static readonly Version Version = new Version()
                                            {
                                                Major = 0, 
                                                Minor = 2,
                                            };

        private readonly List<ConversionJob> conversionJobs = new List<ConversionJob>();

        private bool debugMode;
        private bool initialized;
        private bool cancelAutoExit;

        public Application()
        {
            this.ConvertionJobs = this.conversionJobs.AsReadOnly();

            this.Initialize();

            if (this.initialized)
            {
                Thread fileConvertionThread = new Thread(this.ConvertFiles);
                fileConvertionThread.Start();
            }
        }

        public static Version ApplicationVersion
        {
            get
            {
                return Application.Version;
            }
        }

        public ReadOnlyCollection<ConversionJob> ConvertionJobs
        {
            get;
            private set;
        }

        public Settings Settings
        {
            get;
            private set;
        }

        public bool ShowSettings
        {
            get;
            set;
        }

        public bool Verbose
        {
            get;
            set;
        }

        public void CancelAutoExit()
        {
            this.cancelAutoExit = true;
        }
        
        private void Initialize()
        {
            // Load settigns.
            Diagnostics.Log("Retrieve arguments...");
            this.Settings = new Settings();
            this.Settings.Load();

            // Retrieve arguments.
            Diagnostics.Log("Retrieve arguments...");
            string[] args = Environment.GetCommandLineArgs();

#if (DEBUG)
            if (args.Length <= 1)
            {
                this.debugMode = true;
                System.Array.Resize(ref args, 9);
                args[1] = "--conversion-preset";
                args[2] = "To Mp3";
                args[3] = @"D:\Projects\FileConverter\TestFiles\Herbie Hancock - Speak Like A Child [RVG Edition].flac";
                args[3] = @"D:\Projects\FileConverter\TestFiles\01 - Le Bruit Du Bang.wma";
                args[4] = @"D:\Projects\FileConverter\TestFiles\test\Toccata.wav";
                args[5] = @"D:\Projects\FileConverter\TestFiles\test\Toccata - Copie (4).wav";
                args[5] = "--verbose";
                args[6] = @"D:\Projects\FileConverter\TestFiles\test\Toccata - Copie (3).wav";
                args[7] = @"D:\Projects\FileConverter\TestFiles\test\Toccata - Copie (2).wav";
                args[8] = @"D:\Projects\FileConverter\TestFiles\test\Toccata - Copie (5).wav";

                System.Array.Resize(ref args, 2);
                args[1] = "--settings";
            }
#endif

            for (int index = 0; index < args.Length; index++)
            {
                string argument = args[index];
                Diagnostics.Log("Arg{0}: {1}", index, argument);
            }

            Diagnostics.Log(string.Empty);

            ConversionPreset conversionPreset = null;
            List<string> filePaths = new List<string>();

            // Parse arguments.
            for (int index = 1; index < args.Length; index++)
            {
                string argument = args[index];
                if (argument.StartsWith("--"))
                {
                    // This is an optional parameter.
                    string parameterTitle = argument.Substring(2).ToLowerInvariant();

                    switch (parameterTitle)
                    {
                        case "settings":
                            this.ShowSettings = true;
                            return;

                        case "apply-settings":
                            Settings.ApplyTemporarySettings();
                            Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
                            return;

                        case "conversion-preset":
                            if (index >= args.Length - 1)
                            {
                                MessageBox.Show(string.Format("ERROR ! Invalid format."), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
                                return;
                            }

                            conversionPreset = Settings.GetPresetFromName(args[index + 1]);
                            if (conversionPreset == null)
                            {
                                MessageBox.Show(string.Format("Invalid conversion preset '{0}'.", args[index + 1]), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
                                return;
                            }

                            index++;
                            continue;
                            
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

            if (conversionPreset == null)
            {
                Diagnostics.Log("ERROR ! Can't retrieve the conversion preset from arguments.");
                return;
            }

            // Create convertion jobs.
            for (int index = 0; index < filePaths.Count; index++)
            {
                string inputFilePath = filePaths[index];
                ConversionJob conversionJob = ConversionJobFactory.Create(conversionPreset);
                conversionJob.PrepareConversion(inputFilePath);

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

#if !DEBUG
            bool allConversionsSucceed = true;
            for (int index = 0; index < this.conversionJobs.Count; index++)
            {
                allConversionsSucceed &= this.conversionJobs[index].State == ConversionJob.ConversionState.Done;
            }

            if (allConversionsSucceed)
            {
                System.Threading.Thread.Sleep(3000);

                if (this.cancelAutoExit)
                {
                    return;
                }

                Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
            }
#endif
        }
    }
}
