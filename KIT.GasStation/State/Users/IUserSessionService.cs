using KIT.GasStation.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace KIT.GasStation.State.Users
{
    public interface IUserSessionService
    {
        Task OnLoginAsync(User user, CancellationToken ct);
        Task OnLogoutAsync(CancellationToken ct);
    }
}
