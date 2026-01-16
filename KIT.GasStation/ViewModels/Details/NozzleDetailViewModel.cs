using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private IEnumerable<Nozzle> _nozzles;

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
            string[] hubs,
            IEnumerable<Nozzle> nozzles)
        {
            _nozzleService = nozzleService;
            _tankService = tankService;
            Hubs = hubs;
            _nozzles = nozzles;
        }

        #endregion

        #region Public Commands

        [Command]
        public async Task Save()
        {
            if (await CheckCreatedNozzle())
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
            await LoadData();

            int nozzelCount = 0;

            if (_nozzles == null) return;

            if (CreatedNozzle.Id == 0)
            {
                nozzelCount = _nozzles.Count() + 1;

                CreatedNozzle.Name = $"ТРК {nozzelCount}";
                CreatedNozzle.Tube = nozzelCount;
            }
        }

        #endregion

        #region Private Voids

        private async Task<bool> CheckCreatedNozzle()
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

            if (await _nozzleService.IsTubeAvailableAsync(CreatedNozzle.Id, CreatedNozzle.Tube))
            {
                MessageBoxService.ShowMessage("Данный номер шланга уже занят", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return false;
            }

            return true;
        }

        private async Task LoadData()
        {
            Tanks = new(await _tankService.GetAllAsync());
        }

        #endregion
    }
}

