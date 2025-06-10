using DevExpress.Mvvm;
using System;

namespace FuelEase.SplashScreen
{
    public interface ICustomSplashScreenService
    {
        void Show();
        void Close();
        void Show(string message);
        void Show(string message, string title);
        event Action<DXSplashScreenViewModel> OnShow;
        event Action OnClose;
        CustomSplashScreenState CustomSplashScreenState { get; set; }
    }

    /// <summary>
    /// Статус загрузки
    /// </summary>
    public enum CustomSplashScreenState
    {
        None,

        /// <summary>
        /// Показывается
        /// </summary>
        Showing,

        /// <summary>
        /// Закрыта
        /// </summary>
        Closing
    }
}
