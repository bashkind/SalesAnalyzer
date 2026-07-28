using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace SalesAnalyzer
{
    internal class Program
    {
        // Шаблон записи о продаже
        public class Sale
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

        /// <summary>
        /// Нет комментариев к коду.
        /// Нет аргументов для отбора по диапазону дат, начало и конец. 
        /// </summary>
        static async Task Main(string[] args)
        {
            try
            {
                string mode = "console";          // по умолчанию
                string inputFile = null;
                string outputFile = null;

                foreach (string arg in args)
                {
                    if (arg.StartsWith("--"))
                    {
                        string[] parts = arg.Substring(2).Split('=', 2);
                        string key = parts[0];
                        string value = parts[1];

                        if (key == "mode")
                            mode = value;
                        else if (key == "input")
                            inputFile = value;
                        else if (key == "output")
                            outputFile = value;
                    }
                }
                if (mode == "console")
                {
                    // Синхронное чтение + вывод в консоль
                    var sales = ReadSalesFromFile(inputFile);
                    PrintSales(sales);
                    // Неправильное использование AsParallel, не было профита.
                    // Тут как пример для замера времени исполнения.
                    var sw = new Stopwatch();
                    sw.Start();
                    var revenue = TotalRevenueByCategory(sales);
                    sw.Stop();
                    Console.WriteLine($"revenue {sw.ElapsedMilliseconds} ms, [{revenue.Count}]");
                    sw.Restart();
                    var revenueP = TotalRevenueByCategory(sales, true);
                    sw.Stop();
                    Console.WriteLine($"revenue parallel {sw.ElapsedMilliseconds} ms, [{revenueP.Count}]");

                    sw.Restart();
                    var top5 = Top5ProductsByQuantity(sales);
                    sw.Stop();
                    Console.WriteLine($"top5 {sw.ElapsedMilliseconds} ms, [{top5.Count}]");
                    sw.Restart();
                    var top5P = Top5ProductsByQuantity(sales, true);
                    sw.Stop();
                    Console.WriteLine($"top5 parallel {sw.ElapsedMilliseconds} ms, [{top5P.Count}]");

                    sw.Restart();
                    var avgPrice = AveragePricePerMonth(sales);
                    sw.Stop();
                    Console.WriteLine($"avgPrice {sw.ElapsedMilliseconds} ms, [{avgPrice.Count}]");
                    sw.Restart();
                    var avgPriceP = AveragePricePerMonth(sales, true);
                    sw.Stop();
                    Console.WriteLine($"avgPrice {sw.ElapsedMilliseconds} ms, [{avgPriceP.Count}]");
                    sw.Restart();

                    var result = new ReportData(sales, false);
                    PrintAnalytics(result);
                }
                else if (mode == "file")
                {
                    // Если вывод в файл то зачем выводить в консоль?
                    // Синхронное чтение + вычисления + синхронная запись JSON
                    var sales = ReadSalesFromFile(inputFile);
                    var result = new ReportData(sales, false);
                    SaveToJson(outputFile, result);  // синхронная запись 
                }
                else if (mode == "async")
                {
                    // Асинхронное чтение
                    var sales = await ReadSalesFromFileAsync(inputFile);

                    // Вычисления (синхронные)
                    var result = new ReportData(sales, false);
                    if (!string.IsNullOrEmpty(outputFile))
                    {
                        // Асинхронная запись JSON
                        await SaveToJsonAsync(outputFile, result);
                    }
                    PrintSales(sales);
                    PrintAnalytics(result);

                }
                else if (mode == "parallel")
                {
                    // Синхронное чтение (как в console)
                    var sales = ReadSalesFromFile(inputFile);
                    PrintSales(sales);
                    // Вычисления с параллельной обработкой (флаг true)
                    var result = new ReportData(sales, true);
                    PrintAnalytics(result);
                }
                else if (mode == "full")
                {
                    var sales = await ReadSalesFromFileAsync(inputFile);
                    var result = new ReportData(sales, true);
                    if (!string.IsNullOrEmpty(outputFile))
                    {
                        // Асинхронная запись JSON
                        await SaveToJsonAsync(outputFile, result);
                    }
                    PrintSales(sales);
                    PrintAnalytics(result);

                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }

        /// <summary>
        /// Стараемся не использовать тип object.
        /// Лучше сделать класс.
        /// Уменьшаем дублирование кода.
        /// </summary>
        private sealed class ReportData
        {
            public Dictionary<string, decimal> TotalRevenueByCategory { get; }
            public Dictionary<string, int> Top5ProductsByQuantity { get; }
            public Dictionary<DateTime, decimal> AveragePricePerMonth { get; }

            public ReportData(List<Sale> sales, bool useParallel)
            {
                TotalRevenueByCategory = TotalRevenueByCategory(sales, useParallel);
                Top5ProductsByQuantity = Top5ProductsByQuantity(sales, useParallel);
                AveragePricePerMonth = AveragePricePerMonth(sales, useParallel);
            }
        }

        static List<Sale> ReadSalesFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл {filePath} не найден.");

            var sales = new List<Sale>();
            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var sale = ParseSale(line);
                        sales.Add(sale);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Пропущена строка: {line}");
                        // В теории не требуется, так как try/catch в main перехватит и напишет текст ошибки, а так двойной вывод.
                        // Console.WriteLine($"Ошибка: {ex.Message}");
                    }
                }
            }
            return sales;
        }

        static Sale ParseSale(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length < 12)
                throw new FormatException("Неверное количество полей");
            // Лучше использовать версию TryParse, напрмер можно вывести непосредственно значение которое не прошло парсинг.
            return new Sale
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

        static void PrintSales(List<Sale> sales)
        {
            Console.WriteLine($"{"OrderId",-10} {"Date",-10} {"Category",-15} {"Qty",-5} {"Price",-10} {"Revenue",-12}");
            Console.WriteLine(new string('-', 65));
            foreach (var s in sales)
            {
                Console.WriteLine($"{s.OrderId,-10} {s.OrderDate:dd.MM.yyyy} {s.ProductCategory,-15} {s.Quantity,-5} {s.UnitPrice,-10:F2} {s.Revenue,-12:F2}");
            }
        }

        public static Dictionary<string, decimal> TotalRevenueByCategory(List<Sale> sales, bool useParallel = false)
        {
            var query = sales.GroupBy(s => s.ProductCategory);
            if (useParallel)
                query = query.AsParallel();
            return query.Select(g => new { Category = g.Key, Sum = g.Sum(s => s.Revenue) })
                .ToDictionary(x => x.Category, x => x.Sum);
        }

        public static Dictionary<string, int> Top5ProductsByQuantity(List<Sale> sales, bool useParallel = false)
        {
            var query = sales.GroupBy(s => s.ProductCategory);
            if (useParallel)
                query = query.AsParallel();
            return query
                .Select(g => new { Category = g.Key, TotalQty = g.Sum(s => s.Quantity) })
                .OrderByDescending(x => x.TotalQty)
                .Take(5)
                .ToDictionary(x => x.Category, x => x.TotalQty);
        }

        public static Dictionary<DateTime, decimal> AveragePricePerMonth(List<Sale> sales, bool useParallel = false)
        {
            var query = sales.GroupBy(s => new DateTime(s.OrderDate.Year, s.OrderDate.Month, 1));
            if (useParallel)
                query = query.AsParallel();
            return query.Select(g => new { Month = g.Key, Avg = g.Average(s => s.UnitPrice) })
                .ToDictionary(x => x.Month, x => x.Avg);
        }

        static void SaveToJson(string filePath, ReportData data)
        {
            /// В теории код парсинга можно вынести как общий код.
            string json = GetJson(data);
            // Стараемся обернуть минимум кода который вызовет ошибку.
            // Вывод ошибку про файл, но в try обернули и парсинг.
            try
            {
                File.WriteAllText(filePath, json);
                Console.WriteLine($"Результаты сохранены в {filePath}");
            }
            catch (Exception ex)
            {
                // Стандартные ошибки стараются не использовать IOException, тут может быть подмена.
                throw new IOException($"Не удалось записать файл {filePath}: {ex.Message}", ex);
            }


        }

        private static string GetJson(ReportData data)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(data, options);
        }

        static async Task<List<Sale>> ReadSalesFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл {filePath} не найден.");

            var sales = new List<Sale>();
            using (var reader = new StreamReader(filePath))
            {
                await reader.ReadLineAsync();
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var sale = ParseSale(line);
                        sales.Add(sale);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Пропущена строка: {line}");
                        Console.WriteLine($"Ошибка: {ex.Message}");
                    }
                }
            }
            return sales;
        }
        static async Task SaveToJsonAsync(string filePath, ReportData data)
        {
            string json = GetJson(data);
            try
            {
                await File.WriteAllTextAsync(filePath, json);
                Console.WriteLine($"Результаты сохранены в {filePath}");
            }
            catch (Exception ex)
            {
                throw new IOException($"Не удалось записать файл {filePath}: {ex.Message}", ex);
            }
        }
        static void PrintAnalytics(ReportData data)
        {
            Console.WriteLine("\n=== Общая выручка по категориям ===");
            foreach (var kv in data.TotalRevenueByCategory)
                Console.WriteLine($"{kv.Key,-20} {kv.Value,15:F2}");

            Console.WriteLine("\n=== Топ-5 категорий по количеству продаж ===");
            foreach (var kv in data.Top5ProductsByQuantity)
                Console.WriteLine($"{kv.Key,-20} {kv.Value,10} шт.");

            Console.WriteLine("\n=== Средняя цена за месяц ===");
            foreach (var kv in data.AveragePricePerMonth)
                Console.WriteLine($"{kv.Key,-15:MMMM yyyy} {kv.Value,10:F2}");
        }
    }
}
