// <copyright file="ViewModelLocator.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:FileConverter"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
*/

namespace FileConverter.ViewModels
{
    using CommunityToolkit.Mvvm.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
        }

        public HelpViewModel Help => Ioc.Default.GetRequiredService<HelpViewModel>();

        public MainViewModel Main => Ioc.Default.GetRequiredService<MainViewModel>();

        public UpgradeViewModel Upgrade => Ioc.Default.GetRequiredService<UpgradeViewModel>();

        public SettingsViewModel Settings => Ioc.Default.GetRequiredService<SettingsViewModel>();

        public DiagnosticsViewModel Diagnostics => Ioc.Default.GetRequiredService<DiagnosticsViewModel>();

        internal void RegisterViewModels(ServiceCollection services)
        {
            services
                .AddSingleton<HelpViewModel>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<UpgradeViewModel>()
                .AddSingleton<SettingsViewModel>()
                .AddSingleton<DiagnosticsViewModel>();
        }
    }
}