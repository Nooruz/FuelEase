using KIT.GasStation.CashRegisters.Exceptions;
using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using QRCoder;
using Serilog;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Dynamic;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;


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

            var shiftOpen = new ShiftOpen
            {
                FiscalNumber = _cashRegister.RegistrationNumber,

            };

            switch (_settings.TapeType)
            {
                case TapeType.TXT:
                    shiftOpen.Txt = true;
                    break;
                case TapeType.TXT80:
                    shiftOpen.Txt80 = true;
                    break;
            }

            _cashRegister.Status = await ExecuteOperationAsync($"/api/{_openShift}", shiftOpen);
            _logger.Information("Статус кассы после открытия смены: {Status}", _cashRegister.Status);
            OnStatusChanged?.Invoke(_cashRegister.Status);
            if (_cashRegister.Status == CashRegisterStatus.Open)
            {
                OnShiftOpened?.Invoke();
            }
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> SaleAsync(FuelSale fuelSale, Fuel fuel, string cashierName, bool isBefore = true)
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

            _cashRegister.Status = await ExecuteOperationAsync($"/api/{_xReport}", fiscalNumber, printReceipt: false);
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

                        }

                        if (jsonResponse.TryGetProperty("message", out JsonElement message))
                        {
                            string messageText = message.ToString();

                            if (printReceipt)
                            {
                                PrintText(includeQr: messageText == _receipt);
                            }

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

            var loginData = new Login
            {
                Email = _cashRegister.UserName,
                Password = _cashRegister.Password
            };

            HttpResponseMessage loginResponse = await PostAsync("/api/auth/login", loginData);

            if (!loginResponse.IsSuccessStatusCode)
            {
                _logger.Error("HTTP ошибка авторизации: {StatusCode}", loginResponse.StatusCode);
                throw new CashRegisterException("Ошибка соединения с сервером");
            }

            var json = await loginResponse.Content.ReadAsStringAsync();

            var response = JsonSerializer.Deserialize<ApiResponse<LoginResult>>(json);

            if (response == null)
            {
                _logger.Error("Ответ от сервера пустой [{Timestamp}]",
                    DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
                throw new CashRegisterException("Пустой ответ сервера");
            }

            if (response.Status != ResponseStatus.Success)
            {
                // аккуратно вытаскиваем текст ошибки
                string errorText = response.Message.ValueKind switch
                {
                    JsonValueKind.String => response.Message.GetString(),
                    JsonValueKind.Object => response.Message.GetProperty("error").GetString(),
                    _ => "Неизвестная ошибка"
                };

                throw new CashRegisterException(errorText);
            }

            LoginResult? loginResult = response.Data;

            if (loginResult == null)
            {
                _logger.Error("Ошибка авторизации. Ответ от сервера пустой. [{Timestamp}]",
                    DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
                throw new CashRegisterException("Ошибка авторизации. Ответ от сервера пустой.");
            }

            _cashRegister.Token = loginResult.AccessToken;
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(loginResult.TokenType, _cashRegister.Token);
        }

        private void PrintText(bool includeQr)
        {
            try
            {
                PrintDocument printDoc = new PrintDocument();
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                printDoc.PrinterSettings.PrinterName = _settings.DefaultPrinterName;

                // Установим размер бумаги: 58мм или 80мм × большая высота (не ограничиваем)
                int paperWidthMm = GetPaperWidthMm(_settings.TapeType);
                int paperWidth = (int)(paperWidthMm / 25.4 * 100); // 1/100 дюйма
                int paperHeight = 800; // 50 дюймов - достаточно для любого чека
                printDoc.DefaultPageSettings.PaperSize = new PaperSize("Custom", paperWidth, paperHeight);

                printDoc.PrintPage += (sender, e) =>
                {
                    int printerDpi = 203;

                    LayoutInfo layout = CalculateLayout(printerDpi, paperWidthMm);

                    float yPos = layout.TopMargin;

                    // Уменьшим размер шрифта до 6pt
                    Font textFont = new Font("Courier New", 6f, FontStyle.Regular);

                    IEnumerable<string> lines = NormalizeLines(_check ?? string.Empty, e.Graphics, textFont, layout.ContentWidth);
                    DrawTextLines(e.Graphics, textFont, layout.ContentWidth, layout.LeftMargin, ref yPos, lines);

                    float textHeight = yPos - layout.TopMargin;

                    bool canIncludeQr = includeQr && !string.IsNullOrWhiteSpace(_url);
                    bool qrDrawn = false;
                    float qrX = 0f;
                    float qrY = 0f;
                    float qrWidth = 0f;

                    if (canIncludeQr)
                    {
                        yPos += layout.TextQrSpacing;
                        using Bitmap qrCodeImage = GenerateQrCodeForThermalPrinter(_url, layout.TargetQrSize);
                        if (qrCodeImage != null)
                        {
                            DrawQr(e.Graphics, qrCodeImage, layout.ContentWidth, layout.LeftMargin, layout.BottomMargin, layout.PrinterDpi, ref yPos,
                                out qrX, out qrY, out qrWidth);
                            qrDrawn = true;
                        }
                        else
                        {
                            yPos += layout.BottomMargin;
                        }
                    }
                    else
                    {
                        yPos += layout.BottomMargin;
                    }

                    // Логируем итоговые параметры
                    _logger.Information($"Расположение элементов:");
                    _logger.Information($"- Текст: начальная позиция {layout.TopMargin}, высота {textHeight}");
                    if (qrDrawn)
                    {
                        _logger.Information($"- QR-код: x={qrX:F2}, y={qrY:F2}, размер={qrWidth:F2}x{qrWidth:F2}");
                    }
                    else
                    {
                        _logger.Information("- QR-код: не печатается");
                    }

                    _logger.Information($"- Общая высота чека: {yPos:F2} (1/100 дюйма)");
                    _logger.Information($"- Это примерно: {yPos / 100 * 25.4:F1} мм");

                    textFont.Dispose();

                    e.HasMorePages = false; // Только одна страница
                };

                printDoc.Print();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка во время печати страницы");
            }
        }

        private LayoutInfo CalculateLayout(int printerDpi, int paperWidthMm)
        {
            // Немного уменьшим отступы
            int leftRightMarginMm = paperWidthMm <= 58 ? 3 : 6;
            int topBottomMarginMm = 10;  // 10мм сверху и снизу

            // Конвертируем мм в единицы принтера (dots)
            int leftMarginDots = (int)(leftRightMarginMm * printerDpi / 25.4);
            int rightMarginDots = (int)(leftRightMarginMm * printerDpi / 25.4);
            int topMarginDots = (int)(topBottomMarginMm * printerDpi / 25.4);
            int bottomMarginDots = (int)(topBottomMarginMm * printerDpi / 25.4);

            // Конвертируем dots в 1/100 дюйма для Graphics
            float leftMargin = leftMarginDots / (float)printerDpi * 100;
            float rightMargin = rightMarginDots / (float)printerDpi * 100;
            float topMargin = topMarginDots / (float)printerDpi * 100;
            float bottomMargin = bottomMarginDots / (float)printerDpi * 100;

            // Ширина бумаги в 1/100 дюйма
            float paperWidthInches = paperWidthMm / 25.4f;
            float printWidth = paperWidthInches * 100;

            // Уменьшим коэффициент безопасности
            float safetyFactor = 0.98f; // Уменьшили до 0.98
            float contentWidth = (printWidth - leftMargin - rightMargin) * safetyFactor;

            _logger.Information($"Ширина бумаги: {printWidth} (1/100 дюйма)");
            _logger.Information($"Доступная ширина контента: {contentWidth} (с учетом коэффициента безопасности {safetyFactor})");
            _logger.Information($"Отступы: слева={leftMargin}, справа={rightMargin}, сверху={topMargin}, снизу={bottomMargin}");

            // Размер QR-кода - занимает всю доступную ширину (но не более 300 точек)
            int maxQrSize = paperWidthMm <= 58 ? 200 : 250; // Уменьшили до 300
            int targetQrSize = Math.Min((int)(contentWidth / 100 * printerDpi), maxQrSize);

            _logger.Information($"Размер QR-кода: {targetQrSize}x{targetQrSize} dots");

            // Отступ между текстом и QR-кодом (в мм, конвертируем в 1/100 дюйма)
            int textQrSpacingMm = 5;
            float textQrSpacing = textQrSpacingMm / 25.4f * 100;

            return new LayoutInfo(printerDpi, leftMargin, rightMargin, topMargin, bottomMargin, contentWidth, printWidth, textQrSpacing, targetQrSize);
        }

        private IEnumerable<string> NormalizeLines(string text, Graphics graphics, Font font, float contentWidth)
        {
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string longestLine = "";
            float maxLineWidth = 0;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    SizeF lineSize = graphics.MeasureString(trimmedLine, font);
                    if (lineSize.Width > maxLineWidth)
                    {
                        maxLineWidth = lineSize.Width;
                        longestLine = trimmedLine;
                    }
                }
            }

            _logger.Information($"Самая длинная строка: '{longestLine}'");
            _logger.Information($"Ширина самой длинной строки: {maxLineWidth}, доступная ширина: {contentWidth}");

            if (maxLineWidth > contentWidth)
            {
                _logger.Warning($"Строка не помещается! Превышение: {maxLineWidth - contentWidth}");
            }

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine))
                {
                    yield return string.Empty;
                    continue;
                }

                SizeF lineSize = graphics.MeasureString(trimmedLine, font);

                if (trimmedLine.StartsWith("-") && trimmedLine.Length > 1 && trimmedLine.All(c => c == '-'))
                {
                    int targetChars = (int)(contentWidth / (lineSize.Width / trimmedLine.Length));
                    if (targetChars < 3) targetChars = 3;
                    trimmedLine = new string('-', targetChars);
                    lineSize = graphics.MeasureString(trimmedLine, font);
                }

                if (lineSize.Width > contentWidth)
                {
                    string currentText = trimmedLine;
                    while (currentText.Length > 3)
                    {
                        currentText = currentText.Substring(0, currentText.Length - 1);
                        lineSize = graphics.MeasureString(currentText, font);
                        if (lineSize.Width <= contentWidth)
                        {
                            trimmedLine = currentText;
                            break;
                        }
                    }

                    _logger.Information($"Обрезана строка до: '{trimmedLine}'");
                }

                yield return trimmedLine;
            }
        }

        private void DrawTextLines(Graphics graphics, Font font, float contentWidth, float leftMargin, ref float yPos, IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    yPos += font.GetHeight(graphics);
                    continue;
                }

                SizeF lineSize = graphics.MeasureString(line, font);
                graphics.DrawString(line, font, Brushes.Black, leftMargin, yPos);
                yPos += lineSize.Height;
            }
        }

        private void DrawQr(
            Graphics graphics,
            Bitmap qrCodeImage,
            float contentWidth,
            float leftMargin,
            float bottomMargin,
            int printerDpi,
            ref float yPos,
            out float qrX,
            out float qrY,
            out float qrWidth)
        {
            qrWidth = qrCodeImage.Width / (float)printerDpi * 100;
            float qrHeight = qrWidth;

            qrX = leftMargin + (contentWidth - qrWidth) / 2;
            if (qrX < leftMargin)
            {
                qrX = leftMargin;
            }
            qrY = yPos;

            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            graphics.DrawImage(qrCodeImage, qrX, qrY, qrWidth, qrHeight);
            yPos += qrHeight + bottomMargin;
        }

        private sealed record LayoutInfo(
            int PrinterDpi,
            float LeftMargin,
            float RightMargin,
            float TopMargin,
            float BottomMargin,
            float ContentWidth,
            float PrintWidth,
            float TextQrSpacing,
            int TargetQrSize);

        private Bitmap GenerateQrCodeForThermalPrinter(string url, int sizeInPixels)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            var gen = new QRCodeGenerator();
            var data = gen.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);

            using var qr = new QRCode(data);

            // Генерируем QR-код нужного размера
            int modules = data.ModuleMatrix.Count;
            int pixelsPerModule = Math.Max(1, sizeInPixels / modules);

            var bmp = qr.GetGraphic(
                pixelsPerModule: pixelsPerModule,
                darkColor: Color.Black,
                lightColor: Color.White,
                drawQuietZones: true);

            bmp.SetResolution(203, 203);

            // Если изображение не точно нужного размера, ресайзим
            if (bmp.Width != sizeInPixels || bmp.Height != sizeInPixels)
            {
                var resized = new Bitmap(sizeInPixels, sizeInPixels, PixelFormat.Format24bppRgb);
                resized.SetResolution(203, 203);

                using (var g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.DrawImage(bmp, 0, 0, sizeInPixels, sizeInPixels);
                }

                bmp.Dispose();
                return resized;
            }

            return bmp;
        }

        private int GetPaperWidthMm(TapeType tapeType)
        {
            return tapeType switch
            {
                TapeType.TXT => 58,
                TapeType.TXT80 => 80,
                _ => 80
            };
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
                //fuelSale.FiscalData.ReturnCheck = await GetDuplicate(fiscalDocument);
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
