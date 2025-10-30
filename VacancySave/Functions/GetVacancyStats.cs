using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Amazon.S3;
using Amazon.S3.Model;

namespace VacancySave
{
    public class GetVacancyStats
    {
        private readonly ILogger _logger;
        private readonly IAmazonS3 _s3Client;
        private const string BucketName = "vacancy-stats-test-task";

        public GetVacancyStats(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetVacancyStats>();

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

        [Function("GetVacancyStats")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Fetching vacancy stats from Object Storage...");

            try
            {
                var s3Object = await _s3Client.GetObjectAsync(BucketName, "vacancies_stats.json");
                using var reader = new StreamReader(s3Object.ResponseStream);
                var content = await reader.ReadToEndAsync();

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(content);
                return response;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File 'vacancies_stats.json' not found in bucket {BucketName}", BucketName);
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("File 'vacancies_stats.json' not found.");
                return notFoundResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file from S3");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Error retrieving file.");
                return errorResponse;
            }
        }
    }
}
