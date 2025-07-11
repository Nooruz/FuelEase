using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services.AuthenticationServices;
using System;
using System.Threading.Tasks;

namespace KIT.GasStation.State.Authenticators
{
    public interface IAuthenticator
    {
        User CurrentUser { get; }
        
        event Action StateChanged;

        Task<RegistrationResult> Register(User user, string password, string confirmPassword);
        Task Login(string username, string? password);
        Task<RegistrationResult> Update(User user, string password, string confirmPassword);
        void Logout();
    }
}
