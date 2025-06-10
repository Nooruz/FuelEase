using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.Helpers;
using FuelEase.State.Shifts;
using System;
using System.Threading.Tasks;

namespace FuelEase.State.Notifications
{
    public class NotificationStore : INotificationStore
    {
        #region Private Members

        private readonly IEventPanelService _eventPanelService;
        private readonly IShiftStore _shiftStore;

        #endregion

        #region Actions

        public event Action<string, string, NotificationType> OnShowing;

        #endregion

        #region Constructors

        public NotificationStore(IShiftService shiftService,
            IShiftStore shiftStore,
            IEventPanelService eventPanelService,
            IFuelSaleService fuelSaleService,
            IFuelService fuelService,
            INozzleService nozzleService,
            ITankService tankService,
            IUnregisteredSaleService unregisteredSaleService,
            IUserService userService,
            IDiscountService discountService)
        {
            _eventPanelService = eventPanelService;
            _shiftStore = shiftStore;

            // Подписка на события смены
            shiftService.OnCreated += ShiftService_OnCreated;
            shiftService.OnUpdated += ShiftService_OnUpdated;

            // Подписка на события продажи топлива
            fuelSaleService.OnCreated += FuelSaleService_OnCreated;
            fuelSaleService.OnDeleted += FuelSaleService_OnDeleted;

            // Подписка на события топлива
            fuelService.OnCreated += FuelService_OnCreated;
            fuelService.OnUpdated += FuelService_OnUpdated;
            fuelService.OnDeleted += FuelService_OnDeleted;

            // Подписка на события ТРК
            nozzleService.OnCreated += NozzleService_OnCreated;
            nozzleService.OnUpdated += NozzleService_OnUpdated;
            nozzleService.OnDeleted += NozzleService_OnDeleted;

            // Подписка на события резервуара
            tankService.OnCreated += TankService_OnCreated;
            tankService.OnUpdated += TankService_OnUpdated;
            tankService.OnDeleted += TankService_OnDeleted;

            // Подписка на события незарегистрированных отпусков
            unregisteredSaleService.OnCreated += UnregisteredSaleService_OnCreated;
            unregisteredSaleService.OnUpdated += UnregisteredSaleService_OnUpdated;
            unregisteredSaleService.OnDeleted += UnregisteredSaleService_OnDeleted;

            // Подписка на события пользовател
            userService.OnCreated += UserService_OnCreated;
            userService.OnUpdated += UserService_OnUpdated;
            userService.OnDeleted += UserService_OnDeleted;

            // Подписка на события скидки
            discountService.OnCreated += DiscountService_OnCreated;
            discountService.OnUpdated += DiscountService_OnUpdated;
            discountService.OnDeleted += DiscountService_OnDeleted;
        }

        #endregion

        #region Public Voids

        public void Show(string title, string message, NotificationType type)
        {
            OnShowing?.Invoke(title, message, type);
        }

        public void Show(string title, string message)
        {
            OnShowing?.Invoke(title, message, NotificationType.Information);
        }

        #endregion

        #region Shifts

        private void ShiftService_OnUpdated(Shift shift)
        {
            _ = Task.Run(async () =>
            {
                if (shift.ShiftState == ShiftState.Closed)
                {
                    _ = await _eventPanelService.CreateAsync(new EventPanel()
                    {
                        CreatedDate = DateTime.Now,
                        Message = $"Смена успешно закрыта.",
                        ShiftId = shift.Id,
                        Type = EventPanelType.Information,
                        EventEntity = EventEntity.Shift,
                        EntityId = shift.Id,
                    });
                }
            });
            Show("Смена закрыта", $"Смена успешно закрыта пользователем: {shift.User.FullName}.");
        }

        private void ShiftService_OnCreated(Shift shift)
        {
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"Смена успешно открыта.",
                    ShiftId = shift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Shift,
                    EntityId = shift.Id,
                });
            });
            Show("Смена открыта", $"Смена успешно открыта пользователем: {shift.User.FullName}.");
        }

        #endregion

        #region FuelSale

        private void FuelSaleService_OnDeleted(int deletedFuelSaleId)
        {
            _ = Task.Run(async () =>
            {
                await _eventPanelService.CreateAsync(new EventPanel
                {
                    CreatedDate = DateTime.Now,
                    Message = $"Продажа топлива удалена. Идентификатор: {deletedFuelSaleId}.",
                    ShiftId = _shiftStore.CurrentShift?.Id ?? 0,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.FuelSale,
                    EntityId = deletedFuelSaleId
                });
            });
            Show("Продажа удалена", $"Продажа топлива с идентификатором {deletedFuelSaleId} удалено.", NotificationType.Warning);
        }

        private void FuelSaleService_OnCreated(FuelSale createdFuelSale)
        {
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"Продажа: {EnumHelper.GetEnumDisplayName(createdFuelSale.PaymentType)} {createdFuelSale.ReceivedQuantity:N2}/{createdFuelSale.Quantity:N2} л. {createdFuelSale.ReceivedSum:N2}/{createdFuelSale.Sum:N2} сом",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.FuelSale,
                    EntityId = createdFuelSale.Id
                });
            });
            Show("Продажа топлива", $"Продажа топлива успешно выполнено.", NotificationType.Information);
        }

        #endregion

        #region Fuel

        private void FuelService_OnDeleted(int id)
        {
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{DisplayName.GetDisplayName<Fuel>()} успешно удалено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Fuel,
                    EntityId = id
                });
            });
            Show(DisplayName.GetDisplayName<Fuel>(), $"Успешно удалено.", NotificationType.Warning);
        }

        private void FuelService_OnUpdated(Fuel fuel)
        {
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{DisplayName.GetDisplayName<Fuel>()} успешно обновлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Fuel,
                    EntityId = fuel.Id
                });
            });
            Show(DisplayName.GetDisplayName<Fuel>(), $"Успешно обновлено.", NotificationType.Information);
        }

        private void FuelService_OnCreated(Fuel fuel)
        {
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{DisplayName.GetDisplayName<Fuel>()} успешно добавлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Fuel,
                    EntityId = fuel.Id
                });
            });
            Show(DisplayName.GetDisplayName<Fuel>(), $"Успешно добавлено.", NotificationType.Information);
        }

        #endregion

        #region Nozzle

        private void NozzleService_OnDeleted(int id)
        {
            string entityName = DisplayName.GetDisplayName<Nozzle>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно удалено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Nozzle,
                    EntityId = id
                });
            });
            Show(entityName, $"Успешно удалено.", NotificationType.Warning);
        }

        private void NozzleService_OnUpdated(Nozzle nozzle)
        {
            string entityName = DisplayName.GetDisplayName<Nozzle>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно обновлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Nozzle,
                    EntityId = nozzle.Id
                });
            });
            Show(entityName, $"Успешно обновлено.", NotificationType.Information);
        }

        private void NozzleService_OnCreated(Nozzle nozzle)
        {
            string entityName = DisplayName.GetDisplayName<Nozzle>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно добавлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Nozzle,
                    EntityId = nozzle.Id
                });
            });
            Show(entityName, $"Успешно добавлено.", NotificationType.Information);
        }

        #endregion

        #region Tank

        private void TankService_OnDeleted(int id)
        {
            string entityName = DisplayName.GetDisplayName<Tank>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно удалено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Tank,
                    EntityId = id
                });
            });
            Show(entityName, $"Успешно удалено.", NotificationType.Warning);
        }

        private void TankService_OnUpdated(Tank tank)
        {
            string entityName = DisplayName.GetDisplayName<Tank>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно обновлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Tank,
                    EntityId = tank.Id
                });
            });
            Show(entityName, $"Успешно обновлено.", NotificationType.Information);
        }

        private void TankService_OnCreated(Tank tank)
        {
            string entityName = DisplayName.GetDisplayName<Tank>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно добавлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Tank,
                    EntityId = tank.Id
                });
            });
            Show(entityName, $"Успешно добавлено.", NotificationType.Information);
        }

        #endregion

        #region UnregisteredSale

        private void UnregisteredSaleService_OnDeleted(int id)
        {
            string entityName = DisplayName.GetDisplayName<UnregisteredSale>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно удалено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.UnregisteredSale,
                    EntityId = id
                });
            });
            Show(entityName, $"Успешно удалено.", NotificationType.Warning);
        }

        private void UnregisteredSaleService_OnUpdated(UnregisteredSale unregisteredSale)
        {
            string entityName = DisplayName.GetDisplayName<UnregisteredSale>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно обновлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.UnregisteredSale,
                    EntityId = unregisteredSale.Id
                });
            });
            Show(entityName, $"Успешно обновлено.", NotificationType.Information);
        }

        private void UnregisteredSaleService_OnCreated(UnregisteredSale unregisteredSale)
        {
            string entityName = DisplayName.GetDisplayName<UnregisteredSale>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно добавлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.UnregisteredSale,
                    EntityId = unregisteredSale.Id
                });
            });
            Show(entityName, $"Успешно добавлено.", NotificationType.Information);
        }

        #endregion

        #region User

        private void UserService_OnDeleted(int id)
        {
            string entityName = DisplayName.GetDisplayName<User>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно удалено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.User,
                    EntityId = id
                });
            });
            Show(entityName, $"Успешно удалено.", NotificationType.Warning);
        }

        private void UserService_OnUpdated(User user)
        {
            string entityName = DisplayName.GetDisplayName<User>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно обновлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.User,
                    EntityId = user.Id
                });
            });
            Show(entityName, $"Успешно обновлено.", NotificationType.Information);
        }

        private void UserService_OnCreated(User user)
        {
            string entityName = DisplayName.GetDisplayName<User>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно добавлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.User,
                    EntityId = user.Id
                });
            });
            Show(entityName, $"Успешно добавлено.", NotificationType.Information);
        }

        #endregion

        #region Discount

        private void DiscountService_OnDeleted(int id)
        {
            string entityName = DisplayName.GetDisplayName<Discount>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно удалено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Discount,
                    EntityId = id
                });
            });
            Show(entityName, $"Успешно удалено.", NotificationType.Warning);
        }

        private void DiscountService_OnUpdated(Discount discount)
        {
            string entityName = DisplayName.GetDisplayName<Discount>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно обновлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Discount,
                    EntityId = discount.Id
                });
            });
            Show(entityName, $"Успешно обновлено.", NotificationType.Information);
        }

        private void DiscountService_OnCreated(Discount discount)
        {
            string entityName = DisplayName.GetDisplayName<Discount>();
            _ = Task.Run(async () =>
            {
                _ = await _eventPanelService.CreateAsync(new EventPanel()
                {
                    CreatedDate = DateTime.Now,
                    Message = $"{entityName} успешно добавлено.",
                    ShiftId = _shiftStore.CurrentShift.Id,
                    Type = EventPanelType.Information,
                    EventEntity = EventEntity.Discount,
                    EntityId = discount.Id
                });
            });
            Show(entityName, $"Успешно добавлено.", NotificationType.Information);
        }

        #endregion
    }
}
