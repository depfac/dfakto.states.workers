using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using dFakto.States.Workers.Abstractions;
using dFakto.States.Workers.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Xunit;

namespace dFakto.States.Workers.Tests
{
    public class HttpWorkerTests : BaseTests
    {
        class TestJson
        {
            public int IntTest { get; set; } = 33;
        }
        private readonly IStoreFactory _storeFactory;

        public HttpWorkerTests()
        {
            _storeFactory = Host.Services.GetService<IStoreFactory>();
        }

        private string GetFileTokenContent(string fileToken)
        {
            using(var fileStore = _storeFactory.GetFileStoreFromFileToken(fileToken))
            using (var input = fileStore.OpenRead(fileToken).Result)
            using (var reader = new StreamReader(input, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
        
        private string SetFileTokenContent(string content)
        {
            using var fileStore = _storeFactory.GetFileStoreFromName("test");

            var token = fileStore.CreateFileToken("tmp").Result;
            using (var output = fileStore.OpenWrite(token).Result)
            using (var writer = new StreamWriter(output))
            {
                writer.Write(content);
            }

            return token;
        }
        
        [Fact]
        public async Task TestGetJson()
        {
            HttpWorker worker = Host.Services.GetService<HttpWorker>();
            var response = await worker.DoJsonWork<HttpWorkerInput,HttpWorkerOutput>(new HttpWorkerInput
            {
                Method = "GET",
                Uri = new Uri("https://postman-echo.com/get?foor=bar"),
                OutputContentFileName = "test.json",
                OutputFileStoreName = "test"
            });

            Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            Assert.True(response.Length > 0);

            var json = GetFileTokenContent(response.ContentFileToken);
            using (var doc = JsonDocument.Parse(json))
            {
                Assert.Equal("https://postman-echo.com/get?foor=bar", doc.RootElement.EnumerateObject().First(x => x.Name == "url").Value.GetString());   
            }
        }
        
        [Fact]
        public async Task TestPostJson()
        {
            var json = JsonSerializer.Serialize(new TestJson(), new JsonSerializerOptions());

            var token = SetFileTokenContent(json);
            
            IWorker worker = Host.Services.GetService<HttpWorker>();
            var response = await worker.DoJsonWork<HttpWorkerInput,HttpWorkerOutput>(new HttpWorkerInput
            {
                Method = "POST",
                Uri = new Uri("https://postman-echo.com/post"),
                ContentFileToken = token,
                OutputFileStoreName = "test"
            });

            Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            Assert.True(response.Length > 0);

            var content = GetFileTokenContent(response.ContentFileToken);
            using (var d = JsonDocument.Parse(content))
            {
                Assert.Equal(json,d.RootElement.GetProperty("data").ToString());
            }
        }
        
        [Fact]
        public async Task TestPostText()
        {
            var text = "Hello world, thanks postman-echo !";
            using var fileStore = _storeFactory.GetFileStoreFromName("test");

            string token = await fileStore.CreateFileToken("sample.txt");
            using (StreamWriter writer = new StreamWriter(await fileStore.OpenWrite(token)))
            {
                writer.Write(text);
            }
            HttpWorker worker = Host.Services.GetService<HttpWorker>();
            var response = await worker.DoJsonWork<HttpWorkerInput,HttpWorkerOutput>(new HttpWorkerInput
            {
                Method = "POST",
                Uri = new Uri("https://postman-echo.com/post"),
                ContentFileToken = token,
                OutputFileStoreName = "test",
                OutputContentFileName = "test.json"
            });

            Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            Assert.True(response.Length > 0);
            Assert.NotNull(response.ContentFileToken);
        }
        
        [Fact]
        public async Task TestErrorManagement()
        {
            HttpWorker worker = Host.Services.GetService<HttpWorker>();
            var response = await worker.DoJsonWork<HttpWorkerInput,HttpWorkerOutput>(new HttpWorkerInput
            {
                Method = "GET",
                Uri = new Uri("https://postman-echo.com/status/404"),
                FailIfError = false
            });

            Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
            Assert.Null(response.ContentFileToken);
        }
        
        [Fact]
        public async Task TestAdditionalHttpHeaders()
        {
            HttpWorker worker = Host.Services.GetService<HttpWorker>();
            var response = await worker.DoJsonWork<HttpWorkerInput,HttpWorkerOutput>(new HttpWorkerInput
            {
                Method = "GET",
                Uri = new Uri("https://postman-echo.com/headers"),
                OutputFileStoreName = "test",
                OutputContentFileName = "test.json",
                AdditionalHeaders = new Dictionary<string, string>()
                {
                    {"apikey","SECRET_KEY"}
                }
            });
            
            var content = GetFileTokenContent(response.ContentFileToken);
            using (var d = JsonDocument.Parse(content))
            {
                Assert.Equal("SECRET_KEY",d.RootElement.GetProperty("headers").GetProperty("apikey").ToString());
            }
        }
    }
}