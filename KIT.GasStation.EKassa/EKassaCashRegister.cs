using KIT.GasStation.CashRegisters.Exceptions;
using KIT.GasStation.CashRegisters.Models;
using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Models.CashRegisters;
using KIT.GasStation.EKassa.Models;
using KIT.GasStation.EKassa.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using QRCoder;
using Serilog;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
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
        private EkassaClient _client;

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

            var request = new ShiftCloseRequest
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Txt = _settings.TapeType == TapeType.TXT ? true : null,
                Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null,
            };

            var data = await _client.ShiftCloseAsync(request);

            PrintText(data.Txt);
        }

        /// <inheritdoc/>
        public async Task InitializationAsync(Guid cashRegisterId)
        {
            CashRegister? cashRegister = await _hardwareConfigurationService.GetCashRegisterAsync(cashRegisterId);

            if (cashRegister == null)
            {
                _logger.Error("ККМ не найдена. Id={CashRegisterId}", cashRegisterId);
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

            var request = new ShiftOpenRequest
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Txt = _settings.TapeType == TapeType.TXT ? true : null,
                Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null,
            };

            try
            {
                var data = await _client.ShiftOpenAsync(request);
                PrintText(data.Txt);
            }
            catch (EkassaHttpException e) when (e.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                // 403 = "Shift already opened" — смена уже открыта на кассе, не ошибка
                _logger.Warning("Смена ККМ уже открыта. Код: {EkassaCode}, Сообщение: {EkassaError}", e.EkassaCode, e.EkassaError);
            }
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> SaleAsync(FiscalData fiscalData, string cashierName)
        {
            _logger.Information("Начало продажи через ККМ. Сумма: {Sum}, Топливо: {Fuel}", fiscalData.Total, fiscalData.FuelName);

            int price = (int)(fiscalData.Price * 100);
            decimal quantity = Math.Round(fiscalData.Total / fiscalData.Price, 6);
            string? discount = null; // null = не отправлять, API не принимает пустую строку

            //if (fuelSale.DiscountSale != null)
            //{
            //    quantity = Math.Round(fuelSale.Sum / fuelSale.DiscountSale.DiscountPrice, 6);
            //    price = (int)(fuelSale.DiscountSale.DiscountPrice * 100);
            //    discount = (((quantity * fuelSale.Price) - fuelSale.Sum) * 100).ToString();
            //}

            var goods = new List<ReceiptGood>()
            {
                new()
                {
                    CalcItemAttributeCode = 0,
                    Name = fiscalData.FuelName,
                    Sgtin = string.IsNullOrEmpty(fiscalData.Tnved) ? null : fiscalData.Tnved,
                    Price = price,
                    Quantity = quantity,
                    Unit = fiscalData.UnitOfMeasurement,
                    St = (int)(fiscalData.SalesTax * 100),
                    Vat = fiscalData.ValueAddedTax ? 12 : 0
                }
            };

            var request = new ReceiptV2Request()
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Goods = goods,
                Discount = discount,
                Cash = fiscalData.PaymentType == PaymentType.Cash,
                Operation = ReceiptOperation.INCOME,
                Txt = _settings.TapeType == TapeType.TXT ? true : null,
                Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null,
            };

            var newfiscalData = await _client.CreateReceiptV2Async(request);

            _logger.Information("Отправка данных в ККМ: {Data}", JsonSerializer.Serialize(request, EkassaJson.Options));

            fiscalData.FiscalModule = GetFiscalModule(newfiscalData);
            fiscalData.FiscalDocument = GetFiscalDocument(newfiscalData);
            fiscalData.RegistrationNumber = newfiscalData.FiscalNumber.ToString();

            PrintText(newfiscalData.Txt, newfiscalData.Link);

            return fiscalData;
        }

        /// <inheritdoc/>
        public async Task XReportAsync(bool printReceipt = true)
        {
            _logger.Information("Получение X-отчёта...");

            var request = new ShiftStateRequest
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Txt = _settings.TapeType == TapeType.TXT ? true : null,
                Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null
            };

            var shiftReportData = await _client.ShiftStateAsync(request);

            if (shiftReportData != null)
            {
                PrintText(shiftReportData.Txt);
            }
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> ReturnAsync(FiscalData fiscalData)
        {
            _logger.Information("Начало возврата через ККМ. Сумма: {Sum}, Топливо: {Fuel}", fiscalData.Total, fiscalData.FuelName);

            var receipt = new ReceiptV2Request
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Goods = new List<ReceiptGood>
                {
                    new()
                    {
                        CalcItemAttributeCode = 0,
                        Name = fiscalData.FuelName,
                        Sgtin = string.IsNullOrEmpty(fiscalData.Tnved) ? null : fiscalData.Tnved,
                        Price = (int)(fiscalData.Price * 100),
                        Quantity = Math.Round(fiscalData.Total / fiscalData.Price, 6),
                        Unit = fiscalData.UnitOfMeasurement,
                        St = (int)(fiscalData.SalesTax * 100),
                        Vat = fiscalData.ValueAddedTax ? 12 : 0
                    }
                },
                Cash = fiscalData.PaymentType == PaymentType.Cash,
                Operation = ReceiptOperation.INCOME_RETURN,
                OriginFdNumber = fiscalData.FiscalDocument,
                Txt = _settings.TapeType == TapeType.TXT ? true : null,
                Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null,
            };

            var newfiscalData = await _client.CreateReceiptV2Async(receipt);

            _logger.Information("Отправка данных в ККМ: {Data}", JsonSerializer.Serialize(receipt));

            fiscalData.FiscalModule = GetFiscalModule(newfiscalData);
            fiscalData.FiscalDocument = GetFiscalDocument(newfiscalData);
            fiscalData.RegistrationNumber = newfiscalData.FiscalNumber.ToString();

            PrintText(newfiscalData.Txt, newfiscalData.Link);

            return fiscalData;
        }

        /// <inheritdoc/>
        public async Task<CashRegisterState> GetShiftStateAsync()
        {
            try
            {
                _logger.Information("Получение статуса смены ККМ...");

                // Используем get_pos_by_fiscal_number вместо shift_state_by_fiscal_number:
                // ответ уже содержит shift_state (1=открыта, 0=закрыта) и shift_date.
                var request = new GetPosByFiscalNumberRequest
                {
                    FiscalNumber = _cashRegister.RegistrationNumber
                };

                var posInfo = await _client.GetPosByFiscalNumberAsync(request);

                // shift_state: 1 = смена открыта, 0 = закрыта
                if (posInfo.ShiftState == 1 && !string.IsNullOrWhiteSpace(posInfo.ShiftDate))
                {
                    if (DateTime.TryParse(posInfo.ShiftDate, out var openDate))
                    {
                        _logger.Information("Смена ККМ открыта с {OpenDate}", openDate);
                        return new CashRegisterState { OpenedAt = openDate };
                    }
                    _logger.Information("Смена ККМ открыта (дата не распознана: {ShiftDate})", posInfo.ShiftDate);
                    return new CashRegisterState { OpenedAt = DateTime.Now };
                }

                _logger.Information("Смена ККМ закрыта (shift_state={ShiftState})", posInfo.ShiftState);
                return new CashRegisterState { Status = CashRegisterStatus.Close };
            }
            catch (EkassaHttpException e)
            {
                _logger.Error(e, "Ошибка при получении статуса смены ККМ. Код: {StatusCode}, Сообщение: {EkassaError}", e.StatusCode, e.EkassaError);
                return new CashRegisterState { Status = CashRegisterStatus.Close };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Ошибка при получении статуса смены ККМ.");
                return new CashRegisterState();
            }
        }

        /// <inheritdoc/>
        public async Task<ShiftSalesReport> GetShiftSalesReportAsync()
        {
            try
            {
                _logger.Information("Получение отчёта по продажам за смену...");

                var request = new GetPosByFiscalNumberRequest
                {
                    FiscalNumber = _cashRegister.RegistrationNumber
                };

                var posInfo = await _client.GetPosByFiscalNumberAsync(request);

                var report = new ShiftSalesReport
                {
                    SaleReceiptCount = GetExtraInt(posInfo, "shift_reciept_prihod"),
                    CashSaleSum = GetExtraTiyinAsDecimal(posInfo, "shift_cash_prihod"),
                    CashlessSaleSum = GetExtraTiyinAsDecimal(posInfo, "shift_bank_prihod"),

                    ReturnReceiptCount = GetExtraInt(posInfo, "shift_reciept_prihod_vozvrat"),
                    CashReturnSum = GetExtraTiyinAsDecimal(posInfo, "shift_cash_prihod_vozvrat"),
                    CashlessReturnSum = GetExtraTiyinAsDecimal(posInfo, "shift_bank_prihod_vozvrat"),
                };

                _logger.Information(
                    "Отчёт по смене: продажи нал={CashSale}, безнал={CashlessSale}, возвраты нал={CashReturn}, безнал={CashlessReturn}",
                    report.CashSaleSum, report.CashlessSaleSum, report.CashReturnSum, report.CashlessReturnSum);

                return report;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при получении отчёта по продажам за смену.");
                return new ShiftSalesReport();
            }
        }

        #endregion

        #region Private Voids

        /// <summary>
        /// Извлекает целочисленное значение из ExtensionData ответа PosInfoData.
        /// </summary>
        private static int GetExtraInt(PosInfoData data, string key)
        {
            if (data.Extra.TryGetValue(key, out var el) && el.TryGetInt32(out var val))
                return val;
            return 0;
        }

        /// <summary>
        /// Извлекает сумму из ExtensionData (в тийинах) и конвертирует в сомы (decimal / 100).
        /// </summary>
        private static decimal GetExtraTiyinAsDecimal(PosInfoData data, string key)
        {
            if (data.Extra.TryGetValue(key, out var el))
            {
                if (el.TryGetInt64(out var val))
                    return val / 100m;
                if (el.TryGetDecimal(out var dVal))
                    return dVal / 100m;
            }
            return 0m;
        }

        /// <summary>
        /// Проверяет доступность интернета посредством ICMP-запроса.
        /// </summary>
        private async Task<bool> IsInternetConnectionAvailable()
        {
            try
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };

                using var request = new HttpRequestMessage(HttpMethod.Head, _cashRegister.Address);
                using var response = await client.SendAsync(request);

                // Любой ответ = сервер доступен
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Сервер {Address} недоступен (HTTPS).", _cashRegister.Address);
                return false;
            }
        }

        /// <summary>
        /// Авторизация на сервере ЕКасса.
        /// </summary>
        private async Task LoginAsync()
        {
            if (!await IsInternetConnectionAvailable())
            {
                return;
            }

            var options = new EkassaOptions
            {
                BaseUri = new Uri(_cashRegister.Address),
                Email = _cashRegister.UserName,
                Password = _cashRegister.Password,
                Timeout = TimeSpan.FromSeconds(30),
                MaxAuthRetry = 1
            };

            var tokenStore = new EkassaTokenStore();

            var loginHttp = new HttpClient
            {
                BaseAddress = options.BaseUri,
                Timeout = options.Timeout
            };

            var loginApi = new EkassaLoginApi(loginHttp, options);

            var loginData = await loginApi.LoginAsync(CancellationToken.None);
            tokenStore.SetToken(loginData);

            var authHandler = new EkassaAuthHandler(tokenStore, loginApi)
            {
                InnerHandler = new HttpClientHandler()
            };

            var apiHttp = new HttpClient(authHandler)
            {
                BaseAddress = options.BaseUri,
                Timeout = options.Timeout
            };

            _client = new EkassaClient(apiHttp, loginApi, tokenStore, options);
        }

        private void PrintText(string? check, string? url = null)
        {
            try
            {
                PrintDocument printDoc = new();
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

                    IEnumerable<string> lines = NormalizeLines(check ?? string.Empty, e.Graphics, textFont, layout.ContentWidth);
                    DrawTextLines(e.Graphics, textFont, layout.ContentWidth, layout.LeftMargin, ref yPos, lines);

                    float textHeight = yPos - layout.TopMargin;

                    bool canIncludeQr = !string.IsNullOrWhiteSpace(url);
                    bool qrDrawn = false;
                    float qrX = 0f;
                    float qrY = 0f;
                    float qrWidth = 0f;

                    if (canIncludeQr)
                    {
                        yPos += layout.TextQrSpacing;
                        using Bitmap qrCodeImage = GenerateQrCodeForThermalPrinter(url, layout.TargetQrSize);
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
                    _logger.Information("Расположение элементов:");
                    _logger.Information("- Текст: начальная позиция {TopMargin}, высота {TextHeight}", layout.TopMargin, textHeight);
                    if (qrDrawn)
                    {
                        _logger.Information("- QR-код: x={QrX:F2}, y={QrY:F2}, размер={QrWidth:F2}x{QrWidth:F2}", qrX, qrY, qrWidth);
                    }
                    else
                    {
                        _logger.Information("- QR-код: не печатается");
                    }

                    _logger.Information("- Общая высота чека: {Height:F2} (1/100 дюйма)", yPos);
                    _logger.Information("- Это примерно: {HeightMm:F1} мм", yPos / 100 * 25.4);

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

            _logger.Information("Ширина бумаги: {PrintWidth} (1/100 дюйма)", printWidth);
            _logger.Information("Доступная ширина контента: {ContentWidth} (с учётом коэффициента безопасности {SafetyFactor})", contentWidth, safetyFactor);
            _logger.Information("Отступы: слева={Left}, справа={Right}, сверху={Top}, снизу={Bottom}", leftMargin, rightMargin, topMargin, bottomMargin);

            // Размер QR-кода - занимает всю доступную ширину (но не более 300 точек)
            int maxQrSize = paperWidthMm <= 58 ? 200 : 250; // Уменьшили до 300
            int targetQrSize = Math.Min((int)(contentWidth / 100 * printerDpi), maxQrSize);

            _logger.Information("Размер QR-кода: {QrSize}x{QrSize} dots", targetQrSize);

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

            _logger.Information("Самая длинная строка: '{LongestLine}'", longestLine);
            _logger.Information("Ширина самой длинной строки: {MaxWidth}, доступная ширина: {ContentWidth}", maxLineWidth, contentWidth);

            if (maxLineWidth > contentWidth)
            {
                _logger.Warning("Строка не помещается! Превышение на: {Overflow}", maxLineWidth - contentWidth);
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

                    _logger.Information("Строка обрезана до: '{TrimmedLine}'", trimmedLine);
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

        private int? GetFiscalDocument(ReceiptData data)
        {
            if (data.Fields?.Tags.TryGetValue("1040", out JsonElement fd) == true)
            {
                string fiscalDocumentString = fd.ToString();
                if (int.TryParse(fiscalDocumentString, out int fiscalDocument))
                {
                    return fiscalDocument;
                }
            }
            return null;
        }

        private string? GetFiscalModule(ReceiptData data)
        {
            if (data.Fields?.Tags.TryGetValue("1041", out JsonElement fd) == true)
            {
                return fd.ToString();
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
