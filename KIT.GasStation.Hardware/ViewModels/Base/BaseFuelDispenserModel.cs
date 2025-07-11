using KIT.GasStation.HardwareConfigurations.Models;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace KIT.GasStation.Hardware.ViewModels.Base
{
    public class BaseFuelDispenserModel : BaseViewModel
    {
        #region Private Members

        private ObservableCollection<string> _availablePorts = new(SerialPort.GetPortNames());
        private bool _allowCheckStatus = true;
        private Column _selectedColumn;
        private Controller _selectedController;

        #endregion

        #region Public Voids

        public ObservableCollection<string> AvailablePorts
        {
            get => _availablePorts;
            set
            {
                _availablePorts = value;
                OnPropertyChanged(nameof(AvailablePorts));
            }
        }
        public ObservableCollection<int> BaudRates => new() { 2400, 4800, 9550, 9600, 9650, 9700, 9750, 10500, 10600, 57600, 115200 };
        public bool AllowCheckStatus
        {
            get => _allowCheckStatus;
            set
            {
                _allowCheckStatus = value;
                OnPropertyChanged(nameof(AllowCheckStatus));
            }
        }
        public Column SelectedColumn
        {
            get => _selectedColumn;
            set
            {
                _selectedColumn = value;
                OnPropertyChanged(nameof(SelectedColumn));
            }
        }
        public Controller SelectedController
        {
            get => _selectedController;
            set
            {
                _selectedController = value;
                OnPropertyChanged(nameof(SelectedController));
            }
        }

        #endregion

        #region Private Voids

        public bool CanCheckStatus()
        {
            return AllowCheckStatus;
        }

        #endregion
    }
}
