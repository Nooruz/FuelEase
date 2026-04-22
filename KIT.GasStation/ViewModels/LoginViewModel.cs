using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Exceptions;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.State.Authenticators;
using KIT.GasStation.ViewModels.Base;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace KIT.GasStation.ViewModels
{
    public class LoginViewModel : BaseViewModel, IHostedService
    {
        #region Private Members

        private readonly ILogger<LoginViewModel> _logger;
        private readonly IAuthenticator _authenticator;
        private readonly IUserService _userService;
        private readonly IShiftService _shiftService;
        private bool _rememberMe = true;
        private ObservableCollection<User> _users = new();
        private User _selectedUser;

        #endregion

        #region Public Properties

        public bool CanLogin => SelectedUser != null;
        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                _rememberMe = value;
                OnPropertyChanged(nameof(RememberMe));
            }
        }
        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
                OnPropertyChanged(nameof(CanLogin));
            }
        }
        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged(nameof(Users));
                SetDefaultUser();
            }
        }

        #endregion

        #region Constructor

        public LoginViewModel(ILogger<LoginViewModel> logger, 
            IAuthenticator authenticator,
            IUserService userService,
            IShiftService shiftService)
        {
            _logger = logger;
            _authenticator = authenticator;
            _userService = userService;
            _shiftService = shiftService;

            _userService.OnCreated += UserService_OnCreated;
            _userService.OnDeleted += UserService_OnDeleted;
            _userService.OnUpdated += UserService_OnUpdated;

            //_userStore.OnLogin += async () => await UserStore_OnLogin();

        }

        #endregion

        #region Public Voids

        [Command]
        public async Task Login()
        {
            try
            {
                if (await CheckShift())
                {
                    //await _navigator.PreloadViewModelsAsync(new[] { ViewType.Main, ViewType.EventPanelView });

#pragma warning disable CS0618 // Password используется как поле ввода пользователя до рефакторинга UI
                    await _authenticator.Login(SelectedUser.FullName, SelectedUser.Password);

                    if (RememberMe)
                    {
                        Properties.Settings.Default.DefaultUserName = SelectedUser.FullName;
                        Properties.Settings.Default.DefaultUserPassword = SelectedUser.Password;
#pragma warning restore CS0618
                        Properties.Settings.Default.Save();
                    }
                }
            }
            catch (InvalidUsernameOrPasswordException ex)
            {
                _logger.LogWarning(ex, ex.Message);
                _ = MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ex.Message);
                MessageBox.Show("Ошибка входа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                //_splashScreenService.Close();
            }
        }

        #endregion

        #region Private Voids

        private void UserService_OnUpdated(User updatedUser)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    int index = Users.ToList().FindIndex(u => u.Id == updatedUser.Id);
                    if (index >= 0)
                        Users[index] = updatedUser;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            });
        }

        private void UserService_OnDeleted(int id)
        {
            Application.Current.Dispatcher.Invoke(() =>
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
            });
        }

        private void UserService_OnCreated(User createdUser)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (!Users.Any(u => u.Id == createdUser.Id))
                    {
                        Users.Add(createdUser);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            });
        }

        /// <summary>
        /// Получение данных
        /// </summary>
        private async Task GetData()
        {
            try
            {
                Users = new(await _userService.GetAllAsync());
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Проверка на открытую смену
        /// </summary>
        private async Task<bool> CheckShift()
        {
            try
            {
                Shift? shift = await _shiftService.GetOpenShiftAsync();
                if (shift == null)
                {
                    return true;
                }
                else
                {
                    if (shift.UserId == SelectedUser.Id)
                    {
                        return true;
                    }
                    else
                    {
                        _ = MessageBoxService.ShowMessage($"Смена уже открыта пользователем: {shift.User.FullName}.\n" +
                            $"Сначала необходимо закрыть эту смену, прежде чем войти в систему с другим пользователем.", "Предупреждение", MessageButton.OK, MessageIcon.Warning);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return false;
            }
        }

        /// <summary>
        /// Установка пользователя по умолчанию
        /// </summary>
        private void SetDefaultUser()
        {
            // Если пользователи есть
            if (Users != null && Users.Any())
            {
                string defaultUserName = Properties.Settings.Default.DefaultUserName;
                string defaultPassword = Properties.Settings.Default.DefaultUserPassword;
                if (string.IsNullOrEmpty(defaultUserName))
                {
                    SetFirstUser();
                }
                else
                {
                    User? user = Users.FirstOrDefault(u => u.FullName == defaultUserName);
                    if (user == null)
                    {
                        SetFirstUser();
                    }
                    else
                    {
                        SelectedUser = user;
                    }
                }
            }
        }

        /// <summary>
        /// Установка первого пользователя
        /// </summary>
        private void SetFirstUser()
        {
            if (Users != null && Users.Any())
            {
                SelectedUser = Users.First();
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            _userService.OnCreated -= UserService_OnCreated;
            _userService.OnDeleted -= UserService_OnDeleted;
            _userService.OnUpdated -= UserService_OnUpdated;
            base.Dispose(disposing);
        }

        #endregion

        #region HostedService

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await GetData();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            
        }

        #endregion
    }
}
