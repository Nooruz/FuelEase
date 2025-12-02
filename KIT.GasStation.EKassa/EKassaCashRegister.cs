using KIT.GasStation.CashRegisters.Exceptions;
using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using QRCoder;
using Serilog;
using System.Drawing.Printing;
using System.Drawing;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Dynamic;


namespace KIT.GasStation.EKassa
{
    public class EKassaCashRegister : ICashRegisterService
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private ILogger _logger;
        private CashRegister _cashRegister;
        private EKassaCashRegisterSettings _settings;
        private HttpClient _client;
        private const string _openShift = "shift_open_by_fiscal_number";
        private const string _closeShift = "shift_close_by_fiscal_number";
        private const string _receipt = "receipt";
        private const string _xReport = "shift_state_by_fiscal_number";
        private string? _check = string.Empty;
        private string? _url = string.Empty;
        

        #endregion

        #region Actions

        public event Action OnShiftOpened;
        public event Action OnShiftClosed;
        public event Action OnReceiptPrinting;
        public event Action<FuelSale> OnReturning;
        public event Action<string> OnUnknownError;
        public event Action<CashRegisterStatus> OnStatusChanged;

        #endregion

        #region Constructors

        public EKassaCashRegister(IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            // Инициализация логгера
            InitLog();
        }

        #endregion

        #region Public Voids

        /// <inheritdoc/>
        public async Task CloseShiftAsync(string cashierName)
        {
            _logger.Information("Закрытие смены ККМ...");
            dynamic fiscalNumber = new ExpandoObject();
            fiscalNumber.fiscal_number = _cashRegister.RegistrationNumber;

            if (_settings.TapeType == TapeType.TXT)
            {
                fiscalNumber.txt = true;
            }
            else
            {
                fiscalNumber.txt80 = true;
            }

            _cashRegister.Status = await ExecuteOperationAsync($"/api/{_closeShift}", fiscalNumber);
            _logger.Information("Статус кассы после закрытия смены: {Status}", _cashRegister.Status);
            OnStatusChanged?.Invoke(_cashRegister.Status);
            if (_cashRegister.Status == CashRegisterStatus.Close)
            {
                OnShiftClosed?.Invoke();
            }
        }

        /// <inheritdoc/>
        public async Task InitializationAsync(Guid cashRegisterId)
        {
            CashRegister? cashRegister = await _hardwareConfigurationService.GetCashRegisterAsync(cashRegisterId);

            if (cashRegister == null)
            {
                _logger.Error("Касса не найдена. [{Timestamp}]", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
                throw new CashRegisterException("ККМ не найдена. Проверьте настройки ККМ.");
            }

            if (cashRegister.Settings is EKassaCashRegisterSettings settings)
            {
                _settings = settings;
            }
            
            _cashRegister = cashRegister;

            // Авторизация на сервере ЕКасса
            await LoginAsync();
        }

        /// <inheritdoc/>
        public async Task OpenShiftAsync(string cashierName)
        {
            _logger.Information("Открытие смены ККМ...");

            dynamic fiscalNumber = new ExpandoObject();
            fiscalNumber.fiscal_number = _cashRegister.RegistrationNumber;

            if (_settings.TapeType == TapeType.TXT)
            {
                fiscalNumber.txt = true;
            }
            else
            {
                fiscalNumber.txt80 = true;
            }

            _cashRegister.Status = await ExecuteOperationAsync($"/api/{_openShift}", fiscalNumber);
            _logger.Information("Статус кассы после открытия смены: {Status}", _cashRegister.Status);
            OnStatusChanged?.Invoke(_cashRegister.Status);
            if (_cashRegister.Status == CashRegisterStatus.Open)
            {
                OnShiftOpened?.Invoke();
            }
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> SaleAsync(FuelSale fuelSale, Fuel fuel, string cashierName)
        {
            try
            {
                _logger.Information("Начало продажи через ККМ. Сумма: {Sum}, Топливо: {Fuel}", fuelSale.Sum, fuel.Name);

                decimal price = fuelSale.Price * 100;
                decimal quantity = Math.Round(fuelSale.Sum / fuelSale.Price, 6);
                string discount = string.Empty;
                string received = fuelSale.CustomerSum != null ? (fuelSale.CustomerSum * 100).ToString() : string.Empty;

                if (fuelSale.DiscountSale != null)
                {
                    quantity = Math.Round(fuelSale.Sum / fuelSale.DiscountSale.DiscountPrice, 6);
                    price = fuelSale.DiscountSale.DiscountPrice * 100;
                    discount = (((quantity * fuelSale.Price) - fuelSale.Sum) * 100).ToString();
                }

                dynamic fiscalNumber = new ExpandoObject();
                fiscalNumber.fiscal_number = _cashRegister.RegistrationNumber;
                fiscalNumber.received = received;
                fiscalNumber.discount = discount;

                fiscalNumber.goods = new[]
                {
                    new
                    {
                        calcItemAttributeCode = 0,
                        name = fuel.Name,
                        sgtin = fuel.TNVED,
                        price,
                        quantity,
                        unit = fuel.UnitOfMeasurement.Name,
                        st = fuel.SalesTax * 100,
                        vat = fuel.ValueAddedTax ? 12 : 0
                    }
                };

                fiscalNumber.cash = fuelSale.PaymentType == PaymentType.Cash;
                fiscalNumber.operation = "INCOME";

                // Условное свойство
                if (_settings.TapeType == TapeType.TXT)
                    fiscalNumber.txt = true;
                else
                    fiscalNumber.txt80 = true;

                _logger.Information("Отправка данных в ККМ: {Data}", JsonSerializer.Serialize(fiscalNumber));

                _cashRegister.Status = await ExecuteOperationAsync($"/api/v2/{_receipt}", fiscalNumber, fuelSale);
                _logger.Information("Продажа завершена. Статус кассы: {Status}", _cashRegister.Status);
                OnStatusChanged?.Invoke(_cashRegister.Status);

                if (_cashRegister.Status == CashRegisterStatus.Open)
                {
                    OnReceiptPrinting?.Invoke();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при продаже через ККМ");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task XReportAsync(bool printReceipt = true)
        {
            _logger.Information("Получения статуса Х-Отчет...");

            dynamic fiscalNumber = new ExpandoObject();
            fiscalNumber.fiscal_number = _cashRegister.RegistrationNumber;

            if (_settings.TapeType == TapeType.TXT)
            {
                fiscalNumber.txt = true;
            }
            else
            {
                fiscalNumber.txt80 = true;
            }

            _cashRegister.Status = await ExecuteOperationAsync($"/api/{_xReport}", fiscalNumber, printReceipt: printReceipt);
            _logger.Information("Статус кассы: {Status}", _cashRegister.Status);
            OnStatusChanged?.Invoke(_cashRegister.Status);
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> ReturnAsync(FuelSale fuelSale, Fuel fuel)
        {
            try
            {
                _logger.Information("Начало возврата через ККМ. Сумма: {Sum}, Топливо: {Fuel}", fuelSale.Sum, fuel.Name);

                decimal price = fuelSale.Price * 100;
                decimal quantity = Math.Round(fuelSale.Sum / fuelSale.Price, 6);

                if (fuelSale.DiscountSale != null)
                {
                    quantity = Math.Round(fuelSale.Sum / fuelSale.DiscountSale.DiscountPrice, 6);
                    price = fuelSale.DiscountSale.DiscountPrice * 100;
                }

                if (fuelSale.FiscalData == null)
                {
                    return null;
                }

                dynamic fiscalNumber = new ExpandoObject();
                fiscalNumber.fiscal_number = _cashRegister.RegistrationNumber;

                // Условно добавляем txt или txt80
                if (_settings.TapeType == TapeType.TXT)
                    fiscalNumber.txt = true;
                else
                    fiscalNumber.txt80 = true;

                // Добавляем товары
                fiscalNumber.goods = new[]
                {
                    new
                    {
                        calcItemAttributeCode = 0,
                        name = fuel.Name,
                        sgtin = fuel.TNVED,
                        price,
                        quantity,
                        unit = fuel.UnitOfMeasurement.Name,
                        st = fuel.SalesTax * 100,
                        vat = fuel.ValueAddedTax ? 12 : 0
                    }
                };

                // Остальные поля
                fiscalNumber.cash = fuelSale.PaymentType == PaymentType.Cash;
                fiscalNumber.operation = "INCOME_RETURN";
                fiscalNumber.originFdNumber = fuelSale.FiscalData.FiscalDocument;

                _logger.Information("Отправка данных в ККМ: {Data}", JsonSerializer.Serialize(fiscalNumber));

                _cashRegister.Status = await ExecuteOperationAsync($"/api/v2/{_receipt}", fiscalNumber, fuelSale);
                _logger.Information("Возврат завершен. Статус кассы: {Status}", _cashRegister.Status);
                OnStatusChanged?.Invoke(_cashRegister.Status);

                if (_cashRegister.Status == CashRegisterStatus.Open)
                {
                    OnReturning?.Invoke(fuelSale);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при возврате через ККМ");
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task<string?> GetShiftStateAsync()
        {
            _logger.Information("Получения статуса Х-Отчет...");

            dynamic fiscalNumber = new ExpandoObject();
            fiscalNumber.fiscal_number = _cashRegister.RegistrationNumber;

            if (_settings.TapeType == TapeType.TXT)
            {
                fiscalNumber.txt = true;
            }
            else
            {
                fiscalNumber.txt80 = true;
            }

            _cashRegister.Status = await ExecuteOperationAsync($"/api/{_xReport}", fiscalNumber);
            _logger.Information("Статус кассы: {Status}", _cashRegister.Status);
            OnStatusChanged?.Invoke(_cashRegister.Status);

            return _check;
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> ReturnAndReceivedSaleAsync(FuelSale fuelSale, Fuel fuel, string cashierName)
        {
            try
            {
                await ReturnAsync(fuelSale, fuel);

                if (fuelSale.ReceivedSum == 0)
                    return null;

                _logger.Information("Начало продажи через ККМ. Сумма: {Sum}, Топливо: {Fuel}", fuelSale.Sum, fuel.Name);

                decimal price = fuelSale.Price * 100;
                decimal quantity = Math.Round(fuelSale.ReceivedSum / fuelSale.Price, 6);
                string discount = string.Empty;
                string received = fuelSale.CustomerSum != null ? (fuelSale.CustomerSum * 100).ToString() : string.Empty;

                if (fuelSale.DiscountSale != null)
                {
                    quantity = Math.Round(fuelSale.ReceivedSum / fuelSale.DiscountSale.DiscountPrice, 6);
                    price = fuelSale.DiscountSale.DiscountPrice * 100;
                    discount = (((quantity * fuelSale.Price) - fuelSale.ReceivedSum) * 100).ToString();
                }

                dynamic fiscalNumber = new ExpandoObject();

                // Обязательные поля
                fiscalNumber.fiscal_number = _cashRegister.RegistrationNumber;
                fiscalNumber.received = received;
                fiscalNumber.discount = discount;

                // Условное поле
                if (_settings.TapeType == TapeType.TXT)
                    fiscalNumber.txt = true;
                else
                    fiscalNumber.txt80 = true;

                // Массив товаров
                fiscalNumber.goods = new[]
                {
                    new
                    {
                        calcItemAttributeCode = 0,
                        name = fuel.Name,
                        sgtin = fuel.TNVED,
                        price,
                        quantity,
                        unit = fuel.UnitOfMeasurement.Name,
                        st = fuel.SalesTax * 100,
                        vat = fuel.ValueAddedTax ? 12 : 0
                    }
                };

                // Остальные поля
                fiscalNumber.cash = fuelSale.PaymentType == PaymentType.Cash;
                fiscalNumber.operation = "INCOME";

                _logger.Information("Отправка данных в ККМ: {Data}", JsonSerializer.Serialize(fiscalNumber));

                _cashRegister.Status = await ExecuteOperationAsync($"/api/v2/{_receipt}", fiscalNumber, fuelSale);
                _logger.Information("Продажа завершена. Статус кассы: {Status}", _cashRegister.Status);
                OnStatusChanged?.Invoke(_cashRegister.Status);

                if (_cashRegister.Status == CashRegisterStatus.Open)
                {
                    OnReceiptPrinting?.Invoke();
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при возврате и продаже полученных суммам через ККМ");
            }
            return null;
        }

        #endregion

        #region Private Voids

        /// <summary>
        /// Проверяет доступность интернета посредством ICMP-запроса.
        /// </summary>
        private bool IsInternetConnectionAvailable()
        {
            try
            {
                using Ping ping = new();
                string host = GetDomainFromUrl(_cashRegister.Address);
                PingReply reply = ping.Send(host);
                if (reply.Status != IPStatus.Success)
                {
                    _logger.Error("Сайт не доступен или отсутствует интернет-соединение.");
                    throw new CashRegisterException("Сайт не доступен или отсутствует интернет-соединение.");
                }
                return reply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Извлекает доменное имя из URL.
        /// </summary>
        private string GetDomainFromUrl(string url)
        {
            try
            {
                Uri uri = new(url);
                return uri.Host;
            }
            catch (UriFormatException e)
            {
                _logger.Error(e, "Неверный адрес URL адрес ККМ");
                return string.Empty;
            }
        }

        /// <summary>
        /// Выполняет POST-запрос с заданным API и данными.
        /// </summary>
        private async Task<HttpResponseMessage> PostAsync(string api, object payload)
        {
            string jsonData = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            return await _client.PostAsync(api, content);
        }

        /// <summary>
        /// Универсальный метод для проведения транзакционных операций (продажа, возврат).
        /// </summary>
        private async Task<CashRegisterStatus> ExecuteOperationAsync(string apiEndpoint, object payload, FuelSale? fuelSale = null, bool printReceipt = true)
        {
            if (_client == null)
            {
                _logger.Error("Соединение не установлено. Проверьте правильность заполнения полей 'Пользователь' и 'Пароль'. [{Timestamp}]", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
                await LoginAsync();
                return CashRegisterStatus.Error;
            }

            _check = string.Empty;
            _url = string.Empty;

            string jsonData = JsonSerializer.Serialize(payload);
            _logger.Debug("Отправка запроса: {ApiEndpoint} | Данные: {JsonData}", apiEndpoint, jsonData);

            HttpResponseMessage response = await PostAsync(apiEndpoint, payload);
            string responseContent = await response.Content.ReadAsStringAsync();

            _logger.Debug("Ответ от ККМ: {ResponseContent}", responseContent);

            try
            {
                JsonElement jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (response.IsSuccessStatusCode)
                {
                    if (jsonResponse.TryGetProperty("data", out JsonElement data))
                    {
                        if (data.TryGetProperty("html", out JsonElement html))
                        {
                            //await PrintHtml(html.ToString());
                        }

                        if (data.TryGetProperty("txt", out JsonElement txt))
                        {
                            _check = txt.ToString();

                            if (data.TryGetProperty("link", out JsonElement link))
                            {
                                _url = link.ToString();
                            }

                            if (printReceipt)
                            {
                                PrintText();
                            }
                        }

                        if (jsonResponse.TryGetProperty("message", out JsonElement message))
                        {
                            switch (message.ToString())
                            {
                                case _openShift:
                                    _logger.Information("Смена успешно открыта.");
                                    return CashRegisterStatus.Open;
                                case _closeShift:
                                    _logger.Information("Смена успешно закрыта.");
                                    return CashRegisterStatus.Close;
                                case _receipt:
                                    await CreateFiscalDataAsync(fuelSale, data);
                                    _logger.Information("Чек успешно сформирован.");
                                    return CashRegisterStatus.Open;
                                case _xReport:
                                    return CashRegisterStatus.Open;
                                default:
                                    return CashRegisterStatus.Unknown;
                            }
                        }
                    }
                }
                else
                {
                    if (jsonResponse.TryGetProperty("message", out JsonElement message))
                    {
                        string messageText = message.GetString() ?? string.Empty;
                        string errorMessage = messageText;

                        // Попробуем разобрать message как JSON-объект, если это возможно
                        try
                        {
                            using JsonDocument errorDoc = JsonDocument.Parse(messageText);
                            if (errorDoc.RootElement.TryGetProperty("error", out JsonElement error) && error.ValueKind == JsonValueKind.String)
                            {
                                errorMessage = error.GetString();
                            }
                        }
                        catch (JsonException)
                        {
                            // Если не удалось разобрать, значит message - это просто строка ошибки
                        }

                        _logger.Warning("Ошибка ККМ: {error}", errorMessage);

                        CashRegisterStatus status = errorMessage switch
                        {
                            "Shift already opened" => CashRegisterStatus.Open,
                            "No opened shift" => CashRegisterStatus.Close,
                            "Shift must be closed" => CashRegisterStatus.Exceeded24Hours,
                            _ => CashRegisterStatus.Unknown
                        };

                        if (status == CashRegisterStatus.Unknown)
                        {
                            OnUnknownError?.Invoke($"Ошибка eKassa. {errorMessage}");
                        }

                        return status;
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.Error(ex, "Ошибка при обработке JSON-ответа от ККМ.");
            }
            
            return CashRegisterStatus.Unknown;
        }

        /// <summary>
        /// Авторизация на сервере ЕКасса.
        /// </summary>
        private async Task LoginAsync()
        {
            if (!IsInternetConnectionAvailable())
            {
                return;
            }

            _client = new HttpClient
            {
                BaseAddress = new Uri(_cashRegister.Address)
            };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var loginData = new
            {
                email = _cashRegister.UserName,
                password = _cashRegister.Password
            };

            HttpResponseMessage loginResponse = await PostAsync("/api/auth/login", loginData);
            if (loginResponse.IsSuccessStatusCode)
            {
                var loginResult = JsonSerializer.Deserialize<JsonElement>(await loginResponse.Content.ReadAsStringAsync());
                if (loginResult.TryGetProperty("status", out _) &&
                    loginResult.TryGetProperty("data", out JsonElement data) &&
                    data.TryGetProperty("access_token", out var token))
                {
                    _cashRegister.Token = token.GetString();
                    _client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _cashRegister.Token);
                }
            }
            else
            {
                _logger.Error("Соединение не установлено. Проверьте правильность заполнения полей 'Пользователь' и 'Пароль'. [{Timestamp}]", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
                throw new CashRegisterException("Ошибка авторизации. Проверьте правильность заполнения полей 'Пользователь' и 'Пароль'.");
            }
        }

        private void PrintText()
        {
            try
            {
                // Генерация QR-кода в виде Bitmap с помощью QRCoder
                Bitmap? qrCodeImage = GenerateQrCodeBitmap();

                // Настройка документа для печати
                PrintDocument printDoc = new PrintDocument();

                // Устанавливаем поля равными нулю, чтобы печатать без отступов
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                printDoc.PrinterSettings.PrinterName = _settings.DefaultPrinterName;
                printDoc.PrintPage += (sender, e) =>
                {
                    // Настройка графики для резкого масштабирования
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                    float xPos = e.PageBounds.Left;
                    float yPos = e.PageBounds.Top;

                    Font textFont = _settings.TapeType is TapeType.TXT
                        ? new Font("Courier New", 6f)
                        : new Font("Courier New", 7f);

                    SizeF textSize = e.Graphics.MeasureString(_check, textFont, (int)e.PageBounds.Width);
                    RectangleF textRect = new RectangleF(xPos, yPos, e.PageBounds.Width, textSize.Height);
                    e.Graphics.DrawString(_check, textFont, Brushes.Black, textRect);

                    yPos += textSize.Height + 10; // Отступ между текстом и QR‑кодом

                    if (qrCodeImage != null)
                    {
                        // Например, установить QR‑код в прямоугольник 140x140 пикселей
                        // При этом важно, что размер указывается не в миллиметрах, а в пикселях.
                        int imageWidth = 140;
                        int imageHeight = 140;
                        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        e.Graphics.DrawImage(qrCodeImage, xPos, yPos, imageWidth, imageHeight);
                    }
                };

                // Печать документа
                printDoc.Print();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Ошибка во время печати страницы");
            }
        }

        private Bitmap? GenerateQrCodeBitmap()
        {
            if (string.IsNullOrEmpty(_url))
            {
                return null;
            }

            // Создаём генератор QR-кодов
            var qrGenerator = new QRCodeGenerator();
            // Создаём данные QR-кода
            var qrCodeData = qrGenerator.CreateQrCode(_url, QRCodeGenerator.ECCLevel.M);

            // Используем специализированный класс для получения PNG в виде массива байт
            var qrCode = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(30);

            // Преобразуем массив байт в Bitmap
            using var ms = new MemoryStream(qrCodeBytes);
            var bmp = new Bitmap(ms);

            // Устанавливаем разрешение Bitmap, чтобы соответствовало DPI принтера (например, 203 DPI)
            bmp.SetResolution(203, 203);
            return bmp;
        }

        private Bitmap ScaleImage(Bitmap original, int scaleFactor)
        {
            int width = original.Width * scaleFactor;
            int height = original.Height * scaleFactor;
            Bitmap scaled = new Bitmap(width, height);
            // Сохраняем оригинальное разрешение
            scaled.SetResolution(original.HorizontalResolution, original.VerticalResolution);

            using (Graphics g = Graphics.FromImage(scaled))
            {
                // Режим NearestNeighbor для резкого масштабирования
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(original, new Rectangle(0, 0, width, height));
            }

            return scaled;
        }

        #endregion

        #region Fiscal Documents

        private async Task<string> GetDuplicate(int? fiscalDocument)
        {
            if (fiscalDocument == null)
            {
                return string.Empty;
            }

            dynamic duplicate = new ExpandoObject();
            duplicate.fiscal_number = _cashRegister.RegistrationNumber;
            duplicate.fd_number = fiscalDocument;

            if (_settings.TapeType == TapeType.TXT)
            {
                duplicate.txt = true;
            }
            else
            {
                duplicate.txt80 = true;
            }

            HttpResponseMessage duplicateMessage = await _client.PostAsync("/api/duplicate", new StringContent(JsonSerializer.Serialize(duplicate), Encoding.UTF8, "application/json"));
            if (duplicateMessage.IsSuccessStatusCode)
            {
                var duplicateResult = JsonSerializer.Deserialize<JsonElement>(await duplicateMessage.Content.ReadAsStringAsync());
                if (duplicateResult.TryGetProperty("data", out JsonElement data) &&
                    data.TryGetProperty("html", out JsonElement html))
                {
                    return html.ToString();
                }
            }
            return string.Empty;
        }

        private async Task CreateFiscalDataAsync(FuelSale? fuelSale, JsonElement data)
        {
            if (fuelSale == null)
            {
                return;
            }

            int? fiscalDocument = GetFiscalDocument(data);

            if (fuelSale.FiscalData == null)
            {
                fuelSale.FiscalData = new()
                {
                    FiscalModule = GetFiscalModule(data),
                    FiscalDocument = fiscalDocument,
                    RegistrationNumber = GetFiscalNumber(data),
                    Check = await GetDuplicate(fiscalDocument)
                };
            }
            else
            {
                fuelSale.FiscalData.ReturnCheck = await GetDuplicate(fiscalDocument);
            }
        }

        private int? GetFiscalDocument(JsonElement data)
        {
            if (data.TryGetProperty("fields", out JsonElement fields) &&
                fields.TryGetProperty("1040", out JsonElement fd))
            {
                string fiscalDocumentString = fd.ToString();
                if (int.TryParse(fiscalDocumentString, out int fiscalDocument))
                {
                    return fiscalDocument;
                }
            }
            return null;
        }

        private string? GetFiscalModule(JsonElement data)
        {
            if (data.TryGetProperty("fields", out JsonElement fields) &&
                fields.TryGetProperty("1041", out JsonElement fd))
            {
                return fd.ToString();
            }
            return null;
        }

        private string? GetFiscalNumber(JsonElement data)
        {
            if (data.TryGetProperty("fiscalNumber", out JsonElement fn))
            {
                return fn.ToString();
            }
            return null;
        }

        #endregion

        #region Logs

        /// <summary>
        /// Инициализация логгера.
        /// </summary>
        private void InitLog()
        {
            // 1. Создадим/убедимся, что существует папка logs
            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);

            // 2. Формируем имя файла. Можно добавить время, 
            //    но обязательно без «:» (двоеточий). Например, yyyy-MM-dd_HH-mm-ss.
            var logFilePath = Path.Combine(logsDir, $"{nameof(EKassaCashRegister)}_{DateTime.Now:dd.MM.yyyy}.log");

            // 3. Настраиваем Serilog
            _logger = new LoggerConfiguration()
                // Указываем минимальный уровень
                .MinimumLevel.Debug()
                // Пишем в файл с «дневным» ротационным интервалом
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    // Можно задать, сколько файлов хранить
                    retainedFileCountLimit: 7,
                    // Можно включить автопереход на новый файл при достижении лимита размера
                    rollOnFileSizeLimit: true
                )
                // При желании можно добавить вывод в консоль
                //.WriteTo.Console()
                .CreateLogger();

            // 4. Пробный лог на уровне Information
            _logger.Information("=======================ЕКасса инициализирован=======================");
        }

        #endregion
    }
}
