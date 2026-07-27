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
                    var revenue = TotalRevenueByCategory(sales);
                    var top5 = Top5ProductsByQuantity(sales);
                    var avgPrice = AveragePricePerMonth(sales);
                    PrintAnalytics(revenue, top5, avgPrice);
                }
                else if (mode == "file")
                {
                    // Синхронное чтение + вычисления + синхронная запись JSON
                    var sales = ReadSalesFromFile(inputFile);
                    PrintSales(sales);
                    var revenue = TotalRevenueByCategory(sales);
                    var top5 = Top5ProductsByQuantity(sales);
                    var avgPrice = AveragePricePerMonth(sales);
                    var result = new
                    {
                        TotalRevenueByCategory = revenue,
                        Top5ProductsByQuantity = top5,
                        AveragePricePerMonth = avgPrice
                    };
                    PrintAnalytics(revenue, top5, avgPrice);
                    SaveToJson(outputFile, result);  // синхронная запись 
                }
                else if (mode == "async")
                {
                    // Асинхронное чтение
                    var sales = await ReadSalesFromFileAsync(inputFile);

                    // Вычисления (синхронные)
                    var revenue = TotalRevenueByCategory(sales);
                    var top5 = Top5ProductsByQuantity(sales);
                    var avgPrice = AveragePricePerMonth(sales);
                    if (!string.IsNullOrEmpty(outputFile))
                    {
                        // Асинхронная запись JSON
                        var result = new
                        {
                            TotalRevenueByCategory = revenue,
                            Top5ProductsByQuantity = top5,
                            AveragePricePerMonth = avgPrice
                        };
                        await SaveToJsonAsync(outputFile, result);
                    }
                    PrintSales(sales);
                    PrintAnalytics(revenue, top5, avgPrice);
                    
                }
                else if (mode == "parallel")
                {
                    // Синхронное чтение (как в console)
                    var sales = ReadSalesFromFile(inputFile);
                    PrintSales(sales);

                    // Вычисления с параллельной обработкой (флаг true)
                    var revenue = TotalRevenueByCategory(sales, useParallel: true);
                    var top5 = Top5ProductsByQuantity(sales, useParallel: true);
                    var avgPrice = AveragePricePerMonth(sales, useParallel: true);

                    PrintAnalytics(revenue, top5, avgPrice);
                }
                else if (mode == "full")
                {
                    var sales = await ReadSalesFromFileAsync(inputFile);
                    var revenue = TotalRevenueByCategory(sales, useParallel: true);
                    var top5 = Top5ProductsByQuantity(sales, useParallel: true);
                    var avgPrice = AveragePricePerMonth(sales, useParallel: true);
                    if (!string.IsNullOrEmpty(outputFile))
                    {
                        // Асинхронная запись JSON
                        var result = new
                        {
                            TotalRevenueByCategory = revenue,
                            Top5ProductsByQuantity = top5,
                            AveragePricePerMonth = avgPrice
                        };
                        await SaveToJsonAsync(outputFile, result);
                    }
                    PrintSales(sales);
                    PrintAnalytics(revenue, top5, avgPrice);
                    
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                Environment.ExitCode = 1;
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
                        Console.WriteLine($"Ошибка: {ex.Message}");
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
            var query = sales.GroupBy(s => s.ProductCategory)
                             .Select(g => new { Category = g.Key, Sum = g.Sum(s => s.Revenue) });
            if (useParallel)
                query = query.AsParallel().Select(x => x);
            return query.ToDictionary(x => x.Category, x => x.Sum);
        }

        public static Dictionary<string, int> Top5ProductsByQuantity(List<Sale> sales, bool useParallel = false)
        {
            var query = sales.GroupBy(s => s.ProductCategory)
                             .Select(g => new { Category = g.Key, TotalQty = g.Sum(s => s.Quantity) })
                             .OrderByDescending(x => x.TotalQty)
                             .Take(5);
            if (useParallel)
                query = query.AsParallel().OrderByDescending(x => x.TotalQty).Take(5);
            return query.ToDictionary(x => x.Category, x => x.TotalQty);
        }

        public static Dictionary<DateTime, decimal> AveragePricePerMonth(List<Sale> sales, bool useParallel = false)
        {
            var query = sales.GroupBy(s => new DateTime(s.OrderDate.Year, s.OrderDate.Month, 1))
                             .Select(g => new { Month = g.Key, Avg = g.Average(s => s.UnitPrice) });
            if (useParallel)
                query = query.AsParallel().Select(x => x);
            return query.ToDictionary(x => x.Month, x => x.Avg);
        }

        static void SaveToJson(string filePath, object data)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"Результаты сохранены в {filePath}");
            }
            catch (Exception ex)
            {
                throw new IOException($"Не удалось записать файл {filePath}: {ex.Message}", ex);
            }


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
        static async Task SaveToJsonAsync(string filePath, object data)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string json = JsonSerializer.Serialize(data, options);
                await File.WriteAllTextAsync(filePath, json);
                Console.WriteLine($"Результаты сохранены в {filePath}");
            }
            catch (Exception ex)
            {
                throw new IOException($"Не удалось записать файл {filePath}: {ex.Message}", ex);
            }
        }
        static void PrintAnalytics(Dictionary<string, decimal> revenue,
                           Dictionary<string, int> top5,
                           Dictionary<DateTime, decimal> avgPrice)
        {
            Console.WriteLine("\n=== Общая выручка по категориям ===");
            foreach (var kv in revenue)
                Console.WriteLine($"{kv.Key,-20} {kv.Value,15:F2}");

            Console.WriteLine("\n=== Топ-5 категорий по количеству продаж ===");
            foreach (var kv in top5)
                Console.WriteLine($"{kv.Key,-20} {kv.Value,10} шт.");

            Console.WriteLine("\n=== Средняя цена за месяц ===");
            foreach (var kv in avgPrice)
                Console.WriteLine($"{kv.Key,-15:MMMM yyyy} {kv.Value,10:F2}");
        }
    }
}
