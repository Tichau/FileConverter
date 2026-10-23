// <copyright file="ConversionService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;

    using FileConverter.ConversionJobs;
    using FileConverter.Diagnostics;

    public class ConversionService : ObservableObject, IConversionService
    {
        private readonly object conversionQueueLock = new object();
        private readonly List<ConversionJob> conversionJobs = new List<ConversionJob>();

        private readonly int numberOfConversionThread = 1;

        private ISettingsService settingsService;
        private bool conversionQueueIsRunning;

        public ConversionService(ISettingsService settingsService)
        {
            if (settingsService == null)
            {
                throw new ArgumentNullException(nameof(settingsService));
            }

            this.settingsService = settingsService;

            this.ConversionJobs = this.conversionJobs.AsReadOnly();

            this.numberOfConversionThread = this.settingsService.Settings.MaximumNumberOfSimultaneousConversions;
            Debug.Log($"Maximum number of conversion threads: {this.numberOfConversionThread}");

            if (this.numberOfConversionThread <= 0)
            {
                this.numberOfConversionThread = System.Math.Max(1, Environment.ProcessorCount / 2);
                Debug.Log($"The number of processors on this computer is {Environment.ProcessorCount}. Set the default number of conversion threads to {this.numberOfConversionThread}");
            }
        }

        public event System.EventHandler<ConversionJobRegisteredEventArgs> ConversionJobRegistered;

        public event System.EventHandler<ConversionJobsTerminatedEventArgs> ConversionJobsTerminated;

        public ReadOnlyCollection<ConversionJob> ConversionJobs
        {
            get;
            private set;
        }

        public void RegisterConversionJob(ConversionJob conversionJob)
        {
            if (conversionJob == null)
            {
                throw new ArgumentNullException(nameof(conversionJob));
            }

            this.conversionJobs.Add(conversionJob);
            this.OnPropertyChanged(nameof(this.ConversionJobs));
            this.ConversionJobRegistered?.Invoke(this, new ConversionJobRegisteredEventArgs(conversionJob));
        }

        public void ConvertFilesAsync()
        {
            lock (this.conversionQueueLock)
            {
                if (this.conversionQueueIsRunning)
                {
                    Debug.Log("Conversion queue is already running.");
                    return;
                }

                this.conversionQueueIsRunning = true;
            }

            Thread fileConvertionThread = Helpers.InstantiateThread("ConversionQueueThread", this.ConvertFiles);
            fileConvertionThread.Start();
        }

        public void RetryConversionJob(ConversionJob conversionJob)
        {
            if (conversionJob == null)
            {
                throw new ArgumentNullException(nameof(conversionJob));
            }

            lock (this.conversionQueueLock)
            {
                if (this.conversionQueueIsRunning)
                {
                    Debug.Log("Can't retry a conversion while the queue is running.");
                    return;
                }
            }

            ConversionJob retryJob = ConversionJobFactory.Create(conversionJob.ConversionPreset, conversionJob.InitialInputPath);
            this.RegisterConversionJob(retryJob);
            this.ConvertFilesAsync();
        }

        private void ConvertFiles()
        {
            try
            {
                List<ConversionJob> activeJobs = new List<ConversionJob>();

                // Prepare conversions.
                for (int index = 0; index < this.ConversionJobs.Count; index++)
                {
                    if (this.ConversionJobs[index].State == ConversionState.Unknown)
                    {
                        this.ConversionJobs[index].PrepareConversion();
                        activeJobs.Add(this.ConversionJobs[index]);
                    }
                }

                if (activeJobs.Count == 0)
                {
                    Debug.Log("No pending conversion jobs to run.");
                    return;
                }

                System.Collections.Specialized.StringCollection files = new System.Collections.Specialized.StringCollection();
                // Convert!
                Thread[] jobThreads = new Thread[this.numberOfConversionThread];
                while (true)
                {
                    // Compute conversion flags.
                    ConversionFlags conversionFlags = ConversionFlags.None;
                    bool allJobAreFinished = true;
                    for (int jobIndex = 0; jobIndex < activeJobs.Count; jobIndex++)
                    {
                        ConversionJob conversionJob = activeJobs[jobIndex];
                        allJobAreFinished &= !(conversionJob.State == ConversionState.Ready || conversionJob.State == ConversionState.InProgress);

                        if (conversionJob.State == ConversionState.InProgress)
                        {
                            conversionFlags |= conversionJob.StateFlags;
                        }
                    }

                    if (allJobAreFinished)
                    {
                        break;
                    }

                    // Start job if possible.
                    for (int jobIndex = 0; jobIndex < activeJobs.Count; jobIndex++)
                    {
                        ConversionJob conversionJob = activeJobs[jobIndex];
                        if (conversionJob.State == ConversionState.Ready && conversionJob.CanStartConversion(conversionFlags))
                        {
                            // Find a thread to execute the job.
                            Thread jobThread = null;
                            for (int threadIndex = 0; threadIndex < jobThreads.Length; threadIndex++)
                            {
                                Thread thread = jobThreads[threadIndex];
                                if (thread == null || !thread.IsAlive)
                                {
                                    jobThread = Helpers.InstantiateThread(conversionJob.GetType().Name, this.ExecuteConversionJob);
                                    jobThreads[threadIndex] = jobThread;
                                    break;
                                }
                            }

                            if (jobThread != null)
                            {
                                jobThread.Start(conversionJob);

                                while (conversionJob.State == ConversionState.Ready)
                                {
                                    Debug.Log("Wait the launch of the conversion thread before launching any other thread.");
                                    Thread.Sleep(20);
                                }
                            }

                            if (!files.Contains(conversionJob.OutputFilePath))
                            {
                                files.Add(conversionJob.OutputFilePath);
                            }

                            break;
                        }
                    }

                    Thread.Sleep(50);
                }

                // Copy the output files to the clipboard
                if (this.settingsService.Settings.CopyFilesInClipboardAfterConversion && files.Count > 0)
                {
                    Thread clipboardThread = Helpers.InstantiateThread("CopyFilesToClipboardThread", this.CopyFilesToClipboard);
                    clipboardThread.SetApartmentState(ApartmentState.STA);
                    clipboardThread.Start(files);
                }

                bool allConversionsSucceed = true;
                for (int index = 0; index < activeJobs.Count; index++)
                {
                    allConversionsSucceed &= activeJobs[index].State == ConversionState.Done;
                }

                if (this.ConversionJobsTerminated != null)
                {
                    this.ConversionJobsTerminated.Invoke(this, new ConversionJobsTerminatedEventArgs(allConversionsSucceed));
                }
            }
            finally
            {
                lock (this.conversionQueueLock)
                {
                    this.conversionQueueIsRunning = false;
                }
            }
        }

        private void ExecuteConversionJob(object parameter)
        {
            ConversionJob conversionJob = parameter as ConversionJob;
            if (conversionJob == null)
            {
                throw new System.ArgumentException("The parameter must be a conversion job.", nameof(parameter));
            }

            if (conversionJob.State != ConversionState.Ready)
            {
                Debug.LogError("Fail to execute conversion job.");
                return;
            }

            try
            {
                conversionJob.StartConversion();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failure during conversion: {exception}");
            }
        }

        private void CopyFilesToClipboard(object _filePaths)
        {
            try
            {
                System.Collections.Specialized.StringCollection FilePaths = _filePaths as System.Collections.Specialized.StringCollection;
                System.Windows.Forms.Clipboard.SetFileDropList(FilePaths);
                Debug.Log("Output files copied to the clipboard:");
                for (int index = 0; index < FilePaths.Count; index++)
                {
                    Debug.Log($"  {FilePaths[index]}");
                }
            }
            catch (Exception exception)
            {
                Debug.Log("Can't copy files to the clipboard.");
                Debug.Log($"An exception has been thrown: {exception}.");
            }
        }
    }
}
