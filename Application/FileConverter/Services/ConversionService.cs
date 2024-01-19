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
        private readonly List<ConversionJob> conversionJobs = new List<ConversionJob>();

        private readonly int numberOfConversionThread = 1;

        public ConversionService(ISettingsService settingsService)
        {
            if (settingsService == null)
            {
                throw new ArgumentNullException(nameof(settingsService));
            }

            this.ConversionJobs = this.conversionJobs.AsReadOnly();

            this.numberOfConversionThread = settingsService.Settings.MaximumNumberOfSimultaneousConversions;
            Diagnostics.Debug.Log("Maximum number of conversion threads: {0}", this.numberOfConversionThread);

            if (this.numberOfConversionThread <= 0)
            {
                this.numberOfConversionThread = System.Math.Max(1, Environment.ProcessorCount / 2);
                Diagnostics.Debug.Log("The number of processors on this computer is {0}. Set the default number of conversion threads to {0}", settingsService.Settings.MaximumNumberOfSimultaneousConversions);
            }
        }

        public event System.EventHandler<ConversionJobsTerminatedEventArgs> ConversionJobsTerminated;

        public ReadOnlyCollection<ConversionJob> ConversionJobs
        {
            get;
            private set;
        }

        public void RegisterConversionJob(ConversionJob conversionJob)
        {
            this.conversionJobs.Add(conversionJob);
            this.OnPropertyChanged(nameof(this.ConversionJobs));
        }

        public void ConvertFilesAsync()
        {
            Thread fileConvertionThread = Helpers.InstantiateThread("ConversionQueueThread", this.ConvertFiles);
            fileConvertionThread.Start();
        }

        private void ConvertFiles()
        {
            // Prepare conversions.
            for (int index = 0; index < this.ConversionJobs.Count; index++)
            {
                this.ConversionJobs[index].PrepareConversion();
            }

            // Convert!
            Thread[] jobThreads = new Thread[this.numberOfConversionThread];
            while (true)
            {
                // Compute conversion flags.
                ConversionFlags conversionFlags = ConversionFlags.None;
                bool allJobAreFinished = true;
                for (int jobIndex = 0; jobIndex < this.conversionJobs.Count; jobIndex++)
                {
                    ConversionJob conversionJob = this.conversionJobs[jobIndex];
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
                for (int jobIndex = 0; jobIndex < this.conversionJobs.Count; jobIndex++)
                {
                    ConversionJob conversionJob = this.conversionJobs[jobIndex];
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

                        break;
                    }
                }

                Thread.Sleep(50);
            }

            bool allConversionsSucceed = true;
            for (int index = 0; index < this.conversionJobs.Count; index++)
            {
                allConversionsSucceed &= this.conversionJobs[index].State == ConversionState.Done;
            }

            if (this.ConversionJobsTerminated != null)
            {
                this.ConversionJobsTerminated.Invoke(this, new ConversionJobsTerminatedEventArgs(allConversionsSucceed));
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
                Debug.LogError("Failure during conversion: {0}", exception.ToString());
            }
        }
    }
}
