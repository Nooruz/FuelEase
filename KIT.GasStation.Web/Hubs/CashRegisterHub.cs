using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR;

namespace KIT.GasStation.Web.Hubs
{
    public class CashRegisterHub : Hub<ICashRegisterHub>
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly ILogger<CashRegisterHub> _logger;

        #endregion

        #region Constructors

        public CashRegisterHub(IHardwareConfigurationService hardwareConfigurationService,
            ILogger<CashRegisterHub> logger)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            _logger = logger;
        }

        #endregion

        #region Public Voids

        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("CashRegister connected: {ConnectionId}", connectionId);
            // Optionally, you can add logic here to register the cash register
            // in a group or perform other initialization tasks.
            await base.OnConnectedAsync();
        }

        #endregion
    }
}