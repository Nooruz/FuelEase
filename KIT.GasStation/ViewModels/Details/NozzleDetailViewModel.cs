using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.Details
{
    public class NozzleDetailViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly INozzleService _nozzleService;
        private readonly ITankService _tankService;
        //private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private Nozzle _createdNozzle = new();
        private ObservableCollection<Tank> _tanks = new();
        //private ObservableCollection<Column> _columns = new();

        #endregion

        #region Public Properties

        public Nozzle CreatedNozzle
        {
            get => _createdNozzle;
            set
            {
                _createdNozzle = value;
                OnPropertyChanged(nameof(CreatedNozzle));
            }

        }
        public ObservableCollection<Tank> Tanks
        {
            get => _tanks;
            set
            {
                _tanks = value;
                OnPropertyChanged(nameof(Tanks));
            }
        }
        //public ObservableCollection<Column> Columns
        //{
        //    get => _columns;
        //    set
        //    {
        //        _columns = value;
        //        OnPropertyChanged(nameof(Columns));
        //    }
        //}

        #endregion

        #region Constructors

        public NozzleDetailViewModel(INozzleService nozzleService,
            ITankService tankService)
        {
            _nozzleService = nozzleService;
            _tankService = tankService;
            //_hardwareConfigurationService = hardwareConfigurationService;
        }

        #endregion

        #region Public Commands

        [Command]
        public async Task Save()
        {
            if (CheckCreatedNozzle())
            {
                if (CreatedNozzle.Id == 0)
                {
                    await _nozzleService.CreateAsync(CreatedNozzle);
                }
                else
                {
                    await _nozzleService.UpdateAsync(CreatedNozzle.Id, CreatedNozzle);
                }

                CurrentWindowService?.Close();
            }
        }

        public async Task StartAsync()
        {
            await LoeadData();
        }

        #endregion

        #region Private Voids

        private bool CheckCreatedNozzle()
        {
            if (string.IsNullOrEmpty(CreatedNozzle.Name))
            {
                MessageBoxService.ShowMessage("Введите наименование ТРК", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return false;
            }

            if (CreatedNozzle.Tube == 0)
            {
                MessageBoxService.ShowMessage("Введите номер шланга", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return false;
            }

            if (CreatedNozzle.Side == 0)
            {
                MessageBoxService.ShowMessage("Введите сторону", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return false;
            }

            if (CreatedNozzle.TankId == 0)
            {
                MessageBoxService.ShowMessage("Выберите резервуар", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return false;
            }

            return true;
        }

        private async Task LoeadData()
        {
            Tanks = new(await _tankService.GetAllAsync());
            //Columns = await _hardwareConfigurationService.GetColumnsAsync();
        }

        #endregion
    }
}

