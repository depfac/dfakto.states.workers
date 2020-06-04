using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace dFakto.States.Workers.Http
{
    public class HttpWorkerInput
    {
        public Uri Uri { get; set; }
        public string Method { get; set; } = HttpMethod.Get.Method;
        public JsonElement? JsonContent { get; set; }
        public string ContentFileToken { get; set; }
        public string OutputFileName { get; set; }
        public string OutputFileStoreName { get; set; }
        public int Timeout { get; set; } = 60;
        public bool FailIfError { get; set; } = true;
        
        public Dictionary<string,string> HttpHeaders { get; set; }
        public Dictionary<string,string> HttpQueryParams { get; set; }
    }
}