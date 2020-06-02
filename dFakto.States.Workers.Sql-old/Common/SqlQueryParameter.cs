using System.Text.Json;

namespace dFakto.States.Workers.Sql.Common
{
    public class SqlQueryParameter
    {
        public string Name { get; set; }
        public JsonElement Value { get; set; }
    }
}