using Microsoft.Extensions.DependencyInjection;

namespace KIT.GasStation.HardwareSettings.Services
{
    public sealed class DialogService : IDialogService
    {
        private readonly IServiceProvider _sp;

        public DialogService(IServiceProvider sp) => _sp = sp;

        public DialogResult Show<TDialog>() where TDialog : Form
        {
            using var dlg = _sp.GetRequiredService<TDialog>();
            return dlg.ShowDialog();
        }

        public DialogResult Show<TDialog>(IWin32Window owner) where TDialog : Form
        {
            using var dlg = _sp.GetRequiredService<TDialog>();
            return dlg.ShowDialog(owner);
        }

        public DialogResult Show<TDialog, TModel>(TModel model) where TDialog : Form
        {
            // ВАЖНО: передаём model как runtime-аргумент, остальные зависимости DI докинет сам
            using var dlg = ActivatorUtilities.CreateInstance<TDialog>(_sp, model);
            return dlg.ShowDialog();
        }
    }
}
