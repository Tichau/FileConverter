// <copyright file="NavigationService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System;
    using System.Collections.Generic;
    using System.Windows;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.DependencyInjection;

    using FileConverter.Annotations;

    using Application = FileConverter.Application;

    public class NavigationService : ObservableObject, INavigationService
    {
        private readonly Dictionary<string, PageInfo> pageInfoByType;
        private int numberOfPageShowed = 0;

        public NavigationService()
        {
            this.pageInfoByType = new Dictionary<string, PageInfo>();
        }
        
        public void Show([NotNull] string pageKey)
        {
            lock (this.pageInfoByType)
            {
                PageInfo pageInfo;
                if (!this.pageInfoByType.TryGetValue(pageKey, out pageInfo))
                {
                    throw new ArgumentException($"No such page: {pageKey}.", nameof(pageKey));
                }

                if (pageInfo.Showed)
                {
                    return;
                }

                pageInfo.Showed = true;

                if (pageInfo.Instance == null || !pageInfo.Instance.IsLoaded)
                {
                    pageInfo.Instance = Activator.CreateInstance(pageInfo.Type) as Window;
                }

                Diagnostics.Debug.Log($"Show page {pageKey}.");

                this.pageInfoByType[pageKey] = pageInfo;

                this.numberOfPageShowed++;

                if (pageInfo.CancelAutoExit)
                {
                    Application application = Application.Current as Application;
                    application?.CancelAutoExit();
                }

                pageInfo.Instance.Show();
            }
        }

        public void Close([NotNull] string pageKey, bool alreadyClosing)
        {
            if (pageKey == null)
            {
                throw new ArgumentNullException(nameof(pageKey));
            }

            lock (this.pageInfoByType)
            {
                PageInfo pageInfo;
                if (!this.pageInfoByType.TryGetValue(pageKey, out pageInfo))
                {
                    throw new ArgumentException($"No such page: {pageKey}.", nameof(pageKey));
                }

                if (!pageInfo.Showed)
                {
                    return;
                }

                pageInfo.Showed = false;
                
                Diagnostics.Debug.Log($"Close page {pageKey}.");

                this.pageInfoByType[pageKey] = pageInfo;

                if (pageInfo.MainWindow)
                {
                    this.CloseSecondaryWindowsIfThereIsNoOtherMainWindowShowed();
                }

                if (!alreadyClosing)
                {
                    pageInfo.Instance.Close();
                }

                this.numberOfPageShowed--;

                // If this is the last window.
                if (this.numberOfPageShowed == 0)
                {
                    IUpgradeService upgradeService = Ioc.Default.GetRequiredService<IUpgradeService>();
                    bool upgradeInProgress = upgradeService.UpgradeVersionDescription != null &&
                             upgradeService.UpgradeVersionDescription.NeedToUpgrade &&
                             !upgradeService.UpgradeVersionDescription.InstallerDownloadDone;

                    if (upgradeInProgress)
                    {
                        if (pageKey == Pages.Upgrade)
                        {
                            upgradeService.CancelUpgrade();
                            Application.AskForShutdown();
                        }
                        else
                        {
                            INavigationService navigationService = Ioc.Default.GetRequiredService<INavigationService>();
                            navigationService.Show(Pages.Upgrade);
                            Diagnostics.Debug.Log("There is an upgrade in progress, display the upgrade window.");
                        }
                    }
                    else
                    {
                        Application.AskForShutdown();
                    }
                }
            }
        }

        public void RegisterPage<T>(string pageKey, bool cancelAutoExit, bool mainWindow) where T : Window
        {
            if (string.IsNullOrEmpty(pageKey))
            {
                throw new ArgumentNullException(nameof(pageKey));
            }

            lock (this.pageInfoByType)
            {
                if (!this.pageInfoByType.ContainsKey(pageKey))
                {
                    this.pageInfoByType.Add(pageKey, new PageInfo(typeof(T), cancelAutoExit, mainWindow));
                }
                else
                {
                    throw new Exception($"Page {pageKey} already registered.");
                }
            }
        }

        private void CloseSecondaryWindowsIfThereIsNoOtherMainWindowShowed()
        {
            List<string> windowsToClose = new List<string>();
            foreach (KeyValuePair<string, PageInfo> keyValuePair in this.pageInfoByType)
            {
                if (keyValuePair.Value.MainWindow && keyValuePair.Value.Showed)
                {
                    // There is another main window showed.
                    return;
                }

                if (keyValuePair.Value.Showed)
                {
                    windowsToClose.Add(keyValuePair.Key);
                }
            }

            // Close all windows.
            for (int index = 0; index < windowsToClose.Count; index++)
            {
                this.Close(windowsToClose[index], false);
            }
        }

        private struct PageInfo
        {
            public Window Instance;
            public Type Type;
            public bool CancelAutoExit;
            public bool MainWindow;

            public bool Showed;

            public PageInfo(Type type, bool cancelAutoExit, bool mainWindow)
            {
                this.Instance = null;
                this.Type = type;
                this.CancelAutoExit = cancelAutoExit;
                this.MainWindow = mainWindow;
                this.Showed = false;
            }
        }
    }
}
