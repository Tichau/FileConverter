// <copyright file="INavigationService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.Services
{
    using System.Windows;

    public interface INavigationService : GalaSoft.MvvmLight.Views.INavigationService
    {
        void RegisterPage<T>(string pageKey, bool cancelAutoExit) where T : Window;
    }
}