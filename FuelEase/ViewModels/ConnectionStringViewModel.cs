using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Editors;
using FuelEase.Domain.Models;
using FuelEase.Helpers;
using FuelEase.SplashScreen;
using FuelEase.ViewModels.Base;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FuelEase.ViewModels
{
    public class ConnectionStringViewModel : BaseViewModel
    {
        #region Private Members

        private readonly ICustomSplashScreenService _customSplashScreenService;
        private SqlConnectionStringBuilder _connectionStringBuilder = new();
        private ObservableCollection<string> _availableDataBases = new();
        private bool _databasesLoaded = false;

        #endregion

        #region Public Properties

        /// <summary>
        /// Имя сервера.
        /// </summary>
        public string ServerName
        {
            get => _connectionStringBuilder.DataSource;
            set
            {
                _connectionStringBuilder.DataSource = value;
                _databasesLoaded = false;
                OnPropertyChanged(nameof(ServerName));
            }
        }

        /// <summary>
        /// Тип аутентификации.
        /// </summary>
        public AuthenticationType AuthenticationType
        {
            get => _connectionStringBuilder.IntegratedSecurity ? AuthenticationType.Windows : AuthenticationType.SQLServer;
            set
            {
                _connectionStringBuilder.IntegratedSecurity = value == AuthenticationType.Windows;
                OnPropertyChanged(nameof(AuthenticationType));
            }
        }

        /// <summary>
        /// Имя пользователя.
        /// </summary>
        public string UserName
        {
            get => _connectionStringBuilder.UserID;
            set
            {
                _connectionStringBuilder.UserID = value;
                OnPropertyChanged(nameof(UserName));
            }
        }

        /// <summary>
        /// Пароль.
        /// </summary>
        public string Password
        {
            get => _connectionStringBuilder.Password;
            set
            {
                _connectionStringBuilder.Password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        /// <summary>
        /// Использовать шифрование.
        /// </summary>
        public bool Encryp
        {
            get => _connectionStringBuilder.Encrypt;
            set
            {
                _connectionStringBuilder.Encrypt = value;
                OnPropertyChanged(nameof(Encryp));
            }
        }

        /// <summary>
        /// Доверять сертификату сервера.
        /// </summary>
        public bool TrustServerCertificate
        {
            get => _connectionStringBuilder.TrustServerCertificate;
            set
            {
                _connectionStringBuilder.TrustServerCertificate = value;
                OnPropertyChanged(nameof(TrustServerCertificate));
            }
        }

        /// <summary>
        /// Выбранная база данных.
        /// </summary>
        public string SelectedDataBase
        {
            get => _connectionStringBuilder.InitialCatalog;
            set
            {
                _connectionStringBuilder.InitialCatalog = value;
                OnPropertyChanged(nameof(SelectedDataBase));
            }
        }

        /// <summary>
        /// Список вариантов аутентификации.
        /// </summary>
        public List<KeyValuePair<AuthenticationType, string>> AvailableAuthentications => new(EnumHelper.GetLocalizedEnumValues<AuthenticationType>());

        public ObservableCollection<string> AvailableDataBases
        {
            get => _availableDataBases;
            set
            {
                _availableDataBases = value;
                OnPropertyChanged(nameof(AvailableDataBases));
            }
        }

        #endregion

        #region Constructors

        public ConnectionStringViewModel(ICustomSplashScreenService customSplashScreenService, 
            IConfiguration configuration)
        {
            _customSplashScreenService = customSplashScreenService;
            CreateSqlConnection(configuration);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Загружает список доступных баз данных.
        /// </summary>
        [Command]
        public async Task AvailableDataBasesPopupOpening(OpenPopupEventArgs args)
        {
            // Разрешаем DevExpress открыть попап
            args.Cancel = false;

            if (args.Source is ComboBoxEdit comboBox)
            {
                // Если данные уже загружены, не выполняем повторную загрузку
                if (!_databasesLoaded)
                {
                    try
                    {
                        var csb = new SqlConnectionStringBuilder(_connectionStringBuilder.ConnectionString)
                        {
                            InitialCatalog = "master"
                        };

                        using var connection = new SqlConnection(csb.ConnectionString);
                        await connection.OpenAsync();

                        using var command = connection.CreateCommand();
                        command.CommandText = "SELECT name FROM sys.databases ORDER BY name";

                        using var reader = await command.ExecuteReaderAsync();

                        AvailableDataBases.Clear();
                        while (await reader.ReadAsync())
                        {
                            var dbName = reader.GetString(0);
                            AvailableDataBases.Add(dbName);
                        }
                        _databasesLoaded = true;
                    }
                    catch (Exception ex)
                    {
                        // Обработка ошибки
                    }
                }

                // Если данные загружены, можно открыть попап
                _customSplashScreenService.Close();
                _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    comboBox.IsPopupOpen = true;
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
        }

        [Command]
        public async Task Save()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            try
            {
                // Проверяем, существует ли файл
                if (!File.Exists(filePath))
                {
                    // Создаем базовую структуру JSON с секцией ConnectionStrings
                    var defaultConfig = new JObject(
                        new JProperty("ConnectionStrings", new JObject(
                            new JProperty("DefaultConnection", "")
                        )),
                        new JProperty("Logging", new JObject(
                            new JProperty("LogLevel", new JObject(
                                new JProperty("Default", "Information")
                            ))
                        ))
                    );

                    // Записываем содержимое в файл с форматированием
                    File.WriteAllText(filePath, defaultConfig.ToString(Newtonsoft.Json.Formatting.Indented));
                }

                // Читаем содержимое файла
                var json = await File.ReadAllTextAsync(filePath);

                // Загружаем JSON как JObject
                var jObject = JObject.Parse(json);

                // Переходим к разделу ConnectionStrings
                var connectionStrings = jObject["ConnectionStrings"] as JObject;
                if (connectionStrings == null)
                {
                    // Если раздела нет, создаём его
                    connectionStrings = new JObject();
                    jObject["ConnectionStrings"] = connectionStrings;
                }

                // Обновляем или создаём строку подключения
                connectionStrings["DefaultConnection"] = _connectionStringBuilder.ConnectionString;

                // Сериализуем обратно в строку
                string output = JsonConvert.SerializeObject(jObject, Formatting.Indented);

                // Сохраняем изменения в файл
                await File.WriteAllTextAsync(filePath, output);

                MessageBoxService.ShowMessage("Строка подключения успешно сохранена", "Успех", MessageButton.OK, MessageIcon.Information);
            }
            catch (Exception ex)
            {
                // Обработка ошибки
                // Например, логгирование: Log.Error(ex, "Ошибка обновления строки подключения");
                MessageBoxService.ShowMessage($"Ошибка сохранения строки подключения.\nПодробнее: {ex.Message}", "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
        }

        [Command]
        public async Task CheckConnection()
        {
            try
            {
                _customSplashScreenService.Show("Проверка подключения к серверу...");
                // Обновляем строки подключения
                _connectionStringBuilder.DataSource = ServerName;
                _connectionStringBuilder.IntegratedSecurity = AuthenticationType == AuthenticationType.Windows;
                _connectionStringBuilder.UserID = UserName;
                _connectionStringBuilder.Password = Password;
                _connectionStringBuilder.Encrypt = Encryp;
                _connectionStringBuilder.TrustServerCertificate = TrustServerCertificate;
                using var connection = new SqlConnection(_connectionStringBuilder.ConnectionString);
                await connection.OpenAsync();
                MessageBoxService.ShowMessage("Подключение успешно установлено", "Успех", MessageButton.OK, MessageIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowMessage($"Ошибка подключения к серверу.\nПодробнее: {ex.Message}", "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
            finally
            {
                _customSplashScreenService.Close();
            }
        }

        #endregion

        #region Private Voids

        /// <summary>
        /// Создает строку подключения на основе конфигурации.
        /// </summary>
        private void CreateSqlConnection(IConfiguration configuration)
        {
            try
            {
                string baseConnectionString = configuration.GetConnectionString("DefaultConnection");
                _connectionStringBuilder = new SqlConnectionStringBuilder(baseConnectionString);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

    }

    public class DataBaseItem : DomainObject
    {
        #region Private Members

        private string _name;

        #endregion

        /// <summary>
        /// Имя базы данных.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public override void Update(DomainObject updatedItem)
        {
            throw new System.NotImplementedException();
        }
    }

    public enum AuthenticationType
    {
        [Display(Name = "Проверка подлинности Windows")]
        Windows,    // Аутентификация Windows

        [Display(Name = "Проверка подлинности SQL Server")]
        SQLServer   // Аутентификация SQL Server (SQL Logins)
    }

}
