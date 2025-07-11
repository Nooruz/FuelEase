using KIT.GasStation.Domain.Models;
using System;

namespace KIT.GasStation.State.Users
{
    public interface IUserStore
    {
        event Action<User> OnLogin;
        event Action OnLogout;

        User? CurrentUser { get; set; }
    }
}
