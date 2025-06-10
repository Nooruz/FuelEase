using FuelEase.Domain.Models;
using System;

namespace FuelEase.State.Users
{
    public interface IUserStore
    {
        event Action<User> OnLogin;
        event Action OnLogout;

        User? CurrentUser { get; set; }
    }
}
