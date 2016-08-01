// <copyright file="Application.xaml.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

/*  File Converter - This program allow you to convert file format to another.
    Copyright (C) 2016 Adrien Allard
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
    using System.Threading.Tasks;
    using System.Windows;

    using FileConverter.ConversionJobs;
    using FileConverter.Diagnostics;
    using FileConverter.Windows;

    public partial class Application : System.Windows.Application
    {
        private static readonly Version Version = new Version()
                                            {
                                                Major = 1, 
                                                Minor = 0,
                                                Patch = 0,
                                            };

        private readonly List<ConversionJob> conversionJobs = new List<ConversionJob>();

        private int numberOfConversionThread = 1;

        private bool needToRunConversionThread;
        private bool cancelAutoExit;
        private bool isSessionEnding;
        
        public Application()
        {
            this.ConvertionJobs = this.conversionJobs.AsReadOnly();
        }

        public event EventHandler<ApplicationTerminateArgs> OnApplicationTerminate;

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
            set;
        }

        public bool ShowSettings
        {
            get;
            set;
        }

        public bool HideMainWindow
        {
            get;
            set;
        }

        public UpgradeVersionDescription UpgradeVersionDescription
        {
            get;
            private set;
        }

        public bool Verbose
        {
            get;
            set;
        }

        public void CancelAutoExit()
        {
            this.cancelAutoExit = true;

            if (this.OnApplicationTerminate != null)
            {
                this.OnApplicationTerminate.Invoke(this, new ApplicationTerminateArgs(float.NaN));
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.Initialize();

            if (this.needToRunConversionThread)
            {
                Thread fileConvertionThread = new Thread(this.ConvertFiles);
                fileConvertionThread.Start();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Debug.Log("Exit application.");
            
            if (!this.isSessionEnding && this.UpgradeVersionDescription != null && this.UpgradeVersionDescription.NeedToUpgrade)
            {
                Debug.Log("A new version of file converter has been found: {0}.", this.UpgradeVersionDescription.LatestVersion);

                if (string.IsNullOrEmpty(this.UpgradeVersionDescription.InstallerPath))
                {
                    Debug.LogError("Invalid installer path.");
                }
                else
                {
                    Debug.Log("Wait for the end of the installer download.");
                    while (this.UpgradeVersionDescription.InstallerDownloadInProgress)
                    {
                        Thread.Sleep(1000);
                    }

                    string installerPath = this.UpgradeVersionDescription.InstallerPath;
                    if (!System.IO.File.Exists(installerPath))
                    {
                        Debug.LogError("Can't find upgrade installer ({0}). Try to restart the application.", installerPath);
                        return;
                    }

                    // Start process.
                    Debug.Log("Start file converter upgrade from version {0} to {1}.", ApplicationVersion, this.UpgradeVersionDescription.LatestVersion);

                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(installerPath)
                        {
                            UseShellExecute = true,
                        };

                    Debug.Log("Start upgrade process: {0}{1}.", System.IO.Path.GetFileName(startInfo.FileName), startInfo.Arguments);
                    System.Diagnostics.Process process = new System.Diagnostics.Process
                    {
                        StartInfo = startInfo
                    };

                    process.Start();
                }
            }

            Debug.Release();
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            this.isSessionEnding = true;
            this.Shutdown();
        }

        private void Initialize()
        {
#if BUILD32
            Diagnostics.Debug.Log("File Converter v" + ApplicationVersion.ToString() + " (32 bits)");
#else
            Diagnostics.Debug.Log("File Converter v" + ApplicationVersion.ToString() + " (64 bits)");
#endif

            // Retrieve arguments.
            Debug.Log("Retrieve arguments...");
            string[] args = Environment.GetCommandLineArgs();

#if (DEBUG)
            {
                ////System.Array.Resize(ref args, 5);
                ////args[1] = "--conversion-preset";
                ////args[2] = "To Png";
                ////args[3] = "--verbose";
                ////args[4] = @"D:\Test\images\Mario Big.png";
            }
#endif

            // Log arguments.
            for (int index = 0; index < args.Length; index++)
            {
                string argument = args[index];
                Debug.Log("Arg{0}: {1}", index, argument);
            }

            Debug.Log(string.Empty);

            if (args.Length == 1)
            {
                // Diplay help windows to explain that this application is a context menu extension.
                ApplicationStartHelp applicationStartHelp = new ApplicationStartHelp();
                applicationStartHelp.Show();
                this.HideMainWindow = true;
                return;
            }

            // Parse arguments.
            List<string> filePaths = new List<string>();
            string conversionPresetName = null;
            for (int index = 1; index < args.Length; index++)
            {
                string argument = args[index];
                if (string.IsNullOrEmpty(argument))
                {
                    continue;
                }

                if (argument.StartsWith("--"))
                {
                    // This is an optional parameter.
                    string parameterTitle = argument.Substring(2).ToLowerInvariant();

                    switch (parameterTitle)
                    {
                        case "post-install-init":
                            Settings.PostInstallationInitialization();
                            Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
                            return;

                        case "version":
                            Console.Write(ApplicationVersion.ToString());
                            Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
                            return;

                        case "settings":
                            this.ShowSettings = true;
                            this.HideMainWindow = true;
                            break;

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

                            conversionPresetName = args[index + 1];
                            index++;
                            continue;

                        case "verbose":
                            {
                                this.Verbose = true;
                            }

                            break;

                        default:
                            Debug.LogError("Unknown application argument: '--{0}'.", parameterTitle);
                            return;
                    }
                }
                else
                {
                    filePaths.Add(argument);
                }
            }

            // Load settigns.
            Debug.Log("Load settings...");
            this.Settings = Settings.Load();
            if (this.Settings == null)
            {
                Diagnostics.Debug.LogError("The application will now shutdown. If you want to fix the problem yourself please edit or delete the file: C:\\Users\\UserName\\AppData\\Local\\FileConverter\\Settings.user.xml.");
                Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
                return;
            }

            if (this.Settings.MaximumNumberOfSimultaneousConversions <= 0)
            {
                this.Settings.MaximumNumberOfSimultaneousConversions = System.Math.Max(1, Environment.ProcessorCount / 2);
                Diagnostics.Debug.Log("The number of processors on this computer is {0}. Set the default number of conversion threads to {0}", this.Settings.MaximumNumberOfSimultaneousConversions);
            }

            this.numberOfConversionThread = this.Settings.MaximumNumberOfSimultaneousConversions;
            Diagnostics.Debug.Log("Maximum number of conversion threads: {0}", this.numberOfConversionThread);

            // Check upgrade.
            if (this.Settings.CheckUpgradeAtStartup)
            {
#if DEBUG
                Task<UpgradeVersionDescription> task = Upgrade.Helpers.GetLatestVersionDescriptionAsync(this.OnGetLatestVersionDescription);
#else
                long fileTime = Registry.GetValue<long>(Registry.Keys.LastUpdateCheckDate);
                DateTime lastUpdateDateTime = DateTime.FromFileTime(fileTime);

                TimeSpan durationSinceLastUpdate = DateTime.Now.Subtract(lastUpdateDateTime);
                if (durationSinceLastUpdate > new TimeSpan(1, 0, 0, 0))
                {
                    Task<UpgradeVersionDescription> task = Upgrade.Helpers.GetLatestVersionDescriptionAsync(this.OnGetLatestVersionDescription);
                }
#endif
            }

            ConversionPreset conversionPreset = null;
            if (!string.IsNullOrEmpty(conversionPresetName))
            {
                conversionPreset = this.Settings.GetPresetFromName(conversionPresetName);
                if (conversionPreset == null)
                {
                    Debug.LogError("Invalid conversion preset '{0}'. (code 0x02)", conversionPresetName);
                    Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
                    return;
                }
            }

            if (conversionPreset != null)
            {
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

                this.needToRunConversionThread = true;
            }
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

            if (this.Settings.ExitApplicationWhenConversionsFinished)
            {
                bool allConversionsSucceed = true;
                for (int index = 0; index < this.conversionJobs.Count; index++)
                {
                    allConversionsSucceed &= this.conversionJobs[index].State == ConversionJob.ConversionState.Done;
                }

                if (this.cancelAutoExit)
                {
                    return;
                }

                if (allConversionsSucceed)
                {
                    float remainingTime = this.Settings.DurationBetweenEndOfConversionsAndApplicationExit;
                    while (remainingTime > 0f)
                    {
                        if (this.OnApplicationTerminate != null)
                        {
                            this.OnApplicationTerminate.Invoke(this, new ApplicationTerminateArgs(remainingTime));
                        }

                        System.Threading.Thread.Sleep(1000);
                        remainingTime--;

                        if (this.cancelAutoExit)
                        {
                            return;
                        }
                    }

                    if (this.OnApplicationTerminate != null)
                    {
                        this.OnApplicationTerminate.Invoke(this, new ApplicationTerminateArgs(remainingTime));
                    }

                    Dispatcher.BeginInvoke((Action)(() => Application.Current.Shutdown()));
                }
            }
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

            try
            {
                conversionJob.StartConvertion();
            }
            catch (Exception exception)
            {
                Debug.LogError("Failure during conversion: {0}", exception.ToString());
            }
        }

        private void OnGetLatestVersionDescription(UpgradeVersionDescription upgradeVersionDescription)
        {
            if (upgradeVersionDescription == null)
            {
                return;
            }
            
            Registry.SetValue(Registry.Keys.LastUpdateCheckDate, DateTime.Now.ToFileTime());

            if (upgradeVersionDescription.LatestVersion <= ApplicationVersion)
            {
                return;
            }

            this.UpgradeVersionDescription = upgradeVersionDescription;
            (this.MainWindow as MainWindow).OnNewVersionReleased(upgradeVersionDescription);
        }
    }
}
