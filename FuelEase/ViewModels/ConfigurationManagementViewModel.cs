using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using FuelEase.State.Navigators;
using FuelEase.ViewModels.Base;
using FuelEase.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FuelEase.ViewModels
{
    public class ConfigurationManagementViewModel : BaseViewModel
    {
        #region Private Members

        private readonly INavigator _navigation;
        private BaseViewModel? _currentViewModel;
        private ObservableCollection<BaseViewModel> _currentViewModels = new();

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
        public ObservableCollection<BaseViewModel> CurrentViewModels
        {
            get => _currentViewModels;
            set
            {
                _currentViewModels = value;
                OnPropertyChanged(nameof(CurrentViewModels));
            }
        }

        #endregion

        #region Constructor

        public ConfigurationManagementViewModel(INavigator navigator)
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
                if (args is string stringViewType)
                {
                    // Пробуем преобразовать строку в значение перечисления ViewType
                    if (Enum.TryParse(typeof(ViewType), stringViewType, out var parsedViewType) && parsedViewType is ViewType viewType)
                    {
                        // Получаем базовую ViewModel, соответствующую переданному типу представления
                        BaseViewModel baseViewModel = await GetBaseViewModel(viewType);

                        // Ищем уже существующую ViewModel того же типа в списке текущих ViewModel
                        BaseViewModel? existingViewModel = CurrentViewModels
                            .FirstOrDefault(vm => vm.GetType() == baseViewModel.GetType());

                        if (existingViewModel != null)
                        {
                            // Если ViewModel уже существует, устанавливаем её как текущую
                            CurrentViewModel = existingViewModel;
                        }
                        else
                        {
                            // Если ViewModel не существует, добавляем новую и делаем её текущей
                            CurrentViewModels.Add(baseViewModel);
                            CurrentViewModel = baseViewModel;
                        }
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

                BaseViewModel? existingViewModel = CurrentViewModels
                            .FirstOrDefault(vm => vm.GetType() == baseViewModel.GetType());
                if (existingViewModel != null)
                {
                    CurrentViewModels.Remove(existingViewModel);
                }
            }
            catch (Exception)
            {

            }
        }

        [Command]
        public void Settings()
        {
            WindowService.Title = "Настройки";
            WindowService.Show(nameof(SettingsView), new SettingsViewModel());
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
