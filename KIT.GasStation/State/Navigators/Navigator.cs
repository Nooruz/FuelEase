using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KIT.GasStation.State.Navigators
{
    public class Navigator : INavigator
    {
        private readonly IViewModelFactory _viewModelFactory;

        // Кэш для заранее загруженных ViewModel
        private readonly Dictionary<ViewType, BaseViewModel> _preloadedViewModels = new();

        private BaseViewModel _currentViewModel;
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel?.Dispose(); // Освобождаем ресурсы старой ViewModel
                _currentViewModel = value;    // Обновляем текущую ViewModel сильной ссылкой
                StateChanged?.Invoke();       // Уведомляем об изменении состояния
            }
        }

        public Navigator(IViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public event Action StateChanged;
        public event Action OnDispose;

        /// <summary>
        /// Предварительная загрузка определенных ViewModel.
        /// </summary>
        /// <param name="viewTypes">Список типов ViewModel, которые нужно предварительно загрузить.</param>
        public async Task PreloadViewModelsAsync(IEnumerable<ViewType> viewTypes)
        {
            foreach (var viewType in viewTypes)
            {
                if (!_preloadedViewModels.ContainsKey(viewType))
                {
                    // Загружаем ViewModel и сохраняем её в кэш
                    var viewModel = await _viewModelFactory.CreateViewModelAsync(viewType);
                    _preloadedViewModels[viewType] = viewModel;
                }
            }
        }

        /// <summary>
        /// Получить ViewModel из кэша или создать новую, если её нет.
        /// </summary>
        /// <param name="viewType">Тип ViewModel.</param>
        /// <returns>Загруженная ViewModel.</returns>
        public BaseViewModel GetViewModel(ViewType viewType)
        {
            if (_preloadedViewModels.TryGetValue(viewType, out var preloadedViewModel))
            {
                // Возвращаем предварительно загруженную ViewModel
                return preloadedViewModel;
            }

            // Создаем новую ViewModel, если она не была предварительно загружена
            return _viewModelFactory.CreateViewModel(viewType);
        }

        /// <summary>
        /// Получить предварительно загруженную ViewModel из кэша или создать новую.
        /// </summary>
        /// <param name="viewType">Тип ViewModel.</param>
        /// <returns>Загруженная ViewModel.</returns>
        public async Task<BaseViewModel> GetViewModelAsync(ViewType viewType)
        {
            if (_preloadedViewModels.TryGetValue(viewType, out var preloadedViewModel))
            {
                // Если ViewModel предварительно загружена, запустим асинхронную инициализацию, если требуется
                if (preloadedViewModel is IAsyncInitializable asyncInitializable)
                {
                    await asyncInitializable.StartAsync();
                }
                // Возвращаем предварительно загруженную ViewModel
                return preloadedViewModel;
            }

            var viewModel = await _viewModelFactory.CreateViewModelAsync(viewType);

            if (viewModel is IAsyncInitializable asyncInitializableViewModel)
            {
                await asyncInitializableViewModel.StartAsync();
            }

            // Создаем новую ViewModel, если она не была предварительно загружена
            return viewModel;
        }

        /// <summary>
        /// Перенаправление на указанную ViewModel.
        /// </summary>
        /// <param name="viewType">Тип ViewModel.</param>
        public async Task Renavigate(ViewType viewType)
        {
            // Получаем ViewModel (либо из кэша, либо создаем новую)
            var newViewModel = await GetViewModelAsync(viewType);
            CurrentViewModel = newViewModel;
        }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}
