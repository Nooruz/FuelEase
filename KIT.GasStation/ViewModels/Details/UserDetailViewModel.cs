using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.Details
{
    public class UserDetailViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly ILogger<UserDetailViewModel> _logger;
        private readonly IDataService<UserRole> _userRoleService;
        private readonly IUserService _userService;
        private ObservableCollection<UserRole> _userRoles = new();
        private User _createdUser = new();
        private string? _confirmedPassword;
        #endregion

        #region Public Properties

        public ObservableCollection<UserRole> UserRoles
        {
            get => _userRoles;
            set
            {
                _userRoles = value;
                OnPropertyChanged(nameof(UserRoles));
            }
        }
        public User CreatedUser
        {
            get => _createdUser;
            set
            {
                _createdUser = value;
                OnPropertyChanged(nameof(CreatedUser));
            }
        }
        public string? ConfirmedPassword
        {
            get => _confirmedPassword;
            set
            {
                _confirmedPassword = value;
                OnPropertyChanged(nameof(ConfirmedPassword));
            }
        }

        #endregion

        #region Constructor

        public UserDetailViewModel(ILogger<UserDetailViewModel> logger,
            IDataService<UserRole> userRoleService,
            IUserService userService)
        {
            _logger = logger;
            _userRoleService = userRoleService;
            _userService = userService;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task Create()
        {
            if (string.IsNullOrEmpty(CreatedUser.FullName))
            {
                _ = MessageBoxService.ShowMessage("Введите ФИО!", "Ошибка", MessageButton.OK, MessageIcon.Exclamation);
                return;
            }

            if (CreatedUser.UserRoleId == 0)
            {
                _ = MessageBoxService.ShowMessage("Выберите роль!", "Ошибка", MessageButton.OK, MessageIcon.Exclamation);
                return;
            }

            if (!string.IsNullOrEmpty(CreatedUser.Password))
            {
                if (CreatedUser.Password != ConfirmedPassword)
                {
                    _ = MessageBoxService.ShowMessage("Пароли не совпадают!", "Ошибка", MessageButton.OK, MessageIcon.Exclamation);
                    return;
                }
            }

            if (CreatedUser.Id == 0)
            {
                _ = await _userService.CreateAsync(CreatedUser);
            }
            else
            {
                _ = await _userService.UpdateAsync(CreatedUser.Id, CreatedUser);
            }

            CurrentWindowService.Close();
        }

        public async Task StartAsync()
        {
            UserRoles = new(await _userRoleService.GetAllAsync());
        }

        #endregion
    }
}
