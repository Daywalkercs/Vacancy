using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using VacancySave.Models;

namespace VacancySave
{
    public class SaveVacancyStats
    {
        private static readonly string? BucketName = Environment.GetEnvironmentVariable("BUCKET_NAME");
        private static readonly string ObjectKey = "vacancies_stats.json";

        public async Task<Response> FunctionHandler(Request request)
        {
            Console.WriteLine("Fetching C# Developer vacancies from HH.ru...");

            int vacanciesCount;
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "VacancySaveFunction/1.0");

                string query = Uri.EscapeDataString("C# Developer");
                string url = $"https://api.hh.ru/vacancies?text={query}&per_page=100";

                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);
                vacanciesCount = json.RootElement.GetProperty("found").GetInt32();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при запросе HH.ru API: " + ex.Message);
                return new Response(500, "Ошибка при запросе HH.ru API");
            }

            Console.WriteLine($"Vacancies found: {vacanciesCount}");

            var today = DateTime.UtcNow.Date;
            List<VacancyStat> stats = new();

            var s3Config = new AmazonS3Config
            {
                ServiceURL = Environment.GetEnvironmentVariable("AWS_ENDPOINT"),
                ForcePathStyle = true
            };

            var s3Client = new AmazonS3Client(
                Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
                Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
                s3Config
            );

            // Загружаем существующий файл
            try
            {
                var s3Object = await s3Client.GetObjectAsync(BucketName, ObjectKey);
                using var reader = new StreamReader(s3Object.ResponseStream);
                string existing = await reader.ReadToEndAsync();
                stats = JsonSerializer.Deserialize<List<VacancyStat>>(existing) ?? new();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("Файл ещё не существует, будет создан новый.");
            }

            // Обновляем или добавляем запись
            var existingItem = stats.Find(s => s.Date == today);
            if (existingItem != null)
                existingItem.VacanciesCount = vacanciesCount;
            else
                stats.Add(new VacancyStat { Date = today, VacanciesCount = vacanciesCount });

            string jsonToSave = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });

            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = BucketName,
                Key = ObjectKey,
                ContentBody = jsonToSave,
                ContentType = "application/json"
            });

            Console.WriteLine("Данные успешно сохранены в бакет.");

            return new Response(200, "Статистика успешно сохранена.");
        }
    }
}
