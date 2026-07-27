Консольное приложение для анализа данных о продажах из CSV-файла.

Режимы работы (задаются через аргумент --mode):
-----------------------------------------------
console   - синхронное чтение CSV, расчёт аналитики, вывод в консоль.
file      - синхронное чтение, расчёт аналитики, сохранение результатов в JSON.
async     - асинхронное чтение и (если указан --output) асинхронная запись в JSON.
parallel  - синхронное чтение, параллельные вычисления (PLINQ), вывод в консоль.
full      - асинхронное чтение + параллельные вычисления + асинхронная запись в JSON
            (если указан --output).

Аргументы командной строки:
----------------------------
--mode - режим работы (по умолчанию console)
--input - путь к входному CSV-файлу (обязательный)
--output - путь для сохранения JSON-результатов (требуется для режимов file и full, опционален для async)

Примеры запуска:
-----------------
dotnet run --mode=console --input=sales_5000.csv
dotnet run --mode=file --input=sales_5000.csv --output=result.json
dotnet run --mode=async --input=sales_5000.csv --output=result_async.json
dotnet run --mode=parallel --input=sales_5000.csv
dotnet run --mode=full --input=sales_5000.csv --output=result_full.json
