using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.Details
{
    public class NozzleDetailViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly INozzleService _nozzleService;
        private readonly ITankService _tankService;
        private Nozzle _createdNozzle = new();
        private ObservableCollection<Tank> _tanks = new();
        private readonly IHubClient _hubClient;

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
        public string[] Hubs { get; set; }

        #endregion

        #region Constructors

        public NozzleDetailViewModel(INozzleService nozzleService,
            ITankService tankService,
            IHubClient hubClient)
        {
            _nozzleService = nozzleService;
            _tankService = tankService;
            _hubClient = hubClient;
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
            var hub = _hubClient.Connection;
            await _hubClient.EnsureStartedAsync();

            Hubs = await hub.InvokeAsync<string[]>("GetAllGroups");


            await LoadData();
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

        private async Task LoadData()
        {
            Tanks = new(await _tankService.GetAllAsync());
            //Columns = await _hardwareConfigurationService.GetColumnsAsync();
        }

        #endregion
    }
}

