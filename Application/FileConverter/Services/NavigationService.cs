// <copyright file="NavigationService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Ioc;

    using Application = FileConverter.Application;

    public class NavigationService : ObservableObject, INavigationService
    {
        private readonly Dictionary<string, PageInfo> pageInfoByType;
        private readonly List<string> historic;
        private string currentPageKey;

        public NavigationService()
        {
            this.pageInfoByType = new Dictionary<string, PageInfo>();
            this.historic = new List<string>();

            SimpleIoc.Default.Register<INavigationService>(() => this);
        }

        public string CurrentPageKey
        {
            get => this.currentPageKey;

            private set
            {
                if (this.currentPageKey == value)
                {
                    return;
                }

                this.currentPageKey = value;
                this.RaisePropertyChanged();
            }
        }

        public object Parameter { get; private set; }
        
        public void GoBack()
        {
            lock (this.pageInfoByType)
            {
                if (this.historic.Count <= 0)
                {
                    return;
                }

                string pageKey = this.historic.Last();
                PageInfo pageInfo;
                if (!this.pageInfoByType.TryGetValue(pageKey, out pageInfo))
                {
                    throw new ArgumentException($"No such page: {pageKey} ", nameof(pageKey));
                }

                this.historic.RemoveAt(this.historic.Count - 1);
                this.CurrentPageKey = this.historic.LastOrDefault();

                Diagnostics.Debug.Log($"Go back from {pageKey ?? "null"} to {this.CurrentPageKey ?? "null"}.");

                this.pageInfoByType[pageKey] = pageInfo;

                if (pageInfo.Instance.IsVisible)
                {
                    pageInfo.Instance.Close();
                }

                if (string.IsNullOrEmpty(this.CurrentPageKey))
                {
                    Application.Current.Shutdown();
                }
            }
        }

        public void NavigateTo(string pageKey)
        {
            this.NavigateTo(pageKey, null);
        }

        public virtual void NavigateTo(string pageKey, object parameter)
        {
            lock (this.pageInfoByType)
            {
                PageInfo pageInfo;
                if (!this.pageInfoByType.TryGetValue(pageKey, out pageInfo))
                {
                    throw new ArgumentException($"No such page: {pageKey}.", nameof(pageKey));
                }

                if (pageInfo.Instance == null || !pageInfo.Instance.IsLoaded)
                {
                    pageInfo.Instance = Activator.CreateInstance(pageInfo.Type) as Window;
                }

                this.Parameter = parameter;
                this.historic.Add(pageKey);
                this.CurrentPageKey = pageKey;

                Diagnostics.Debug.Log($"Navigate to {this.CurrentPageKey ?? "null"}.");

                this.pageInfoByType[pageKey] = pageInfo;
                
                if (pageInfo.Instance.IsVisible)
                {
                    return;
                }

                if (pageInfo.CancelAutoExit)
                {
                    Application application = Application.Current as Application;
                    application?.CancelAutoExit();
                }

                pageInfo.Instance.Show();
            }
        }

        public void RegisterPage<T>(string pageKey, bool cancelAutoExit) where T : Window
        {
            if (string.IsNullOrEmpty(pageKey))
            {
                throw new ArgumentNullException(nameof(pageKey));
            }

            lock (this.pageInfoByType)
            {
                if (!this.pageInfoByType.ContainsKey(pageKey))
                {
                    this.pageInfoByType.Add(pageKey, new PageInfo(typeof(T), cancelAutoExit));
                }
                else
                {
                    throw new Exception($"Page {pageKey} already registered.");
                }
            }
        }

        private struct PageInfo
        {
            public Window Instance;
            public Type Type;
            public bool CancelAutoExit;

            public PageInfo(Type type, bool cancelAutoExit)
            {
                this.Instance = null;
                this.Type = type;
                this.CancelAutoExit = cancelAutoExit;
            }
        }
    }
}
