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

using System.Diagnostics;

namespace FileConverter
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Windows;

    using FileConverter.ConversionJobs;
    using FileConverter.Diagnostics;

    public partial class Application : System.Windows.Application
    {
        private int numberOfConversionThread = 1;

        private static readonly Version Version = new Version()
                                            {
                                                Major = 0, 
                                                Minor = 3,
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
            Diagnostics.Debug.Log("The number of processors on this computer is {0}. Set the default number of conversion threads to {0}", Environment.ProcessorCount);
            this.numberOfConversionThread = Environment.ProcessorCount;

            // Load settigns.
            Debug.Log("Load settings...");
            this.Settings = new Settings();
            this.Settings.Load();

            // Retrieve arguments.
            Debug.Log("Retrieve arguments...");
            string[] args = Environment.GetCommandLineArgs();

#if (DEBUG)
            if (args.Length <= 1)
            {
                this.debugMode = true;
                System.Array.Resize(ref args, 8);
                args[1] = "--conversion-preset";
                args[2] = "To Ogg";
                args[3] = "--verbose";
                
                args[4] = @"D:\Test\TrailerV2 compressed.mkv";
                args[4] = @"D:\Test\image.png";
                args[4] = @"E:\Track01.cda";
                args[4] = @"D:\Test\Track01.mp3";
                args[5] = @"D:\Test\Track02.mp3";
                args[6] = @"D:\Test\Track03.mp3";
                args[7] = @"D:\Test\Track04.mp3";

                System.Array.Resize(ref args, 2);
                args[1] = "--settings";

                //System.Array.Resize(ref args, 4);
                //args[1] = "--conversion-preset";
                //args[2] = "To Ogg";
                //args[3] = "--verbose";

                System.Array.Resize(ref args, 6);
                args[1] = "--conversion-preset";
                args[2] = "Extract CDA To Ogg";
                args[3] = "--verbose";

                args[4] = @"E:\Track01.cda";
                args[5] = @"E:\Track02.cda";
            }
#endif

            for (int index = 0; index < args.Length; index++)
            {
                string argument = args[index];
                Debug.Log("Arg{0}: {1}", index, argument);
            }

            Debug.Log(string.Empty);

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
                                Debug.LogError("Invalid format. (code 0x01)");
                                Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
                                return;
                            }

                            conversionPreset = this.Settings.GetPresetFromName(args[index + 1]);
                            if (conversionPreset == null)
                            {
                                Debug.LogError("Invalid conversion preset '{0}'. (code 0x02)", args[index + 1]);
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
                            Debug.LogError("Unknown option {0}.", parameterTitle);
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
                Debug.LogError("Can't retrieve the conversion preset from arguments.");
                return;
            }

            // Create convertion jobs.
            Debug.Log("Create jobs for conversion preset: '{0}'", conversionPreset.Name);
            try
            {
                for (int index = 0; index < filePaths.Count; index++)
                {
                    string inputFilePath = filePaths[index];
                    ConversionJob conversionJob = ConversionJobFactory.Create(conversionPreset, inputFilePath);
                    conversionJob.PrepareConversion(inputFilePath);

                    this.conversionJobs.Add(conversionJob);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
                throw;
            }

            this.initialized = true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Debug.Log("Exit application.");

            Debug.Release();
        }

        private void ConvertFiles()
        {
            Thread[] jobThreads = new Thread[this.numberOfConversionThread];
            
            while (true)
            {
                // Compute conversion flags.
                ConversionFlags conversionFlags = ConversionFlags.None;
                bool allJobAreFinished = true;
                for (int jobIndex = 0; jobIndex < this.conversionJobs.Count; jobIndex++)
                {
                    ConversionJob conversionJob = this.conversionJobs[jobIndex];
                    allJobAreFinished &= !(conversionJob.State == ConversionJob.ConversionState.Ready ||
                                         conversionJob.State == ConversionJob.ConversionState.InProgress);

                    if (conversionJob.State == ConversionJob.ConversionState.InProgress)
                    {
                        conversionFlags |= conversionJob.StateFlags;
                    }
                }

                if (allJobAreFinished)
                {
                    break;
                }

                // Start job if possible.
                for (int jobIndex = 0; jobIndex < this.conversionJobs.Count; jobIndex++)
                {
                    ConversionJob conversionJob = this.conversionJobs[jobIndex];
                    if (conversionJob.State == ConversionJob.ConversionState.Ready &&
                        conversionJob.CanStartConversion(conversionFlags))
                    {
                        // Find a thread to execute the job.
                        Thread jobThread = null;
                        for (int threadIndex = 0; threadIndex < jobThreads.Length; threadIndex++)
                        {
                            Thread thread = jobThreads[threadIndex];
                            if (thread == null || !thread.IsAlive)
                            {
                                jobThread = new Thread(this.ExecuteConversionJob);
                                jobThreads[threadIndex] = jobThread;
                                break;
                            }
                        }

                        if (jobThread != null)
                        {
                            jobThread.Start(conversionJob);
                        }

                        break;
                    }
                }

                Thread.Sleep(50);
            }

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

        private void ExecuteConversionJob(object parameter)
        {
            ConversionJob conversionJob = parameter as ConversionJob;
            if (conversionJob == null)
            {
                throw new System.ArgumentException("The parameter must be a conversion job.", "parameter");
            }

            if (conversionJob.State != ConversionJob.ConversionState.Ready)
            {
                Debug.LogError("Fail to execute conversion job.");
                return;
            }

            conversionJob.StartConvertion();

            if (conversionJob.State == ConversionJob.ConversionState.Done && !System.IO.File.Exists(conversionJob.OutputFilePath))
            {
                Debug.LogError("Can't find the output file.");
            }
            else if (conversionJob.State == ConversionJob.ConversionState.Failed && System.IO.File.Exists(conversionJob.OutputFilePath))
            {
                Debug.Log("The conversion job failed but there is an output file that does exists.");
            }
        }
    }
}
