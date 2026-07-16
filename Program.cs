// TODO: (melss) Это не последняя версия, не соблюдено требование "Необходимо разработать консольное приложение (CLI) на **.NET 10**".
// TODO: (melss) Проект не собирается и не запускается, ошибка "1>C:\Users\melss\vs_code\SalesAnalyzer\SalesAnalyzer.csproj(90,5): error : Данный проект ссылается на пакеты NuGet, отсутствующие на этом компьютере. Используйте восстановление пакетов NuGet, чтобы скачать их.  Дополнительную информацию см. по адресу: http://go.microsoft.com/fwlink/?LinkID=322105. Отсутствует следующий файл: ..\packages\System.ValueTuple.4.6.2\build\net471\System.ValueTuple.targets."
// TODO: (melss) Зачем здесь зависимость "..\packages\System.ValueTuple.4.6.2\build\net471\System.ValueTuple.targets"?
// TODO: (melss) Что это в файле проекта "<Compile Include="Properties\AssemblyInfo.cs" />"?
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace SalesAnalyzer
{
    internal class Program
    {
        // Шаблон записи о продаже
        public class SaleRecord // TODO: (melss) Почему отклонились от задания в такой мелочи? "- Читать CSV, преобразовывать строки в объекты `Sale`."
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public int CustomerId { get; set; }
            public string ProductCategory { get; set; }
            public string Region { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Discount { get; set; }
            public string PaymentMethod { get; set; }
            public int DeliveryDays { get; set; }
            public double CustomerRating { get; set; }
            public decimal Revenue { get; set; }
        }

        static void Main(string[] args)
        {
            // TODO: (melss) Вообще не понял прикола с 1 или 2. Где ожидаемые аргументы из задания?
            // TODO: (melss) Нету не одной try/catch. Обработки ошибок не предусмотрено.
            // Так как работа с аргументами командной строки не реализована,выбор режима выполняется через простое меню в консоли
            Console.WriteLine("=== Программа анализа продаж ===");
            Console.WriteLine("Выберите режим:");
            Console.WriteLine("  1 - Только чтение и вывод в консоль");
            Console.WriteLine("  2 - Чтение + сохранение результатов в JSON");
            Console.Write("Ваш выбор (1 или 2): ");
            string choice = Console.ReadLine();
            if (choice != "1" && choice != "2")
            {
                Console.WriteLine("Неверный выбор. Завершение.");
                return;
            }
            Console.Write("Введите путь к входному CSV-файлу: ");
            string inputFile = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(inputFile))
            {
                Console.WriteLine("Путь не указан. Завершение.");
                return;
            }
            string outputFile = null;
            if (choice == "2")
            {
                Console.Write("Введите путь для сохранения JSON-файла: ");
                outputFile = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(outputFile))
                {
                    Console.WriteLine("Путь не указан. Завершение.");
                    return;
                }
            }
                // Читаем файл и выводим таблицу
                var sales = ReadSalesFromFile(inputFile);
                PrintSales(sales);
                Console.WriteLine($"\n Всего записей: {sales.Count}");
                // Вывод блока аналитики
                var revenueByCategory = TotalRevenueByCategory(sales);
                Console.WriteLine("\n=== Общая выручка по категориям ===");
                foreach (var kv in revenueByCategory)
                    Console.WriteLine($"{kv.Key,-20} {kv.Value,15:F2}");
                var top5 = Top5ProductsByQuantity(sales);
                Console.WriteLine("\n=== Топ-5 категорий по количеству продаж ===");
                foreach (var kv in top5)
                    Console.WriteLine($"{kv.Key,-20} {kv.Value,10} шт.");
                var avgPricePerMonth = AveragePricePerMonth(sales);
                Console.WriteLine("\n=== Средняя цена за месяц ===");
                foreach (var kv in avgPricePerMonth)
                    Console.WriteLine($"{kv.Key,-15:MMMM yyyy} {kv.Value,10:F2}");
                // Если выбран режим 2, сохраняем JSON
                if (choice == "2")
                {
                    var result = new
                    {
                        TotalRevenueByCategory = revenueByCategory,
                        Top5ProductsByQuantity = top5,
                        AveragePricePerMonth = avgPricePerMonth
                    };
                    SaveToJson(outputFile, result);
                }
                Console.WriteLine("\nНажмите любую клавишу для выхода...");
                Console.ReadKey();
            
            
        }

        // Чтение файла CSV и парсинг строк
        static List<SaleRecord> ReadSalesFromFile(string filePath)
        {
            var sales = new List<SaleRecord>();
            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    {
                        var sale = ParseSale(line);
                        sales.Add(sale);
                    }
                }
            }
            return sales;
        }

        // Парсинг одной строки из файла CSV
        static SaleRecord ParseSale(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 12)
                throw new FormatException("Неверное количество полей");
            return new SaleRecord
            {
                OrderId = int.Parse(parts[0]),
                OrderDate = DateTime.Parse(parts[1], CultureInfo.InvariantCulture),
                CustomerId = int.Parse(parts[2]),
                ProductCategory = parts[3],
                Region = parts[4],
                Quantity = int.Parse(parts[5]),
                UnitPrice = decimal.Parse(parts[6], CultureInfo.InvariantCulture),
                Discount = decimal.Parse(parts[7], CultureInfo.InvariantCulture),
                PaymentMethod = parts[8],
                DeliveryDays = int.Parse(parts[9]),
                CustomerRating = double.Parse(parts[10], CultureInfo.InvariantCulture),
                Revenue = decimal.Parse(parts[11], CultureInfo.InvariantCulture)
            };
        }

        // Вывод списка в таблицу
        static void PrintSales(List<SaleRecord> sales)
        {
            Console.WriteLine($"{"OrderId",-10} {"Date",-10} {"Category",-15} {"Qty",-5} {"Price",-10} {"Revenue",-12}");
            Console.WriteLine(new string('-', 65)); // TODO: (melss) Что это за строка? Зачем и почему так написано?
            for (int i = 0; i < sales.Count; i++)  // TODO: (melss) Объясните почему здесь написали for? Почему не foreach?
            {
                var s = sales[i];
                Console.WriteLine($"{s.OrderId,-10} {s.OrderDate:dd.MM.yyyy} {s.ProductCategory,-15} {s.Quantity,-5} {s.UnitPrice,-10:F2} {s.Revenue,-12:F2}");
            }
        }

        // Сумма выручки по категориям
        public static Dictionary<string, decimal> TotalRevenueByCategory(List<SaleRecord> sales)
        {
            return sales
                .GroupBy(s => s.ProductCategory)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Revenue));
        }

        // Топ-5 категорий по общему количеству
        public static Dictionary<string, int> Top5ProductsByQuantity(List<SaleRecord> sales)
        {
            return sales
                .GroupBy(s => s.ProductCategory)
                .Select(g => new { Category = g.Key, TotalQty = g.Sum(s => s.Quantity) })
                .OrderByDescending(x => x.TotalQty)
                .Take(5)
                .ToDictionary(x => x.Category, x => x.TotalQty);
        }

        // Средняя цена за месяц
        public static Dictionary<DateTime, decimal> AveragePricePerMonth(List<SaleRecord> sales)
        {
            return sales
                .GroupBy(s => new DateTime(s.OrderDate.Year, s.OrderDate.Month, 1))
                .ToDictionary(g => g.Key, g => g.Average(s => s.UnitPrice));
        }

        // Сохранение в JSON
        static void SaveToJson(string filePath, object data)
        {
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, json);
            }
        }
    }
}