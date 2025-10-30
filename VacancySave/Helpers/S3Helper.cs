using Amazon.S3;
using Amazon.S3.Model;
using System.Text.Json;
using VacancySave.Models;

namespace VacancySave.Helpers
{
    public class S3Helper
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public S3Helper(string accessKey, string secretKey, string serviceUrl, string bucketName)
        {
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true // важно для Yandex Object Storage
            };
            _s3Client = new AmazonS3Client(accessKey, secretKey, config);
            _bucketName = bucketName;
        }

        // Чтение файла с вакансиями
        public async Task<List<VacancyStat>> GetStatsAsync(string fileName)
        {
            try
            {
                var response = await _s3Client.GetObjectAsync(_bucketName, fileName);
                using var stream = response.ResponseStream;
                return await JsonSerializer.DeserializeAsync<List<VacancyStat>>(stream) ?? new List<VacancyStat>();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Если файла нет — возвращаем пустой список
                return new List<VacancyStat>();
            }
        }

        // Сохранение списка ваканский в S3
        public async Task SaveStatsAsync(string fileName, List<VacancyStat> stats)
        {
            var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = stream
            };

            await _s3Client.PutObjectAsync(request);
        }
    }
}
