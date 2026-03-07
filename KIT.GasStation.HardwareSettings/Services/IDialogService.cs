namespace KIT.GasStation.HardwareSettings.Services
{
    public interface IDialogService
    {
        DialogResult Show<TDialog>() where TDialog : Form;
        DialogResult Show<TDialog>(IWin32Window owner) where TDialog : Form;

        DialogResult Show<TDialog, TModel>(TModel model) where TDialog : Form;
    }
}
