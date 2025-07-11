using DevExpress.Mvvm;
using System;

namespace KIT.GasStation.SplashScreen
{
    public class CustomSplashScreenService : ICustomSplashScreenService
    {
        #region Actions

        public event Action<DXSplashScreenViewModel> OnShow;
        public event Action OnClose;

        #endregion

        #region Public Properties

        public CustomSplashScreenState CustomSplashScreenState { get; set; }

        #endregion

        #region Private Members

        private DXSplashScreenViewModel _viewModel;

        #endregion

        #region Constructors

        public CustomSplashScreenService()
        {
            _viewModel = new();
        }

        #endregion

        #region Public Voids

        public void Show()
        {
            OnShow?.Invoke(_viewModel);
        }
        public void Close()
        {
            OnClose?.Invoke();
        }

        public void Show(string message)
        {
            _viewModel.Title = "Пожалуйста, подождите";
            _viewModel.Subtitle = message;
            OnShow?.Invoke(_viewModel);
        }

        public void Show(string message, string title)
        {
            _viewModel.Subtitle = message;
            _viewModel.Title = title;
            OnShow?.Invoke(_viewModel);
        }

        #endregion
    }
}
