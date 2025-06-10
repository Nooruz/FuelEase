using FuelEase.Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace FuelEase.Web.Hubs
{
    public class EventPanelHub : Hub
    {
        public async Task SendEventPanel(string message)
        {
            await Clients.All.SendAsync("ReceiveEventPanel", message);
        }
    }
}
