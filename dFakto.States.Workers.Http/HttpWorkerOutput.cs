using System.Net;
using System.Text.Json;

namespace dFakto.States.Workers.Http
{
    public class HttpWorkerOutput
    {
        public JsonElement? JsonContent { get; set; }
        public string ContentFileToken { get; set; }
        public long Length { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Reason { get; set; }
    }
}