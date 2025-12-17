using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Helpers;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.ViewModels.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KIT.GasStation.ViewModels
{
    public class EventPanelViewModel : BaseViewModel
    {
        #region Private Members

        private readonly IShiftStore _shiftStore;
        private readonly IEventPanelService _eventPanelService;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly ILogger<EventPanelViewModel> _logger;
        private ObservableCollection<EventPanel> _events = new();
        private EventPanel? _selectedEventPanel;

        #endregion

        #region Public Properties

        public ObservableCollection<EventPanel> Events
        {
            get => _events;
            set
            {
                _events = value;
                OnPropertyChanged(nameof(Events));
            }
        }
        public EventPanel? SelectedEventPanel
        {
            get => _selectedEventPanel;
            set
            {
                _selectedEventPanel = value;
                OnPropertyChanged(nameof(SelectedEventPanel));
            }
        }
        public List<KeyValuePair<EventPanelType, string>> EventPanelTypes => new(EnumHelper.GetLocalizedEnumValues<EventPanelType>());

        #endregion

        #region Constructor

        public EventPanelViewModel(ILogger<EventPanelViewModel> logger,
            IEventPanelService eventPanelService,
            IShiftStore shiftStore,
            IFuelSaleService fuelSaleService)
        {
            _logger = logger;
            _eventPanelService = eventPanelService;
            _shiftStore = shiftStore;
            _fuelSaleService = fuelSaleService;

            _eventPanelService.OnCreated += EventPanelService_OnCreated;
            _shiftStore.OnLogin += ShiftStore_OnLogin;
            _shiftStore.OnOpened += ShiftStore_OnOpened;
            _fuelSaleService.OnUpdated += FuelSaleService_OnUpdated;
        }

        #endregion

        #region Private Voids

        private void FuelSaleService_OnUpdated(FuelSale updatedFuelSale)
        {
            EventPanel? eventPanel = Events.FirstOrDefault(e => e.EventEntity == EventEntity.FuelSale && e.EntityId == updatedFuelSale.Id);
            if (eventPanel != null)
            {
                string message = string.Empty;

                if (updatedFuelSale.FuelSaleStatus != FuelSaleStatus.None)
                {
                    eventPanel.Message = $"{EnumHelper.GetEnumDisplayName(updatedFuelSale.OperationType)} статус: {EnumHelper.GetEnumDisplayName(updatedFuelSale.FuelSaleStatus)} {EnumHelper.GetEnumDisplayName(updatedFuelSale.PaymentType)} {updatedFuelSale.ReceivedQuantity:N2}/{updatedFuelSale.Quantity:N2} л. {updatedFuelSale.ReceivedSum:N2}/{updatedFuelSale.Sum:N2} сом";
                    _ = _eventPanelService.EnqueueUpdateAsync(eventPanel);
                }
            }
        }

        private void EventPanelService_OnCreated(EventPanel createdEventPanel)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (!Events.Any(e => e.Id == createdEventPanel.Id))
                    {
                        Events.Add(createdEventPanel);
                        SelectedEventPanel = createdEventPanel;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            });
        }

        private void ShiftStore_OnLogin(Shift shift)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (shift == null)
                    {
                        Events = new();
                    }
                    else
                    {
                        Events = new(await _eventPanelService.GetAllAsync(shift.Id));
                    }
                    if (Events != null && Events.Count > 0)
                    {
                        SelectedEventPanel = Events.LastOrDefault();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            });
        }

        private void ShiftStore_OnOpened(Shift shift)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    Events = new(await _eventPanelService.GetAllAsync(shift.Id));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            });
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            _eventPanelService.OnCreated -= EventPanelService_OnCreated;
            _shiftStore.OnLogin -= ShiftStore_OnLogin;
            _shiftStore.OnOpened -= ShiftStore_OnOpened;
            _fuelSaleService.OnUpdated -= FuelSaleService_OnUpdated;

            base.Dispose(disposing);
        }

        #endregion
    }
}
