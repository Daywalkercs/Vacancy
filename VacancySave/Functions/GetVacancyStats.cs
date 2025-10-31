using Amazon.S3;
using VacancySave.Models;

namespace VacancySave
{
    public class GetVacancyStats
    {
        private static readonly string? BucketName = Environment.GetEnvironmentVariable("BUCKET_NAME");
        private static readonly string ObjectKey = "vacancies_stats.json";

        public async Task<Response> FunctionHandler(Request request)
        {
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

            try
            {
                var s3Object = await s3Client.GetObjectAsync(BucketName, ObjectKey);
                using var reader = new StreamReader(s3Object.ResponseStream);
                string content = await reader.ReadToEndAsync();

                return new Response(200, content);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new Response(404, "Файл vacancies_stats.json не найден");
            }
            catch (Exception ex)
            {
                return new Response(500, "Ошибка при получении файла: " + ex.Message);
            }
        }
    }
}
