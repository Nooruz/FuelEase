using KIT.GasStation.State.Navigators;
using KIT.GasStation.ViewModels.Factories;
using System;
using System.Windows.Input;

namespace KIT.GasStation.Commands
{
    public class UpdateCurrentViewModelCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly INavigator _navigator;
        private readonly IViewModelFactory _viewModelFactory;

        public UpdateCurrentViewModelCommand(INavigator navigator,
            IViewModelFactory viewModelFactory)
        {
            _navigator = navigator;
            _viewModelFactory = viewModelFactory;
        }

        public bool CanExecute(object parameter)
        {
            return false;
        }

        public async void Execute(object parameter)
        {
            if (parameter is ViewType viewType)
            {
                if (_navigator.CurrentViewModel?.ToString() ==
                    _viewModelFactory.CreateViewModelAsync(viewType)?.ToString())
                    return;
                _navigator.CurrentViewModel = await _viewModelFactory.CreateViewModelAsync(viewType);
            }
        }
    }
}
