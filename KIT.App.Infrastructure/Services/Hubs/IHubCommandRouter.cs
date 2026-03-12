namespace KIT.App.Infrastructure.Services.Hubs
{
    public interface IHubCommandRouter : IAsyncDisposable
    {
        void RegisterHandlers();
    }
}
