using DevExpress.Mvvm;
using DevExpress.Mvvm.POCO;
using DevExpress.Xpf.Core;
using KIT.App.Infrastructure.HostBuilders;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.EntityFramework;
using KIT.GasStation.HostBuilders;
using KIT.GasStation.Services;
using KIT.GasStation.SplashScreen;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels;
using KIT.GasStation.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace KIT.GasStation
{
    public partial class App : Application
    {
        #region Private Members

        private readonly IHost _host;
        private readonly DXSplashScreenViewModel _splashScreenViewModel;
        private Window _window;
        private ThemedWindow _loginWindow;
        private Mutex _instanceMutex;
        private bool _ownsMutex;
        private NotifyIcon _notifyIcon;


        #endregion

        #region Static Properties

        /// <summary>
        /// Возвращает название компании из атрибута AssemblyCompany сборки.
        /// </summary>
        public static string CompanyName
        {
            get
            {
                // Пример получения из атрибута AssemblyCompany
                var attribute = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyCompanyAttribute>();
                return attribute?.Company ?? "ОсОО \"КИТ\"";
            }
        }
        public static string ProductName
        {
            get
            {
                // Пример получения из атрибута AssemblyCompany
                var attribute = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyProductAttribute>();
                return attribute?.Product ?? "КИТ-АЗС";
            }
        }

        #endregion

        #region Constructor

        public App()
        {
            try
            {
                // Отображение заставки
                _splashScreenViewModel = new DXSplashScreenViewModel()
                {
                    Title = ProductName,
                    Status = "Инициализация приложения...",
                    Copyright = $"Авторское право © 2026 {CompanyName} \n Все права защищены."
                };
                SplashScreenManager.Create(() => new ApplicationSplashScreen(), _splashScreenViewModel).Show();

                _splashScreenViewModel.Status = "Создание хоста и регистрация сервисов...";
                // Создание хоста (без его старта)
                _host = CreateHostBuilder().Build();
            }
            catch (Exception e)
            {
#if DEBUG
                MessageBox.Show($"Ошибка: {e.Message}\nИсточник: {e.Source}\nТрассировка: {e.StackTrace}");
#else
                    MessageBox.Show($"Ошибка при запуске.");
#endif

                throw;
            }

            // Регистрация глобальных обработчиков исключений
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                LogUnhandledException(e.ExceptionObject as Exception, "AppDomain");
            };

            DispatcherUnhandledException += (sender, e) =>
            {
                LogUnhandledException(e.Exception, "Dispatcher");
                e.Handled = true;
            };
        }

        #endregion

        private void LogUnhandledException(Exception ex, string source)
        {
            if (ex != null)
            {
                var message = $"Необработанное исключение в {source}: {ex.Message}\nИсточник: {ex.Source}\nТрассировка: {ex.StackTrace}";

                // Логирование
                Log.Error(ex, message);

#if DEBUG
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
#else
        MessageBox.Show("Произошла критическая ошибка. Пожалуйста, свяжитесь с поддержкой.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
#endif
            }
        }

        private IHostBuilder CreateHostBuilder(string[] args = null)
        {
            var host = Host.CreateDefaultBuilder(args);

            _splashScreenViewModel.Status = "Загрузка конфигурации...";
            host.AddLocalConfiguration();

            _splashScreenViewModel.Status = "Добавление контекста базы данных...";
            host.AddDbContext();

            _splashScreenViewModel.Status = "Регистрация дополнительных сервисов...";
            host.AddServices();
            host.AddCashRegisters();

            _splashScreenViewModel.Status = "Регистрация хранилищ...";
            host.AddStores();

            _splashScreenViewModel.Status = "Настройки подключения к серверу...";
            host.AddHubs();

            _splashScreenViewModel.Status = "Регистрация моделей представления...";
            host.AddViewModels();

            return host;
        }

        private void ConfigureLogging(IConfiguration configuration)
        {
            _splashScreenViewModel.Status = "Настройка логирования и применение миграций...";

            using var scope = _host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GasStationDbContext>();

            try
            {
                // Применение миграций и создание базы данных
                dbContext.Database.Migrate();
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                MessageBox.Show($"Ошибка при применении миграций: {sqlEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                PromptForConnectionString(configuration);
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                // Ловим исключение, обернутое в DbUpdateException
                // Обработка sqlEx
            }



            // Добавление логирования после проверки базы данных
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/wpf-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();


        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                _instanceMutex = new Mutex(true, "Global\\КИТ-АЗС", out _ownsMutex);
                if (!_ownsMutex)
                {
                    SplashScreenManager.CloseAll();
                    MessageBox.Show("Приложение уже запущено.", "КИТ-АЗС", MessageBoxButton.OK, MessageBoxImage.Information);
                    Shutdown();
                    return;
                }

                _notifyIcon = new NotifyIcon
                {
                    Icon = new System.Drawing.Icon("logo.ico"),
                    Visible = true,
                    Text = "КИТ-АЗС"
                };

                var menu = new System.Windows.Forms.ContextMenuStrip();
                menu.Items.Add("Выход", null, (s, args) => CloseApplication());

                _notifyIcon.ContextMenuStrip = menu;
                _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();

                //_splashScreenViewModel.Status = "Проверка привязки к оборудованию...";
                //var bindingService = new MachineBindingService();
                //if (!bindingService.EnsureMachineBinding(out var bindingError))
                //{
                //    SplashScreenManager.CloseAll();
                //    MessageBox.Show(bindingError ?? "Ошибка проверки привязки.", "КИТ-АЗС", MessageBoxButton.OK, MessageBoxImage.Error);
                //    Shutdown();
                //    return;
                //}

                _splashScreenViewModel.Status = "Проверка строки подключения...";

                // Получаем конфигурацию
                var configuration = _host.Services.GetRequiredService<IConfiguration>();
                string connectionString = configuration.GetConnectionString("DefaultConnection");

                // Проверяем, есть ли хоть какая-то строка подключения
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    SplashScreenManager.CloseAll();
                    MessageBox.Show("Строка подключения не задана.", "Настройка подключения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (!PromptForConnectionString(configuration))
                    {
                        Shutdown();
                        return;
                    }
                }
                else
                {
                    // Проверяем возможность подключения к серверу (через базу master)
                    _splashScreenViewModel.Status = "Проверка подключения к серверу БД...";
                    if (!await WaitForSqlServerAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)))
                    {
                        SplashScreenManager.CloseAll();
                        MessageBox.Show("Невозможно установить соединение с сервером.\nПроверьте настройки подключения.",
                                         "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (!PromptForConnectionString(configuration))
                        {
                            Shutdown();
                            return;
                        }
                    }
                }

                _splashScreenViewModel.Status = "Проверка наличия базы данных...";
                // Если база данных отсутствует, создаем её (без таблиц)
                await EnsureDatabaseExistsAsync();

                // Настройка логирования
                _splashScreenViewModel.Status = "Настройка логирования...";
                ConfigureLogging(configuration);

                _splashScreenViewModel.Status = "Запуск хоста...";
                await _host.StartAsync();

                var userStore = _host.Services.GetRequiredService<IUserStore>();
                userStore.OnLogin += UserStore_OnLogin;
                userStore.OnLogout += UserStore_OnLogout;

                _splashScreenViewModel.Status = "Запуск сервисов...";
#if !DEBUG
                await ServiceManager.StartWebAsync();
                await ServiceManager.StartWorkerAsync();
#endif

                _splashScreenViewModel.Status = "Загрузка основного окна...";
                // Получаем главное окно из DI и устанавливаем DataContext
                _window = _host.Services.GetRequiredService<MainWindow>();
                _window.DataContext = _host.Services.GetRequiredService<MainWindowViewModel>();

#if DEBUG
                await Task.Delay(1000);
#else
                await Task.Delay(5000);
#endif
                Login();

                Log.Information("WPF приложение запущено");
            }
            catch (Exception exc)
            {
                //ignore
                MessageBox.Show($"{exc.Message} | {exc.Source} | {exc.StackTrace}");
            }
            finally
            {
                SplashScreenManager.CloseAll();
            }

            base.OnStartup(e);
        }

        private void CloseApplication()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Shutdown();
        }

        private void ShowMainWindow()
        {
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
            }

            MainWindow.Show();
            MainWindow.Activate();
        }

        private void UserStore_OnLogout()
        {
            _window.Hide();
            Login();
        }

        private void UserStore_OnLogin(User user)
        {
            _loginWindow.Closed -= LoginWindow_Closed;
            _loginWindow.Close();
            _window.Show();
        }

        private void Login()
        {
            var loginViewModel = _host.Services.GetRequiredService<LoginViewModel>();

            _loginWindow = new()
            {
                Title = $"Вход в систему {ProductName}",
                Content = new LoginView(),
                DataContext = loginViewModel,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            _loginWindow.Closed += LoginWindow_Closed;

            _loginWindow.Show();
        }

        private void LoginWindow_Closed(object? sender, EventArgs e)
        {
            Shutdown();
        }

        /// <summary>
        /// Проверяет возможность подключения к серверу (используя базу "master").
        /// </summary>
        private async Task<bool> CanConnectToServer()
        {
            try
            {
                // Получаем строку подключения и устанавливаем InitialCatalog = "master"
                var configuration = _host.Services.GetRequiredService<IConfiguration>();
                string baseConnectionString = configuration.GetConnectionString("DefaultConnection");
                var csb = new SqlConnectionStringBuilder(baseConnectionString)
                {
                    InitialCatalog = "master"
                };

                using var connection = new SqlConnection(csb.ConnectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Пытается подключиться к SQL Server до истечения таймаута.
        /// </summary>
        /// <param name="timeout">Общий период ожидания (например, 30 секунд).</param>
        /// <param name="delay">Задержка между попытками (например, 1 секунда).</param>
        /// <returns>True, если удалось подключиться до истечения таймаута.</returns>
        private async Task<bool> WaitForSqlServerAsync(TimeSpan timeout, TimeSpan delay)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (await CanConnectToServer())
                {
                    return true;
                }
                await Task.Delay(delay);
            }
            return false;
        }

        /// <summary>
        /// Открывает окно настроек строки подключения и возвращает true, если настройки успешно обновлены.
        /// </summary>
        private bool PromptForConnectionString(IConfiguration configuration)
        {
            SplashScreenManager.Create(() => new ApplicationSplashScreen(), _splashScreenViewModel).Show();
            _splashScreenViewModel.Status = "Открытие окна настроек подключения...";
            // Открываем окно настроек подключения

            //SqlDataSource sqlDataSource = new();
            //var wizard = SqlDataSourceUIHelper.ConfigureConnection(sqlDataSource);


            var connectionSettingsWindow = new Window()
            {
                Content = new ConnectionStringView(),
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Title = "Настройка подключения к базе данных",
                WindowStyle = WindowStyle.ToolWindow,
                DataContext = new ConnectionStringViewModel(_host.Services.GetRequiredService<ICustomSplashScreenService>(),
                configuration)
            };
            SplashScreenManager.CloseAll();
            bool? result = connectionSettingsWindow.ShowDialog();

            if (result == true)
            {
                // после закрытия окна можно обновить конфигурацию, если настройки сохраняются вне appsettings.json.
                // здесь предполагается, что новая строка подключения уже записана и будет прочитана при следующем обращении.
                return true;
            }
            else
            {
                return false;
            }
        }

        // Метод для явного создания базы данных (без таблиц)
        private async Task EnsureDatabaseExistsAsync()
        {
            // Получаем строку подключения из конфигурации и создаём копию с базой "master"
            var configuration = _host.Services.GetRequiredService<IConfiguration>();
            string baseConnectionString = configuration.GetConnectionString("DefaultConnection");
            var csb = new SqlConnectionStringBuilder(baseConnectionString)
            {
                // Для создания базы данных используем базу master
                InitialCatalog = "master"
            };

            // Имя базы данных, которую хотим создать
            string targetDatabase = new SqlConnectionStringBuilder(baseConnectionString).InitialCatalog;

            // Команда для проверки существования базы и её создания, если отсутствует
            string sqlCheckAndCreate = $@"
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{targetDatabase}')
                BEGIN
                    CREATE DATABASE [{targetDatabase}];
                END";

            using var connection = new SqlConnection(csb.ConnectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(sqlCheckAndCreate, connection);
            await command.ExecuteNonQueryAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Try(() => ServiceManager.StopWebAsync().Wait(TimeSpan.FromSeconds(5)));
            Try(() => ServiceManager.StopWorkerAsync().Wait(TimeSpan.FromSeconds(5)));

            Try(() => _host.StopAsync(TimeSpan.FromSeconds(3)).Wait(TimeSpan.FromSeconds(4)));
            Try(() => _host.Dispose());

            Log.Information("WPF приложение завершено");
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private static void Try(Action a)
        {
            try { a(); }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при завершении приложения: {Message}", ex.Message);
            }
        }
    }
}
