using FuelEase.Domain.Models;
using FuelEase.Domain.Services.AuthenticationServices;
using System;
using System.Threading.Tasks;

namespace FuelEase.State.Authenticators
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
