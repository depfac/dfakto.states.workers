using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using dFakto.States.Workers.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace dFakto.States.Workers.Tests
{
    public static class StringUtils
    {
        private static readonly Random random = new Random();

        public static string Random(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
    public static class WorkerExtensions
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter()},
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            IgnoreNullValues = true
        };

        public static async Task<U> DoJsonWork<T, U>(this IWorker worker, T input)
        {
            return JsonSerializer.Deserialize<U>(
                await worker.DoRawJsonWorkAsync(JsonSerializer.Serialize(input, Options), CancellationToken.None),
                Options);
        }
    }
}