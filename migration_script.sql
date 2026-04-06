-- =============================================
-- Миграция: AddNumberToShiftAndFuelSale
-- Дата: 2026-04-04
-- Описание: Добавляет таблицу DocumentCounters,
--   колонки Number/Year в Shifts, Number/SaleDay в FuelSales,
--   ретро-нумерует существующие записи,
--   создаёт уникальные индексы.
-- БЕЗОПАСНО: не удаляет данные, только добавляет.
-- =============================================

BEGIN TRANSACTION;
GO

-- ========== 1. Таблица счётчиков документов ==========
CREATE TABLE [DocumentCounters] (
    [DocumentType] nvarchar(50) NOT NULL,
    [PeriodKey] nvarchar(20) NOT NULL,
    [CurrentValue] int NOT NULL DEFAULT 0,
    CONSTRAINT [PK_DocumentCounters] PRIMARY KEY ([DocumentType], [PeriodKey])
);
GO

-- ========== 2. Новые колонки в Shifts ==========
ALTER TABLE [Shifts] ADD [Number] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Shifts] ADD [Year] int NOT NULL DEFAULT 0;
GO

-- ========== 3. Новые колонки в FuelSales ==========
ALTER TABLE [FuelSales] ADD [Number] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [FuelSales] ADD [SaleDay] date NOT NULL DEFAULT '0001-01-01';
GO

-- ========== 4. Ретро-нумерация смен (по годам) ==========

-- Проставляем Year
UPDATE [Shifts] SET [Year] = YEAR(OpeningDate);

-- Проставляем Number с нумерацией внутри каждого года
;WITH cte AS (
    SELECT Id,
           ROW_NUMBER() OVER (PARTITION BY YEAR(OpeningDate) ORDER BY OpeningDate, Id) AS RowNum
    FROM [Shifts]
)
UPDATE s
SET s.[Number] = cte.RowNum
FROM [Shifts] s
INNER JOIN cte ON s.Id = cte.Id;

-- Заполняем DocumentCounters для смен
INSERT INTO [DocumentCounters] (DocumentType, PeriodKey, CurrentValue)
SELECT 'Shift', CAST([Year] AS NVARCHAR(20)), MAX([Number])
FROM [Shifts]
GROUP BY [Year];
GO

-- ========== 5. Ретро-нумерация продаж (по дням) ==========

-- Проставляем SaleDay
UPDATE [FuelSales] SET [SaleDay] = CAST(CreateDate AS DATE);

-- Проставляем Number с нумерацией внутри каждого дня
;WITH cte AS (
    SELECT Id,
           ROW_NUMBER() OVER (PARTITION BY CAST(CreateDate AS DATE) ORDER BY CreateDate, Id) AS RowNum
    FROM [FuelSales]
)
UPDATE f
SET f.[Number] = cte.RowNum
FROM [FuelSales] f
INNER JOIN cte ON f.Id = cte.Id;

-- Заполняем DocumentCounters для продаж
INSERT INTO [DocumentCounters] (DocumentType, PeriodKey, CurrentValue)
SELECT 'FuelSale', CONVERT(NVARCHAR(10), [SaleDay], 23), MAX([Number])
FROM [FuelSales]
GROUP BY [SaleDay];
GO

-- ========== 6. Уникальные индексы ==========
CREATE UNIQUE INDEX [IX_Shifts_Year_Number] ON [Shifts] ([Year], [Number]);
GO

CREATE UNIQUE INDEX [IX_FuelSales_SaleDay_Number] ON [FuelSales] ([SaleDay], [Number]);
GO

-- ========== 7. Регистрация миграции в EF ==========
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260404120000_AddNumberToShiftAndFuelSale', N'7.0.20');
GO

COMMIT TRANSACTION;
GO
