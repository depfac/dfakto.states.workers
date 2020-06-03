using System.Text.Json;

namespace dFakto.States.Workers.Abstractions.Sql
{
    public class SqlQueryParameter
    {
        public string Name { get; set; }
        public JsonElement Value { get; set; }
    }
}