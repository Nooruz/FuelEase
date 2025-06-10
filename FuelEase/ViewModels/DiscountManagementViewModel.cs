using DevExpress.Mvvm.DataAnnotations;
using FuelEase.State.Navigators;
using FuelEase.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FuelEase.ViewModels
{
    public class DiscountManagementViewModel : BaseViewModel
    {
        #region Private Members

        private readonly INavigator _navigation;
        private BaseViewModel? _currentViewModel;
        // Используем Dictionary для быстрого доступа к ViewModel по типу
        private readonly Dictionary<Type, BaseViewModel> _currentViewModels = new();

        #endregion

        #region Public Properties

        public BaseViewModel? CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }
        // Свойство для отображения ViewModels в UI
        public ObservableCollection<BaseViewModel> CurrentViewModels => new(_currentViewModels.Values);

        #endregion

        #region Constructor

        public DiscountManagementViewModel(INavigator navigator)
        {
            _navigation = navigator;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task ChangeCurrentViewModel(object args)
        {
            try
            {
                // Проверяем, что аргумент является строкой и приводим его к string
                if (args is string stringViewType &&
                    Enum.TryParse(typeof(ViewType), stringViewType, out var parsedViewType) &&
                    parsedViewType is ViewType viewType)
                {

                    // Получаем тип ViewModel для данного ViewType
                    var baseViewModelType = _navigation.GetViewModelAsync(viewType).GetType();

                    // Проверяем, существует ли ViewModel в Dictionary
                    if (_currentViewModels.TryGetValue(baseViewModelType, out var existingViewModel))
                    {
                        // Если ViewModel уже существует, делаем её текущей
                        CurrentViewModel = existingViewModel;
                    }
                    else
                    {
                        // Создаём новую ViewModel, если её нет
                        var newViewModel = await GetBaseViewModel(viewType);
                        _currentViewModels[baseViewModelType] = newViewModel;
                        CurrentViewModel = newViewModel;

                        // Обновляем UI, чтобы новая ViewModel появилась
                        OnPropertyChanged(nameof(CurrentViewModels));
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        [Command]
        public void CloseTab(BaseViewModel baseViewModel)
        {
            try
            {
                baseViewModel.Dispose();

                var baseViewModelType = baseViewModel.GetType();

                // Удаляем ViewModel из Dictionary, если она существует
                if (_currentViewModels.ContainsKey(baseViewModelType))
                {
                    _currentViewModels.Remove(baseViewModelType);

                    // Обновляем текущую ViewModel, если удалена активная
                    if (CurrentViewModel == baseViewModel)
                    {
                        CurrentViewModel = _currentViewModels.Values.FirstOrDefault();
                    }

                    // Обновляем UI
                    OnPropertyChanged(nameof(CurrentViewModels));
                }
            }
            catch (Exception)
            {

            }
        }

        #endregion

        #region Private Voids

        private async Task<BaseViewModel> GetBaseViewModel(ViewType viewType)
        {
            return await _navigation.GetViewModelAsync(viewType);
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var viewModel in CurrentViewModels)
                {
                    viewModel.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
