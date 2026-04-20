using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using WixSharp;
using WixSharp.UI.Forms;
using WixSharp.UI.WPF;

namespace KIT.GasStation.Installer
{
    /// <summary>
    /// Диалог активации: пользователь вводит лицензионный ключ,
    /// ключ проверяется локально (HMAC-SHA256 от первых четырёх групп),
    /// при успехе записывается в MSI-свойство LICENSE_KEY.
    /// </summary>
    public partial class ActivationDialog : WpfDialog, IWpfDialog
    {
        public ActivationDialog()
        {
            InitializeComponent();
        }

        public void Init()
        {
            DataContext = model = new ActivationDialogModel { Host = ManagedFormHost };
        }

        ActivationDialogModel model;

        void GoPrev_Click(object sender, RoutedEventArgs e) => model.GoPrev();

        void GoNext_Click(object sender, RoutedEventArgs e) => model.GoNext();

        void Cancel_Click(object sender, RoutedEventArgs e) => model.Cancel();
    }

    internal class ActivationDialogModel : NotifyPropertyChangedBase
    {
        // Секрет для проверки подлинности ключа. В релизе поменяйте на собственное значение
        // и храните его только внутри установщика.
        private const string ActivationSecret = "KIT-GasStation::activation::v1";

        // Формат: XXXXX-XXXXX-XXXXX-XXXXX-XXXXX (буквы + цифры)
        private static readonly Regex KeyPattern =
            new Regex(@"^[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}$", RegexOptions.Compiled);

        private ManagedForm host;
        private string licenseKey;
        private string validationMessage;

        public ManagedForm Host
        {
            get => host;
            set
            {
                host = value;
                // Подтягиваем ранее введённое значение, если пользователь вернулся назад
                licenseKey = session?["LICENSE_KEY"] ?? "";
                NotifyOfPropertyChange(nameof(LicenseKey));
                NotifyOfPropertyChange(nameof(Banner));
            }
        }

        ISession session => Host?.Runtime.Session;
        IManagedUIShell shell => Host?.Shell;

        public BitmapImage Banner => session?.GetResourceBitmap("WixSharpUI_Bmp_Banner")?.ToImageSource() ??
                                     session?.GetResourceBitmap("WixUI_Bmp_Banner")?.ToImageSource();

        public string LicenseKey
        {
            get => licenseKey;
            set
            {
                licenseKey = (value ?? "").Trim().ToUpperInvariant();
                if (session != null)
                    session["LICENSE_KEY"] = licenseKey;
                ValidationMessage = null;
                NotifyOfPropertyChange(nameof(LicenseKey));
            }
        }

        public string ValidationMessage
        {
            get => validationMessage;
            set
            {
                validationMessage = value;
                NotifyOfPropertyChange(nameof(ValidationMessage));
                NotifyOfPropertyChange(nameof(ValidationVisibility));
            }
        }

        public Visibility ValidationVisibility =>
            string.IsNullOrEmpty(validationMessage) ? Visibility.Collapsed : Visibility.Visible;

        public void GoPrev() => shell?.GoPrev();

        public void GoNext()
        {
            if (!KeyPattern.IsMatch(licenseKey ?? ""))
            {
                ValidationMessage = "Неверный формат ключа. Ожидается XXXXX-XXXXX-XXXXX-XXXXX-XXXXX.";
                return;
            }

            if (!ValidateChecksum(licenseKey))
            {
                ValidationMessage = "Лицензионный ключ не прошёл проверку. Убедитесь, что он введён верно.";
                return;
            }

            // Ключ валиден — сохраняем и идём дальше
            if (session != null)
                session["LICENSE_KEY"] = licenseKey;

            shell?.GoNext();
        }

        public void Cancel() => shell?.Cancel();

        /// <summary>
        /// Последняя группа ключа — первые 5 шестнадцатеричных символов HMAC-SHA256
        /// от первых четырёх групп (в верхнем регистре), переведённые в base32-алфавит.
        /// </summary>
        private static bool ValidateChecksum(string key)
        {
            try
            {
                var parts = key.Split('-');
                if (parts.Length != 5) return false;

                var payload = string.Join("-", parts, 0, 4);
                var provided = parts[4];
                var expected = ComputeChecksum(payload);
                return string.Equals(provided, expected, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static string ComputeChecksum(string payload)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ActivationSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

                // base32 без I/L/O/U — чтобы не путать с 1/0
                const string alphabet = "ABCDEFGHJKMNPQRSTVWXYZ23456789";
                var sb = new StringBuilder(5);
                for (int i = 0; i < 5; i++)
                    sb.Append(alphabet[hash[i] % alphabet.Length]);
                return sb.ToString();
            }
        }
    }
}
