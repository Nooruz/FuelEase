using Microsoft.Extensions.DependencyInjection;

namespace KIT.GasStation.HardwareSettings.CustomControl.Factories
{
    public sealed class PageFactory : IPageFactory
    {
        private readonly IServiceProvider _sp;

        public PageFactory(IServiceProvider sp) => _sp = sp;

        public IPage Create(PageType type)
            => _sp.GetRequiredKeyedService<IPage>(type);
    }
}
