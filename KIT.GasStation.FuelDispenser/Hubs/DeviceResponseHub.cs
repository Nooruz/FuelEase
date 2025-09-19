using KIT.GasStation.FuelDispenser.Models;
using Microsoft.AspNetCore.SignalR;

namespace KIT.GasStation.FuelDispenser.Hubs
{
    public sealed class DeviceResponseHub : Hub<IDeviceResponseClient>
    {
        // Группа = "<controllerId>[:<address>]"
        public static string Group(string fuelDispenseName, int? address = null) =>
            address is null ? fuelDispenseName : $"{fuelDispenseName}:{address}";

        public Task JoinController(string fuelDispenseName, int? address = null)
            => Groups.AddToGroupAsync(Context.ConnectionId, Group(fuelDispenseName, address));

        public Task LeaveController(string fuelDispenseName, int? address = null)
            => Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(fuelDispenseName, address));

        // Пример команд от РМК к серверу:
        public Task SetPrice() =>
            // здесь пробрасываешь в свой командный обработчик/шину
            Task.CompletedTask;

        public Task StartFilling() => Task.CompletedTask;
        public Task StopFilling() => Task.CompletedTask;

        public sealed record StatusChangedEvent(DeviceResponse response);
    }
}
