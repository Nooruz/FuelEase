using FuelEase.Domain.Services;
using FuelEase.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FuelEase.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Private Members

        private FuelEaseSettings _settings = new();

        #endregion

        #region Public Properties

        public FuelEaseSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }

        public List<ReceiptPrintingModeTypeItem> ReceiptPrintingModeTypeItems
        {
            get
            {
                ReceiptPrintingModeType[] enumValues = (ReceiptPrintingModeType[])Enum.GetValues(typeof(ReceiptPrintingModeType));
                return enumValues
                .Select(e => new ReceiptPrintingModeTypeItem { Value = e.ToString(), Description = GetEnumDescription(e) })
                .ToList();
            }
        }

        public IEnumerable<PropertyDescriptor> Properties { get { return TypeDescriptor.GetProperties(Settings).Cast<PropertyDescriptor>(); } }

        #endregion

        #region Constructor

        public SettingsViewModel()
        {
            
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

    public class FuelEaseSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Public Properties

        [Category("Идентификация"), Description("Наименование"), Display(Name = "Наименование")]
        public string NameGasStation
        {
            get => Properties.Settings.Default.NameGasStation;
            set
            {
                Properties.Settings.Default.NameGasStation = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(NameGasStation));
            }
        }

        [Category("Идентификация"), Description("Код"), Display(Name = "№ АЗС")]
        public string IdGasStation
        {
            get => Properties.Settings.Default.IdGasStation;
            set
            {
                Properties.Settings.Default.IdGasStation = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(IdGasStation));
            }
        }

        [Category("Смена"), Description("Автозакрытие смены по времени"), Display(Name = "Автозакрытие смены по времени")]
        public bool AutoClosingShift
        {
            get => Properties.Settings.Default.AutoClosingShift;
            set
            {
                Properties.Settings.Default.AutoClosingShift = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(AutoClosingShift));
            }
        }

        [Category("Смена"), Description("Режим печати чека"), Display(Name = "Режим печати чека")]
        public ReceiptPrintingModeTypeItem ReceiptPrintingModeTypeItem
        {
            get
            {
                var value = (ReceiptPrintingModeType)Enum.Parse(typeof(ReceiptPrintingModeType), Properties.Settings.Default.ReceiptPrintingMode);
                return new()
                {
                    Description = GetEnumDescription(value),
                    Value = value.ToString()
                };
            }
            set
            {
                Properties.Settings.Default.ReceiptPrintingMode = value.Value;
                Properties.Settings.Default.Save();
                OnPropertyChanged(nameof(ReceiptPrintingModeTypeItem));
            }
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
        public string Value { get; set; }
        public string Description { get; set; }
    }

}
