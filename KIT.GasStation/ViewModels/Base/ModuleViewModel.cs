using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using KIT.GasStation.ViewModels.Factories;
using System;
using System.Threading.Tasks;
using System.Windows.Media;

namespace KIT.GasStation.ViewModels.Base
{
    public class ModuleViewModel : BaseViewModel
    {
        #region Private Members

        private ImageSource _icon;
        private bool _isSelected;
        private string _type;
        private readonly ISupportServices _supportServices;

        #endregion

        #region Public Properties

        public ImageSource Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged(nameof(Icon));
            }
        }
        public virtual bool IsSelected { get; set; }
        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        #endregion

        #region Constructor

        public ModuleViewModel(string type, object parent, string title)
        {
            Type = type;
            Title = title;
            _supportServices = (ISupportServices)parent;
        }

        #endregion

        #region Public Voids

        public ModuleViewModel SetIcon(string icon)
        {
            var extension = new SvgImageSourceExtension()
            {
                Uri = new Uri(string.Format(@"pack://application:,,,/KIT.GasStation;component/Images/{0}.svg", icon), UriKind.RelativeOrAbsolute)
            };
            Icon = (ImageSource)extension.ProvideValue(null);
            return this;
        }

        public void Show(object parameter = null, ModuleViewModel viewModel = null)
        {
            INavigationService navigationService = _supportServices.ServiceContainer.GetRequiredService<INavigationService>();
            navigationService.Navigate(Type, viewModel, parameter, _supportServices, true);
        }

        #endregion
    }
}
