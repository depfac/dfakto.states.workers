using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.FileStores;
using dFakto.States.Workers.Interfaces;
using Microsoft.Extensions.Logging;

namespace dFakto.States.Workers
{
    public class HttpWorkerInput
    {
        public Uri Uri { get; set; }
        public string Method { get; set; } = HttpMethod.Get.Method;
        public JsonElement? JsonContent { get; set; }
        public string ContentFileToken { get; set; }
        public string OutputContentFileName { get; set; }
        public string OutputFileStoreName { get; set; }
        public int Timeout { get; set; } = 60;
        public bool FailIfError { get; set; } = true;
    }

    public class HttpWorkerOutput
    {
        public JsonElement? JsonContent { get; set; }
        public string ContentFileToken { get; set; }
        public long Length { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Reason { get; set; }
    }
    
    public class HttpWorker : BaseWorker<HttpWorkerInput, HttpWorkerOutput>
    {
        private const string JsonMediaType = "application/json";
        private readonly ILogger<HttpWorker> _logger;
        private readonly FileStoreFactory _storeFactory;

        public HttpWorker(ILogger<HttpWorker> logger, FileStoreFactory storeFactory)
        :base("Http", TimeSpan.FromSeconds(8))
        {
            _logger = logger;
            _storeFactory = storeFactory;
        }

        public override async Task<HttpWorkerOutput> DoWorkAsync(HttpWorkerInput workerInput, CancellationToken token)
        {
            _logger.LogInformation($"{workerInput.Method}:{workerInput.Uri}");
            
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(workerInput.Timeout);

                var requestContent = await GetRequestContent(workerInput);

                _logger.LogDebug("Sending HTTP request");
                using(HttpRequestMessage request = new HttpRequestMessage
                    {
                        RequestUri = workerInput.Uri,
                        Method = new HttpMethod(workerInput.Method),
                        Content = requestContent
                    })
                using (var response = await client.SendAsync(request, token))
                {
                    _logger.LogInformation($"Response received : {response.StatusCode} - {response.ReasonPhrase}");
                    var result = new HttpWorkerOutput
                    {
                        StatusCode = response.StatusCode,
                        Reason = response.ReasonPhrase
                    };
                    
                    if(workerInput.FailIfError && !response.IsSuccessStatusCode)
                        throw new WorkerException("dFakto.Http.Error",$"HTTP Status : '{response.StatusCode}' Reason : '{response.ReasonPhrase}'");
                
                    if (response.IsSuccessStatusCode &&
                        response.Content.Headers.ContentLength.HasValue &&
                        response.Content.Headers.ContentLength.Value > 0)
                    {
                        if (response.Content.Headers.ContentType.MediaType == JsonMediaType && string.IsNullOrEmpty(workerInput.OutputFileStoreName))
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            using (var doc = JsonDocument.Parse(json))
                            {
                                result.JsonContent = doc.RootElement.Clone();
                            }
                            result.Length = json.Length;
                        }
                        else
                        {
                            var outputFileName = GetOutputFileName(workerInput, response);
                            _logger.LogDebug($"Saving Response body with token '{result.ContentFileToken}' (Filename : {outputFileName})");
                            
                            using var outputFileStore = _storeFactory.GetFileStoreFromName(workerInput.OutputFileStoreName);
                            result.ContentFileToken = await outputFileStore.CreateFileToken(outputFileName);

                            using var output = await outputFileStore.OpenWrite(result.ContentFileToken);
                            using var input = await response.Content.ReadAsStreamAsync();
                            result.Length = input.Length;
                            await input.CopyToAsync(output, 2048, token);
                        }
                    }

                    return result;
                }
            }
        }

        private static string GetOutputFileName(HttpWorkerInput workerInput, HttpResponseMessage response)
        {
            string outputFileName = workerInput.OutputContentFileName ??
                                    response.Content.Headers.ContentDisposition?.FileName ??
                                    workerInput.Uri.Segments.Last();
            if (outputFileName.EndsWith("/"))
                outputFileName = outputFileName.Substring(0, outputFileName.Length - 1);

            if (string.IsNullOrWhiteSpace(outputFileName))
            {
                outputFileName = "http_default_" + DateTime.Now.Ticks;
            }

            return outputFileName;
        }

        private async Task<HttpContent> GetRequestContent(HttpWorkerInput workerInput)
        {
            HttpContent requestContent = null;
            if (workerInput.JsonContent.HasValue && workerInput.JsonContent.Value.ValueKind != JsonValueKind.Null &&
                workerInput.JsonContent.Value.ValueKind != JsonValueKind.Undefined)
            {
                _logger.LogDebug("Using RequestBody Json as Request Content");
                requestContent = new StringContent(workerInput.JsonContent.Value.GetRawText(), Encoding.UTF8, JsonMediaType);
            }
            else if (workerInput.ContentFileToken != null)
            {
                _logger.LogDebug($"Using RequestBody from Token '{workerInput.ContentFileToken}' as Request Content");
                var inputStore = _storeFactory.GetFileStoreFromFileToken(workerInput.ContentFileToken);
                requestContent = new StreamContent(await inputStore.OpenRead(workerInput.ContentFileToken));
                requestContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                requestContent.Headers.ContentType.CharSet = "utf-8";
            }

            return requestContent;
        }
    }
}