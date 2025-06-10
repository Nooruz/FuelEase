using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.ViewModels.Base;
using FuelEase.ViewModels.Details;
using FuelEase.Views.Details;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FuelEase.ViewModels
{
    public class UserViewModel : BaseViewModel
    {
        #region Private Members

        private readonly ILogger<UserViewModel> _logger;
        private readonly ILogger<UserDetailViewModel> _userDetailViewModelLogger;
        private readonly IUserService _userService;
        private readonly IDataService<UserRole> _userRoleService;
        private ObservableCollection<User> _users = new();
        public User _selectedUser;

        #endregion

        #region Public Properties

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged(nameof(Users));
            }
        }
        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
            }
        }

        #endregion

        #region Constructor

        public UserViewModel(ILogger<UserViewModel> logger,
            IUserService userService,
            IDataService<UserRole> userRoleService,
            ILogger<UserDetailViewModel> userDetailViewModelLogger)
        {
            _logger = logger;
            _userService = userService;
            _userRoleService = userRoleService;
            _userDetailViewModelLogger = userDetailViewModelLogger;

            _ = Task.Run(GetData);

            Title = "Пользователи";

            _userService.OnCreated += UserService_OnCreated;
            _userService.OnUpdated += UserService_OnUpdated;
            _userService.OnDeleted += UserService_OnDeleted;
        }

        #endregion

        #region Public Voids

        [Command]
        public void CreateUser()
        {
            try
            {
                WindowService.Title = "Создание пользователя";
                WindowService.Show(nameof(UserDetailView), new UserDetailViewModel(_userDetailViewModelLogger, _userRoleService, _userService));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        [Command]
        public void EditUser()
        {
            try
            {
                if (SelectedUser != null)
                {
                    WindowService.Title = $"Редактирование пользователя ({SelectedUser.FullName})";
                    WindowService.Show(nameof(UserDetailView), new UserDetailViewModel(_userDetailViewModelLogger, _userRoleService, _userService)
                    {
                        CreatedUser = new User
                        {
                            Id = SelectedUser.Id,
                            FullName = SelectedUser.FullName,
                            UserRoleId = SelectedUser.UserRoleId,
                            Password = SelectedUser.Password,
                            Deleted = SelectedUser.Deleted
                        }
                    });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        [Command]
        public async Task DeleteUser()
        {
            try
            {
                if (MessageBoxService.ShowMessage($"Удалить выбранного пользователя? ({SelectedUser.FullName})", "Удаление", MessageButton.YesNo) == MessageResult.Yes)
                {
                    _ = await _userService.DeleteAsync(SelectedUser.Id);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        #endregion

        #region Private Voids

        private async Task GetData()
        {
            Users = new(await _userService.GetAllAsync());
        }

        private void UserService_OnUpdated(User updatedUser)
        {
            User? user = Users.FirstOrDefault(u => u.Id == updatedUser.Id);
            if (user != null)
            {
                user.FullName = updatedUser.FullName;
                user.UserRoleId = updatedUser.UserRoleId;
                user.Password = updatedUser.Password;
                user.UserRoleId = updatedUser.UserRoleId;
            }
        }

        private void UserService_OnCreated(User user)
        {
            Users.Add(user);
        }

        private void UserService_OnDeleted(int id)
        {
            try
            {
                User? user = Users.FirstOrDefault(u => u.Id == id);
                if (user != null)
                {
                    _ = Users.Remove(user);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _userService.OnCreated -= UserService_OnCreated;
                _userService.OnUpdated -= UserService_OnUpdated;
                _userService.OnDeleted -= UserService_OnDeleted;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}