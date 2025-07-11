using KIT.GasStation.Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace KIT.GasStation.Web.Hubs
{
    public class EventPanelHub : Hub
    {
        public async Task SendEventPanel(string message)
        {
            await Clients.All.SendAsync("ReceiveEventPanel", message);
        }
    }
}
