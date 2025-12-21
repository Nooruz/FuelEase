using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using KIT.GasStation.Helpers;
using KIT.GasStation.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Input;

namespace KIT.GasStation.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Private Members

        private GasStationSettings _settings = new();

        #endregion

        #region Public Properties

        public GasStationSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }

        public List<ReceiptPrintingModeTypeItem> ReceiptPrintingModeTypeItems =>
            Enum.GetValues(typeof(ReceiptPrintingModeType))
                .Cast<ReceiptPrintingModeType>()
                .Select(e => new ReceiptPrintingModeTypeItem
                {
                    Value = e.ToString(),
                    Description = GetEnumDescription(e)
                })
                .ToList();

        public IEnumerable<PropertyDescriptor> Properties { get { return TypeDescriptor.GetProperties(Settings).Cast<PropertyDescriptor>(); } }

        #endregion

        #region Constructor

        public SettingsViewModel()
        {
            Title = "Настройки";
        }

        #endregion

        #region Public Commands

        [Command]
        public void Save()
        {
            // Если есть ошибки — не сохраняем
            if (Settings.HasErrors)
                return;

            Settings.ApplyAndSave();
        }

        [Command]
        public void Reload()
        {
            Settings.ReloadDraftsFromStorage();
        }

        #endregion

        #region Private Voids

        private string GetEnumDescription(Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute));

            return attribute != null ? attribute.Description : value.ToString();
        }

        #endregion
    }

    public static class Cat
    {
        public const string Identification = "Идентификация";
        public const string Shift = "Смена";

        public const string HotKeys = "Горячие клавиши";
        public const string HotKeys_Payment = HotKeys + @"\Способы оплаты";
        public const string HotKeys_Trk = HotKeys + @"\Состояние ТРК";
        public const string HotKeys_Trk_Free = HotKeys_Trk + @"\Свободно";
    }



    public class GasStationSettings : INotifyPropertyChanged, IDataErrorInfo
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #region Private Members

        private string _fuelSaleCashlessDraft = Properties.HotKeys.Default.FuelSaleCashless;
        private string _fuelSaleCashDraft = Properties.HotKeys.Default.FuelSaleCash;
        private string _fuelSaleTicketDraft = Properties.HotKeys.Default.FuelSaleTicket;
        private string _startFullFuelingDraft = Properties.HotKeys.Default.StartFullFueling;
        private string _fuelSaleStatementDraft = Properties.HotKeys.Default.FuelSaleStatement;
        private string _fuelSaleDiscountCardDraft = Properties.HotKeys.Default.FuelSaleDiscountCard;
        private string _fuelSaleCardDraft = Properties.HotKeys.Default.FuelSaleCard;
        private string _fuelSaleOtherDraft = Properties.HotKeys.Default.FuelSaleOther;
        private string _nameGasStationDraft = Properties.Settings.Default.NameGasStation;
        private string _idGasStationDraft = Properties.Settings.Default.IdGasStation;
        private bool _autoClosingShiftDraft = Properties.Settings.Default.AutoClosingShift;
        private ReceiptPrintingModeTypeItem _receiptPrintingModeTypeItemDraft =
            new ReceiptPrintingModeTypeItem
            {
                Value = Properties.Settings.Default.ReceiptPrintingMode,
                Description = Properties.Settings.Default.ReceiptPrintingMode
            };

        #endregion

        #region Constructors

        public GasStationSettings()
        {
            // нормализуем описание для режима печати
            _receiptPrintingModeTypeItemDraft = BuildReceiptPrintingModeItem(Properties.Settings.Default.ReceiptPrintingMode);
        }

        #endregion

        #region Public Properties

        public bool HasErrors => GetAllErrors().Any();

        [Category(Cat.Identification), Description("Наименование АЗС"), Display(Name = "Наименование")]
        public string NameGasStation
        {
            get => _nameGasStationDraft;
            set { _nameGasStationDraft = value; OnPropertyChanged(nameof(NameGasStation)); }
        }

        [Category(Cat.Identification), Description("Идентификатор/номер АЗС"), Display(Name = "№ АЗС")]
        public string IdGasStation
        {
            get => _idGasStationDraft;
            set { _idGasStationDraft = value; OnPropertyChanged(nameof(IdGasStation)); }
        }

        [Category(Cat.Shift), Description("Автоматическое закрытие смены по расписанию/времени"), Display(Name = "Автозакрытие смены по времени")]
        public bool AutoClosingShift
        {
            get => _autoClosingShiftDraft;
            set { _autoClosingShiftDraft = value; OnPropertyChanged(nameof(AutoClosingShift)); }
        }

        [Category(Cat.Shift), Description("Определяет, когда печатать чек"), Display(Name = "Режим печати чека")]
        public ReceiptPrintingModeTypeItem ReceiptPrintingModeTypeItem
        {
            get => _receiptPrintingModeTypeItemDraft;
            set
            {
                _receiptPrintingModeTypeItemDraft = value ?? BuildReceiptPrintingModeItem(Properties.Settings.Default.ReceiptPrintingMode);
                OnPropertyChanged(nameof(ReceiptPrintingModeTypeItem));
            }
        }

        [Category(Cat.HotKeys_Payment), Description("Выбор способа оплаты: безналичный расчёт"), Display(Name = "Безналичными")]
        public string FuelSaleCashless
        {
            get => _fuelSaleCashlessDraft;
            set { _fuelSaleCashlessDraft = value; OnPropertyChanged(nameof(FuelSaleCashless)); }
        }

        [Category(Cat.HotKeys_Payment), Description("Выбор способа оплаты: наличный расчёт"), Display(Name = "Наличными")]
        public string FuelSaleCash
        {
            get => _fuelSaleCashDraft;
            set { _fuelSaleCashDraft = value; OnPropertyChanged(nameof(FuelSaleCash)); }
        }

        [Category(Cat.HotKeys_Payment), Description("Выбор способа оплаты: по ведомости"), Display(Name = "Ведомость")]
        public string FuelSaleStatement
        {
            get => _fuelSaleStatementDraft;
            set { _fuelSaleStatementDraft = value; OnPropertyChanged(nameof(FuelSaleStatement)); }
        }

        [Category(Cat.HotKeys_Payment), Description("Выбор способа оплаты: дисконтная карта"), Display(Name = "Дисконтная карта")]
        public string FuelSaleDiscountCard
        {
            get => _fuelSaleDiscountCardDraft;
            set { _fuelSaleDiscountCardDraft = value; OnPropertyChanged(nameof(FuelSaleDiscountCard)); }
        }

        [Category(Cat.HotKeys_Payment), Description("Выбор способа оплаты: топливная карта"), Display(Name = "Топливная карта")]
        public string FuelSaleCard
        {
            get => _fuelSaleCardDraft;
            set { _fuelSaleCardDraft = value; OnPropertyChanged(nameof(FuelSaleCard)); }
        }

        [Category(Cat.HotKeys_Payment), Description("Выбор способа оплаты: талоны"), Display(Name = "Талон")]
        public string FuelSaleTicket
        {
            get => _fuelSaleTicketDraft;
            set { _fuelSaleTicketDraft = value; OnPropertyChanged(nameof(FuelSaleTicket)); }
        }

        [Category(Cat.HotKeys_Payment), Description("Выбор способа оплаты: прочие варианты"), Display(Name = "Другое")]
        public string FuelSaleOther
        {
            get => _fuelSaleOtherDraft;
            set { _fuelSaleOtherDraft = value; OnPropertyChanged(nameof(FuelSaleOther)); }
        }

        [Category(Cat.HotKeys_Trk_Free), Description("Запуск продажи: «до полного бака»"), Display(Name = "До полного бака")]
        public string StartFullFueling
        {
            get => _startFullFuelingDraft;
            set { _startFullFuelingDraft = value; OnPropertyChanged(nameof(StartFullFueling)); }
        }



        #endregion

        #region Public Voids

        public void ApplyAndSave()
        {
            // Settings
            Properties.Settings.Default.NameGasStation = _nameGasStationDraft?.Trim() ?? "";
            Properties.Settings.Default.IdGasStation = _idGasStationDraft?.Trim() ?? "";
            Properties.Settings.Default.AutoClosingShift = _autoClosingShiftDraft;

            if (!string.IsNullOrWhiteSpace(_receiptPrintingModeTypeItemDraft?.Value))
                Properties.Settings.Default.ReceiptPrintingMode = _receiptPrintingModeTypeItemDraft.Value;

            Properties.Settings.Default.Save();

            // HotKeys
            Properties.HotKeys.Default.FuelSaleCashless = NormalizeHotKey(_fuelSaleCashlessDraft);
            Properties.HotKeys.Default.FuelSaleCash = NormalizeHotKey(_fuelSaleCashDraft);
            Properties.HotKeys.Default.FuelSaleStatement = NormalizeHotKey(_fuelSaleStatementDraft);
            Properties.HotKeys.Default.FuelSaleDiscountCard = NormalizeHotKey(_fuelSaleDiscountCardDraft);
            Properties.HotKeys.Default.FuelSaleCard = NormalizeHotKey(_fuelSaleCardDraft);
            Properties.HotKeys.Default.FuelSaleTicket = NormalizeHotKey(_fuelSaleTicketDraft);
            Properties.HotKeys.Default.FuelSaleOther = NormalizeHotKey(_fuelSaleOtherDraft);
            Properties.HotKeys.Default.StartFullFueling = NormalizeHotKey(_startFullFuelingDraft);

            Properties.HotKeys.Default.Save();
        }

        public void ReloadDraftsFromStorage()
        {
            _nameGasStationDraft = Properties.Settings.Default.NameGasStation;
            _idGasStationDraft = Properties.Settings.Default.IdGasStation;
            _autoClosingShiftDraft = Properties.Settings.Default.AutoClosingShift;
            _receiptPrintingModeTypeItemDraft = BuildReceiptPrintingModeItem(Properties.Settings.Default.ReceiptPrintingMode);

            _fuelSaleCashlessDraft = Properties.HotKeys.Default.FuelSaleCashless;
            _fuelSaleCashDraft = Properties.HotKeys.Default.FuelSaleCash;
            _fuelSaleStatementDraft = Properties.HotKeys.Default.FuelSaleStatement;
            _fuelSaleDiscountCardDraft = Properties.HotKeys.Default.FuelSaleDiscountCard;
            _fuelSaleCardDraft = Properties.HotKeys.Default.FuelSaleCard;
            _fuelSaleTicketDraft = Properties.HotKeys.Default.FuelSaleTicket;
            _fuelSaleOtherDraft = Properties.HotKeys.Default.FuelSaleOther;
            _startFullFuelingDraft = Properties.HotKeys.Default.StartFullFueling;

            OnPropertyChanged(nameof(NameGasStation));
            OnPropertyChanged(nameof(IdGasStation));
            OnPropertyChanged(nameof(AutoClosingShift));
            OnPropertyChanged(nameof(ReceiptPrintingModeTypeItem));

            OnPropertyChanged(nameof(FuelSaleCashless));
            OnPropertyChanged(nameof(FuelSaleCash));
            OnPropertyChanged(nameof(FuelSaleStatement));
            OnPropertyChanged(nameof(FuelSaleDiscountCard));
            OnPropertyChanged(nameof(FuelSaleCard));
            OnPropertyChanged(nameof(FuelSaleTicket));
            OnPropertyChanged(nameof(FuelSaleOther));
            OnPropertyChanged(nameof(StartFullFueling));
        }

        #endregion

        #region Private Voids

        private static string GetEnumDescription(Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute));

            return attribute != null ? attribute.Description : value.ToString();
        }

        private string Validate(string columnName)
        {
            // Формат хоткеев
            if (IsHotKeyProperty(columnName))
            {
                var text = GetHotKeyValue(columnName);

                if (!HotKeyParser.TryParse(text, out var g))
                    return "Неверный формат. Пример: Ctrl+F11, F1, Insert, Ctrl+Shift+S";

                // Запрет пустых/None
                if (g == null || g.Key == Key.None)
                    return "Комбинация не распознана.";

                // Дубликаты
                if (HasDuplicateHotKey(columnName, NormalizeHotKey(text)))
                    return "Эта комбинация уже назначена другому действию.";
            }

            return string.Empty;
        }

        private IEnumerable<string> GetAllErrors()
        {
            // перечисли все хоткей-свойства и собери ошибки
            foreach (var name in HotKeyPropertyNames)
            {
                var err = Validate(name);
                if (!string.IsNullOrWhiteSpace(err))
                    yield return err;
            }
        }

        private static readonly string[] HotKeyPropertyNames =
        {
            nameof(FuelSaleCashless),
            nameof(FuelSaleCash),
            nameof(FuelSaleStatement),
            nameof(FuelSaleDiscountCard),
            nameof(FuelSaleCard),
            nameof(FuelSaleTicket),
            nameof(FuelSaleOther),
            nameof(StartFullFueling),
        };
        private static bool IsHotKeyProperty(string columnName) =>
            HotKeyPropertyNames.Contains(columnName);
        private string GetHotKeyValue(string columnName) => columnName switch
        {
            nameof(FuelSaleCashless) => _fuelSaleCashlessDraft,
            nameof(FuelSaleCash) => _fuelSaleCashDraft,
            nameof(FuelSaleStatement) => _fuelSaleStatementDraft,
            nameof(FuelSaleDiscountCard) => _fuelSaleDiscountCardDraft,
            nameof(FuelSaleCard) => _fuelSaleCardDraft,
            nameof(FuelSaleTicket) => _fuelSaleTicketDraft,
            nameof(FuelSaleOther) => _fuelSaleOtherDraft,
            nameof(StartFullFueling) => _startFullFuelingDraft,
            _ => ""
        };
        private bool HasDuplicateHotKey(string selfPropName, string normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            var all = new Dictionary<string, string>
            {
                [nameof(FuelSaleCashless)] = NormalizeHotKey(_fuelSaleCashlessDraft),
                [nameof(FuelSaleCash)] = NormalizeHotKey(_fuelSaleCashDraft),
                [nameof(FuelSaleStatement)] = NormalizeHotKey(_fuelSaleStatementDraft),
                [nameof(FuelSaleDiscountCard)] = NormalizeHotKey(_fuelSaleDiscountCardDraft),
                [nameof(FuelSaleCard)] = NormalizeHotKey(_fuelSaleCardDraft),
                [nameof(FuelSaleTicket)] = NormalizeHotKey(_fuelSaleTicketDraft),
                [nameof(FuelSaleOther)] = NormalizeHotKey(_fuelSaleOtherDraft),
                [nameof(StartFullFueling)] = NormalizeHotKey(_startFullFuelingDraft),
            };

            return all
                .Where(kv => kv.Key != selfPropName)
                .Any(kv => string.Equals(kv.Value, normalized, StringComparison.OrdinalIgnoreCase));
        }
        private static string NormalizeHotKey(string? text) =>
            (text ?? "").Trim();
        private static ReceiptPrintingModeTypeItem BuildReceiptPrintingModeItem(string rawValue)
        {
            if (Enum.TryParse(typeof(ReceiptPrintingModeType), rawValue, out var enumObj) && enumObj is ReceiptPrintingModeType m)
            {
                return new ReceiptPrintingModeTypeItem { Value = m.ToString(), Description = GetEnumDescription(m) };
            }
            // fallback
            return new ReceiptPrintingModeTypeItem { Value = ReceiptPrintingModeType.Before.ToString(), Description = GetEnumDescription(ReceiptPrintingModeType.Before) };
        }

        #endregion

        #region DataErrorInfo

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(FuelSaleCashless))
                {
                    if (!HotKeyParser.TryParse(_fuelSaleCashlessDraft, out _))
                        return "Неверный формат. Пример: Ctrl+F11, F1, Insert, Ctrl+Shift+S";
                }

                return string.Empty;
            }
        }

        #endregion

    }

    /// <summary>
    /// Тип режима печати чека
    /// </summary>
    public enum ReceiptPrintingModeType
    {
        [Description("До")]
        Before,

        [Description("После")]
        After
    }

    public class ReceiptPrintingModeTypeItem
    {
        public string Value { get; set; } = "";
        public string Description { get; set; } = "";
    }

}
