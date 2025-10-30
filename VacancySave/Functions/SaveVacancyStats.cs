using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Amazon.S3;
using Amazon.S3.Model;
using VacancySave.Models;

namespace VacancySave
{
    public class SaveVacancyStats
    {
        private readonly ILogger _logger;
        private readonly IAmazonS3 _s3Client;
        private const string BucketName = "vacancy-stats-test-task"; // Имя бакета в Yandex cloud
        private const string ObjectKey = "vacancies_stats.json"; // Имя json файла в бакете Yandex cloud

        public SaveVacancyStats(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SaveVacancyStats>();

            var config = new AmazonS3Config
            {
                ServiceURL = Environment.GetEnvironmentVariable("AWS_ENDPOINT"),
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(
                Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
                Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
                config
            );
        }

        [Function("SaveVacancyStats")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Fetching C# Developer vacancies from HH.ru...");

            int vacanciesCount;
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "VacancySaveFunction/1.0"); // обязательно для HH.ru

                // Корректно кодируем текст вакансии
                string query = Uri.EscapeDataString("C# Developer");
                string url = $"https://api.hh.ru/vacancies?text={query}&schedule=remote&per_page=100";

                HttpResponseMessage hhResponse = await httpClient.GetAsync(url);
                hhResponse.EnsureSuccessStatusCode();

                string content = await hhResponse.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                vacanciesCount = jsonDoc.RootElement.GetProperty("found").GetInt32();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запросе HH.ru API");
                var errorResp = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResp.WriteStringAsync("Ошибка при запросе HH.ru API");
                return errorResp;
            }

            _logger.LogInformation($"Vacancies found: {vacanciesCount}");

            // Подготовка объекта для записи
            var today = DateTime.UtcNow.Date;
            var stat = new VacancyStat
            {
                Date = today,
                VacanciesCount = vacanciesCount
            };

            // Загружаем существующий файл из S3
            List<VacancyStat> stats = new List<VacancyStat>();
            try
            {
                var s3Object = await _s3Client.GetObjectAsync(BucketName, ObjectKey);
                using var reader = new StreamReader(s3Object.ResponseStream);
                string existingContent = await reader.ReadToEndAsync();
                stats = JsonSerializer.Deserialize<List<VacancyStat>>(existingContent) ?? new List<VacancyStat>();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Файл vacancies_stats.json ещё не существует, будет создан новый.");
            }

            // Обновляем или добавляем запись
            var existing = stats.FirstOrDefault(s => s.Date == today);
            if (existing != null)
                existing.VacanciesCount = vacanciesCount;
            else
                stats.Add(stat);

            // Сохраняем обратно в S3
            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = ObjectKey,
                    ContentBody = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true }),
                    ContentType = "application/json"
                };
                await _s3Client.PutObjectAsync(putRequest);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении файла в S3");
                var errorResp = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResp.WriteStringAsync("Ошибка при сохранении файла в Object Storage");
                return errorResp;
            }

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Файл vacancies_stats.json успешно обновлён в бакете.");
            return response;
        }

    }
}
